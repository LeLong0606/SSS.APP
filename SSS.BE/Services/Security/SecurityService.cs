using Microsoft.EntityFrameworkCore;
using SSS.BE.Domain.Entities;
using SSS.BE.Persistence;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SSS.BE.Services.Security;

public interface IAntiSpamService
{
    Task<bool> IsSpamRequestAsync(string ipAddress, string? userId, string endpoint, string requestData);
    Task LogRequestAsync(string ipAddress, string? userId, string endpoint, string httpMethod, string requestData, int statusCode, long responseTimeMs, string? userAgent = null);
    Task<bool> IsRateLimitExceededAsync(string ipAddress, string? userId, int maxRequestsPerMinute = 60, int maxRequestsPerHour = 1000);
    Task CleanupOldLogsAsync(int retentionDays = 30);
}

public interface IDuplicatePreventionService
{
    Task<bool> IsDuplicateDataAsync<T>(T entity, string entityType, string uniqueKey) where T : class;
    Task LogDuplicateAttemptAsync(string entityType, string entityId, string originalData, string duplicateData, string userId, string ipAddress, string action, bool wasBlocked = true);
    Task<string> GenerateDataHashAsync<T>(T data) where T : class;
    Task<bool> HasRecentDuplicateAttemptsAsync(string userId, string ipAddress, TimeSpan timeWindow);
}

public interface IAuditService
{
    Task LogActionAsync(string tableName, string recordId, string action, string? userId, string? userName, string? ipAddress, object? oldValues = null, object? newValues = null, string? reason = null);
    Task<bool> IsSuspiciousActivityAsync(string userId, string ipAddress);
    Task MarkSuspiciousActivityAsync(string userId, string ipAddress, string reason);
}

public class SecurityService : IAntiSpamService, IDuplicatePreventionService, IAuditService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SecurityService> _logger;

    public SecurityService(ApplicationDbContext context, ILogger<SecurityService> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region Anti-Spam Service Implementation

    public async Task<bool> IsSpamRequestAsync(string ipAddress, string? userId, string endpoint, string requestData)
    {
        try
        {
            var requestHash = await GenerateDataHashAsync(requestData);
            var now = DateTime.UtcNow;
            var oneMinuteAgo = now.AddMinutes(-1);
            var oneHourAgo = now.AddHours(-1);

            // Check for exact duplicate requests in the last minute
            var duplicateCount = await _context.RequestLogs
                .Where(rl => rl.RequestHash == requestHash && 
                           rl.Timestamp >= oneMinuteAgo)
                .CountAsync();

            if (duplicateCount >= 5) // More than 5 identical requests in 1 minute
            {
                _logger.LogWarning("SPAM DETECTED: Duplicate request hash {RequestHash} from IP {IpAddress}, Count: {Count}",
                    requestHash, ipAddress, duplicateCount);
                return true;
            }

            // Check request frequency per IP
            var ipRequestCount = await _context.RequestLogs
                .Where(rl => rl.IpAddress == ipAddress && 
                           rl.Timestamp >= oneMinuteAgo)
                .CountAsync();

            if (ipRequestCount >= 100) // More than 100 requests per minute from same IP
            {
                _logger.LogWarning("SPAM DETECTED: High frequency requests from IP {IpAddress}, Count: {Count}",
                    ipAddress, ipRequestCount);
                return true;
            }

            // Check request frequency per user
            if (!string.IsNullOrEmpty(userId))
            {
                var userRequestCount = await _context.RequestLogs
                    .Where(rl => rl.UserId == userId && 
                               rl.Timestamp >= oneMinuteAgo)
                    .CountAsync();

                if (userRequestCount >= 200) // More than 200 requests per minute per user
                {
                    _logger.LogWarning("SPAM DETECTED: High frequency requests from User {UserId}, Count: {Count}",
                        userId, userRequestCount);
                    return true;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking spam for IP {IpAddress}", ipAddress);
            return false; // Fail open for availability
        }
    }

    public async Task LogRequestAsync(string ipAddress, string? userId, string endpoint, string httpMethod, 
        string requestData, int statusCode, long responseTimeMs, string? userAgent = null)
    {
        try
        {
            var requestHash = await GenerateDataHashAsync(requestData);
            var now = DateTime.UtcNow;
            var oneMinuteAgo = now.AddMinutes(-1);
            var oneHourAgo = now.AddHours(-1);

            // Count recent requests for analysis
            var requestsInLastMinute = await _context.RequestLogs
                .Where(rl => rl.IpAddress == ipAddress && rl.Timestamp >= oneMinuteAgo)
                .CountAsync();

            var requestsInLastHour = await _context.RequestLogs
                .Where(rl => rl.IpAddress == ipAddress && rl.Timestamp >= oneHourAgo)
                .CountAsync();

            var duplicateRequestCount = await _context.RequestLogs
                .Where(rl => rl.RequestHash == requestHash && rl.Timestamp >= oneHourAgo)
                .CountAsync();

            // Check if this is spam
            var isSpam = requestsInLastMinute >= 100 || duplicateRequestCount >= 10;
            var spamReason = isSpam ? 
                (requestsInLastMinute >= 100 ? "High frequency" : "Duplicate requests") : null;

            var requestLog = new RequestLog
            {
                IpAddress = ipAddress,
                UserId = userId,
                Endpoint = endpoint,
                HttpMethod = httpMethod,
                RequestHash = requestHash,
                Timestamp = now,
                UserAgent = userAgent,
                ResponseStatusCode = statusCode,
                ResponseTimeMs = responseTimeMs,
                IsSpamDetected = isSpam,
                SpamReason = spamReason,
                RequestsInLastMinute = requestsInLastMinute,
                RequestsInLastHour = requestsInLastHour,
                DuplicateRequestCount = duplicateRequestCount
            };

            _context.RequestLogs.Add(requestLog);
            await _context.SaveChangesAsync();

            if (isSpam)
            {
                _logger.LogWarning("SPAM REQUEST LOGGED: {Endpoint} from {IpAddress} - {SpamReason}",
                    endpoint, ipAddress, spamReason);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging request from IP {IpAddress}", ipAddress);
        }
    }

    public async Task<bool> IsRateLimitExceededAsync(string ipAddress, string? userId, int maxRequestsPerMinute = 60, int maxRequestsPerHour = 1000)
    {
        try
        {
            var now = DateTime.UtcNow;
            var oneMinuteAgo = now.AddMinutes(-1);
            var oneHourAgo = now.AddHours(-1);

            // Check IP-based rate limits
            var ipRequestsPerMinute = await _context.RequestLogs
                .Where(rl => rl.IpAddress == ipAddress && rl.Timestamp >= oneMinuteAgo)
                .CountAsync();

            var ipRequestsPerHour = await _context.RequestLogs
                .Where(rl => rl.IpAddress == ipAddress && rl.Timestamp >= oneHourAgo)
                .CountAsync();

            if (ipRequestsPerMinute >= maxRequestsPerMinute || ipRequestsPerHour >= maxRequestsPerHour)
            {
                return true;
            }

            // Check user-based rate limits if available
            if (!string.IsNullOrEmpty(userId))
            {
                var userRequestsPerMinute = await _context.RequestLogs
                    .Where(rl => rl.UserId == userId && rl.Timestamp >= oneMinuteAgo)
                    .CountAsync();

                var userRequestsPerHour = await _context.RequestLogs
                    .Where(rl => rl.UserId == userId && rl.Timestamp >= oneHourAgo)
                    .CountAsync();

                if (userRequestsPerMinute >= maxRequestsPerMinute * 2 || userRequestsPerHour >= maxRequestsPerHour * 2)
                {
                    return true;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking rate limit for IP {IpAddress}", ipAddress);
            return false;
        }
    }

    public async Task CleanupOldLogsAsync(int retentionDays = 30)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);

            var deletedCount = await _context.RequestLogs
                .Where(rl => rl.Timestamp < cutoffDate)
                .ExecuteDeleteAsync();

            _logger.LogInformation("Cleaned up {Count} old request logs older than {Days} days", 
                deletedCount, retentionDays);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up old request logs");
        }
    }

    #endregion

    #region Duplicate Prevention Service Implementation

    public async Task<bool> IsDuplicateDataAsync<T>(T entity, string entityType, string uniqueKey) where T : class
    {
        try
        {
            var dataHash = await GenerateDataHashAsync(entity);

            // Check for exact data hash match in recent attempts
            var recentDuplicate = await _context.DuplicateDetectionLogs
                .Where(ddl => ddl.DataHash == dataHash && 
                             ddl.EntityType == entityType &&
                             ddl.DetectedAt >= DateTime.UtcNow.AddHours(-24)) // Last 24 hours
                .FirstOrDefaultAsync();

            return recentDuplicate != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking duplicate data for entity type {EntityType}", entityType);
            return false;
        }
    }

    public async Task LogDuplicateAttemptAsync(string entityType, string entityId, string originalData, 
        string duplicateData, string userId, string ipAddress, string action, bool wasBlocked = true)
    {
        try
        {
            var dataHash = await GenerateDataHashAsync(duplicateData);

            var duplicateLog = new DuplicateDetectionLog
            {
                EntityType = entityType,
                EntityId = entityId,
                DataHash = dataHash,
                DetectedAt = DateTime.UtcNow,
                UserId = userId,
                Action = action,
                OriginalData = originalData,
                DuplicateData = duplicateData,
                IpAddress = ipAddress,
                DetectionMethod = "BUSINESS_LOGIC",
                WasBlocked = wasBlocked,
                Notes = $"Duplicate {entityType} attempt detected"
            };

            _context.DuplicateDetectionLogs.Add(duplicateLog);
            await _context.SaveChangesAsync();

            _logger.LogWarning("DUPLICATE ATTEMPT: {EntityType} {EntityId} by User {UserId} from IP {IpAddress} - {Action}",
                entityType, entityId, userId, ipAddress, action);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging duplicate attempt for {EntityType} {EntityId}", entityType, entityId);
        }
    }

    public async Task<string> GenerateDataHashAsync<T>(T data) where T : class
    {
        try
        {
            var jsonData = JsonSerializer.Serialize(data, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });

            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(jsonData));
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating data hash");
            return Guid.NewGuid().ToString("N"); // Fallback to unique string
        }
    }

    public async Task<bool> HasRecentDuplicateAttemptsAsync(string userId, string ipAddress, TimeSpan timeWindow)
    {
        try
        {
            var cutoffTime = DateTime.UtcNow.Subtract(timeWindow);

            var recentAttempts = await _context.DuplicateDetectionLogs
                .Where(ddl => (ddl.UserId == userId || ddl.IpAddress == ipAddress) &&
                             ddl.DetectedAt >= cutoffTime &&
                             ddl.WasBlocked)
                .CountAsync();

            return recentAttempts >= 5; // 5 or more blocked duplicate attempts
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking recent duplicate attempts for User {UserId}, IP {IpAddress}", 
                userId, ipAddress);
            return false;
        }
    }

    #endregion

    #region Audit Service Implementation

    public async Task LogActionAsync(string tableName, string recordId, string action, string? userId, 
        string? userName, string? ipAddress, object? oldValues = null, object? newValues = null, string? reason = null)
    {
        try
        {
            var auditLog = new AuditLog
            {
                TableName = tableName,
                RecordId = recordId,
                Action = action,
                Timestamp = DateTime.UtcNow,
                UserId = userId,
                UserName = userName,
                IpAddress = ipAddress,
                OldValues = oldValues != null ? JsonSerializer.Serialize(oldValues) : null,
                NewValues = newValues != null ? JsonSerializer.Serialize(newValues) : null,
                Reason = reason,
                RiskLevel = DetermineRiskLevel(action, tableName),
                ApplicationName = "SSS.BE"
            };

            // Determine if this is suspicious activity
            auditLog.IsSuspiciousActivity = await IsSuspiciousActivityAsync(userId ?? "ANONYMOUS", ipAddress ?? "UNKNOWN");

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            if (auditLog.IsSuspiciousActivity)
            {
                _logger.LogWarning("SUSPICIOUS ACTIVITY: {Action} on {TableName} {RecordId} by {UserId} from {IpAddress}",
                    action, tableName, recordId, userId ?? "ANONYMOUS", ipAddress ?? "UNKNOWN");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging audit action {Action} for {TableName} {RecordId}", 
                action, tableName, recordId);
        }
    }

    public async Task<bool> IsSuspiciousActivityAsync(string userId, string ipAddress)
    {
        try
        {
            var oneHourAgo = DateTime.UtcNow.AddHours(-1);

            // Check for high-frequency actions
            var recentActions = await _context.AuditLogs
                .Where(al => (al.UserId == userId || al.IpAddress == ipAddress) &&
                           al.Timestamp >= oneHourAgo)
                .CountAsync();

            // Check for blocked duplicate attempts
            var recentDuplicates = await _context.DuplicateDetectionLogs
                .Where(ddl => (ddl.UserId == userId || ddl.IpAddress == ipAddress) &&
                             ddl.DetectedAt >= oneHourAgo &&
                             ddl.WasBlocked)
                .CountAsync();

            // Check for spam requests
            var recentSpam = await _context.RequestLogs
                .Where(rl => (rl.UserId == userId || rl.IpAddress == ipAddress) &&
                           rl.Timestamp >= oneHourAgo &&
                           rl.IsSpamDetected)
                .CountAsync();

            return recentActions >= 100 || recentDuplicates >= 5 || recentSpam >= 10;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking suspicious activity for User {UserId}, IP {IpAddress}", 
                userId, ipAddress);
            return false;
        }
    }

    public async Task MarkSuspiciousActivityAsync(string userId, string ipAddress, string reason)
    {
        await LogActionAsync("SECURITY", $"{userId}:{ipAddress}", "SUSPICIOUS_ACTIVITY", 
            userId, null, ipAddress, null, new { Reason = reason }, reason);
    }

    #endregion

    #region Helper Methods

    private string DetermineRiskLevel(string action, string tableName)
    {
        // Critical actions
        if (action == "DELETE" || tableName.Contains("User") || tableName.Contains("Role"))
            return "HIGH";

        // Sensitive data modifications
        if (action == "UPDATE" && (tableName.Contains("Employee") || tableName.Contains("Department")))
            return "MEDIUM";

        // Read operations and regular creates
        return "LOW";
    }

    #endregion
}