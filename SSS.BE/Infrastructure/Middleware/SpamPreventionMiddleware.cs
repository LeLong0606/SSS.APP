using SSS.BE.Models.Employee;
using SSS.BE.Services.Security;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace SSS.BE.Infrastructure.Middleware;

/// <summary>
/// Advanced spam prevention middleware with database logging and pattern recognition
/// </summary>
public class SpamPreventionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SpamPreventionMiddleware> _logger;
    private readonly IServiceProvider _serviceProvider;

    // In-memory cache for quick lookups (with expiration)
    private static readonly Dictionary<string, SpamTracker> _ipTrackers = new();
    private static readonly Dictionary<string, SpamTracker> _userTrackers = new();
    private static readonly object _lockObject = new();
    private static DateTime _lastCleanup = DateTime.UtcNow;

    public SpamPreventionMiddleware(RequestDelegate next, ILogger<SpamPreventionMiddleware> logger, 
        IServiceProvider serviceProvider)
    {
        _next = next;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var requestId = context.Items["RequestId"]?.ToString() ?? Guid.NewGuid().ToString("N")[..8];
        var ipAddress = GetClientIpAddress(context);
        var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var endpoint = context.Request.Path.Value ?? "/";
        var httpMethod = context.Request.Method;
        var userAgent = context.Request.Headers.UserAgent.ToString();

        // Skip spam prevention for certain paths
        if (ShouldSkipSpamPrevention(endpoint))
        {
            await _next(context);
            return;
        }

        var startTime = DateTime.UtcNow;
        Exception? thrownException = null;
        int statusCode = 200;

        try
        {
            // 1. Quick in-memory spam check
            if (IsQuickSpamDetected(ipAddress, userId, endpoint))
            {
                await HandleSpamDetected(context, requestId, ipAddress, userId, "Quick spam detection");
                return;
            }

            // 2. Read request body for hash calculation
            var requestBody = await ReadRequestBodyAsync(context.Request);
            
            // 3. Advanced spam detection using database
            using var scope = _serviceProvider.CreateScope();
            var antiSpamService = scope.ServiceProvider.GetRequiredService<IAntiSpamService>();
            
            var isSpam = await antiSpamService.IsSpamRequestAsync(ipAddress, userId, endpoint, requestBody);
            if (isSpam)
            {
                await HandleSpamDetected(context, requestId, ipAddress, userId, "Database spam detection");
                
                // Log in database
                await antiSpamService.LogRequestAsync(ipAddress, userId, endpoint, httpMethod, 
                    requestBody, 429, 0, userAgent);
                return;
            }

            // 4. Update in-memory trackers
            UpdateSpamTrackers(ipAddress, userId);

            // 5. Process the request
            await _next(context);
            statusCode = context.Response.StatusCode;
        }
        catch (Exception ex)
        {
            thrownException = ex;
            statusCode = 500;
            throw;
        }
        finally
        {
            // Log request in database (async, fire-and-forget)
            var responseTime = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var antiSpamService = scope.ServiceProvider.GetRequiredService<IAntiSpamService>();
                    var requestBody = await ReadRequestBodyAsync(context.Request);
                    
                    await antiSpamService.LogRequestAsync(ipAddress, userId, endpoint, httpMethod, 
                        requestBody, statusCode, responseTime, userAgent);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[{RequestId}] Error logging request in spam prevention", requestId);
                }
            });

            // Cleanup old trackers periodically
            CleanupOldTrackers();
        }
    }

    private string GetClientIpAddress(HttpContext context)
    {
        // Check for forwarded headers (load balancers, proxies)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        // Fall back to connection remote IP
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private bool ShouldSkipSpamPrevention(string path)
    {
        var pathsToSkip = new[]
        {
            "/health",
            "/metrics",
            "/swagger",
            "/api-docs",
            "/.well-known",
            "/favicon.ico"
        };

        return pathsToSkip.Any(skip => path.StartsWith(skip, StringComparison.OrdinalIgnoreCase));
    }

    private bool IsQuickSpamDetected(string ipAddress, string? userId, string endpoint)
    {
        lock (_lockObject)
        {
            var now = DateTime.UtcNow;

            // Check IP-based spam
            if (_ipTrackers.TryGetValue(ipAddress, out var ipTracker))
            {
                // Remove old entries
                ipTracker.Requests.RemoveAll(r => now - r > TimeSpan.FromMinutes(1));
                
                // Check if too many requests in last minute
                if (ipTracker.Requests.Count >= 120) // 120 requests per minute = 2 per second
                {
                    _logger.LogWarning("QUICK SPAM DETECTED: IP {IpAddress} made {Count} requests in last minute",
                        ipAddress, ipTracker.Requests.Count);
                    return true;
                }

                ipTracker.Requests.Add(now);
            }
            else
            {
                _ipTrackers[ipAddress] = new SpamTracker { Requests = new List<DateTime> { now } };
            }

            // Check user-based spam if authenticated
            if (!string.IsNullOrEmpty(userId))
            {
                if (_userTrackers.TryGetValue(userId, out var userTracker))
                {
                    userTracker.Requests.RemoveAll(r => now - r > TimeSpan.FromMinutes(1));
                    
                    if (userTracker.Requests.Count >= 200) // 200 requests per minute for authenticated users
                    {
                        _logger.LogWarning("QUICK SPAM DETECTED: User {UserId} made {Count} requests in last minute",
                            userId, userTracker.Requests.Count);
                        return true;
                    }

                    userTracker.Requests.Add(now);
                }
                else
                {
                    _userTrackers[userId] = new SpamTracker { Requests = new List<DateTime> { now } };
                }
            }

            return false;
        }
    }

    private void UpdateSpamTrackers(string ipAddress, string? userId)
    {
        lock (_lockObject)
        {
            var now = DateTime.UtcNow;

            // Update IP tracker
            if (!_ipTrackers.ContainsKey(ipAddress))
            {
                _ipTrackers[ipAddress] = new SpamTracker();
            }
            _ipTrackers[ipAddress].LastSeen = now;

            // Update user tracker
            if (!string.IsNullOrEmpty(userId))
            {
                if (!_userTrackers.ContainsKey(userId))
                {
                    _userTrackers[userId] = new SpamTracker();
                }
                _userTrackers[userId].LastSeen = now;
            }
        }
    }

    private void CleanupOldTrackers()
    {
        var now = DateTime.UtcNow;
        
        // Clean up every 5 minutes
        if (now - _lastCleanup < TimeSpan.FromMinutes(5))
            return;

        lock (_lockObject)
        {
            _lastCleanup = now;
            var cutoff = now.AddMinutes(-10); // Remove trackers older than 10 minutes

            var oldIpKeys = _ipTrackers
                .Where(kvp => kvp.Value.LastSeen < cutoff)
                .Select(kvp => kvp.Key)
                .ToList();

            var oldUserKeys = _userTrackers
                .Where(kvp => kvp.Value.LastSeen < cutoff)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in oldIpKeys)
            {
                _ipTrackers.Remove(key);
            }

            foreach (var key in oldUserKeys)
            {
                _userTrackers.Remove(key);
            }

            if (oldIpKeys.Count > 0 || oldUserKeys.Count > 0)
            {
                _logger.LogDebug("Cleaned up {IpCount} IP trackers and {UserCount} user trackers",
                    oldIpKeys.Count, oldUserKeys.Count);
            }
        }
    }

    private async Task<string> ReadRequestBodyAsync(HttpRequest request)
    {
        if (request.ContentLength == 0 || request.ContentLength == null)
            return string.Empty;

        try
        {
            if (!request.Body.CanSeek)
                request.EnableBuffering();

            var originalPosition = request.Body.Position;
            request.Body.Position = 0;

            using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            
            request.Body.Position = originalPosition;
            
            // Limit body size for hashing (max 1KB for spam detection)
            return body.Length > 1024 ? body[..1024] : body;
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }

    private async Task HandleSpamDetected(HttpContext context, string requestId, string ipAddress, 
        string? userId, string reason)
    {
        _logger.LogWarning("[{RequestId}] SPAM DETECTED: {Reason} from IP {IpAddress}, User {UserId}",
            requestId, reason, ipAddress, userId ?? "Anonymous");

        var response = context.Response;
        response.StatusCode = (int)HttpStatusCode.TooManyRequests;
        response.ContentType = "application/json";

        // Add security headers
        response.Headers["X-Rate-Limit-Exceeded"] = "true";
        response.Headers["X-Spam-Detected"] = "true";
        response.Headers["Retry-After"] = "300"; // 5 minutes

        var errorResponse = new ApiResponse<object>
        {
            Success = false,
            Message = "Too many requests - spam protection activated",
            Errors = new List<string>
            {
                "Your request has been blocked due to suspicious activity",
                "Please wait 5 minutes before trying again",
                "If you believe this is an error, contact support",
                $"Request ID: {requestId}"
            }
        };

        var jsonResponse = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await response.WriteAsync(jsonResponse);
    }
}

/// <summary>
/// In-memory spam tracking data structure
/// </summary>
internal class SpamTracker
{
    public List<DateTime> Requests { get; set; } = new();
    public DateTime LastSeen { get; set; } = DateTime.UtcNow;
}