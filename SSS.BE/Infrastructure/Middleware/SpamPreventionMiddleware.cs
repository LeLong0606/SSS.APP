using SSS.BE.Models.Employee;
using SSS.BE.Services.Security;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace SSS.BE.Infrastructure.Middleware;

/// <summary>
/// Advanced spam prevention middleware with database logging and pattern recognition (Updated for less sensitivity)
/// </summary>
public class SpamPreventionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SpamPreventionMiddleware> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;

    // In-memory cache for quick lookups (with expiration)
    private static readonly Dictionary<string, SpamTracker> _ipTrackers = new();
    private static readonly Dictionary<string, SpamTracker> _userTrackers = new();
    private static readonly object _lockObject = new();
    private static DateTime _lastCleanup = DateTime.UtcNow;

    public SpamPreventionMiddleware(RequestDelegate next, ILogger<SpamPreventionMiddleware> logger, 
        IServiceProvider serviceProvider, IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        _serviceProvider = serviceProvider;
        _configuration = configuration;
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
            // ?? IMPROVEMENT: Less aggressive quick spam check
            if (IsQuickSpamDetected(context, ipAddress, userId, endpoint))
            {
                await HandleSpamDetected(context, requestId, ipAddress, userId, "Quick spam detection");
                return;
            }

            // ?? IMPROVEMENT: Skip database spam detection for GET requests from authenticated users
            if (!(httpMethod == "GET" && context.User?.Identity?.IsAuthenticated == true))
            {
                // 2. Read request body for hash calculation (only for non-GET requests)
                var requestBody = httpMethod != "GET" ? await ReadRequestBodyAsync(context.Request) : string.Empty;
                
                // 3. Advanced spam detection using database (less frequent checks)
                if (ShouldCheckDatabaseSpam(context))
                {
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
                }
            }

            // 4. Update in-memory trackers (less frequently)
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
            // ?? IMPROVEMENT: Log only important requests to reduce database load
            if (ShouldLogRequest(context, statusCode))
            {
                var responseTime = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
                _ = Task.Run(async () =>
                {
                    try
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var antiSpamService = scope.ServiceProvider.GetRequiredService<IAntiSpamService>();
                        var requestBody = httpMethod != "GET" ? await ReadRequestBodyAsync(context.Request) : string.Empty;
                        
                        await antiSpamService.LogRequestAsync(ipAddress, userId, endpoint, httpMethod, 
                            requestBody, statusCode, responseTime, userAgent);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[{RequestId}] Error logging request in spam prevention", requestId);
                    }
                });
            }

            // Cleanup old trackers less frequently
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
            "/favicon.ico",
            "/css/",
            "/js/",
            "/images/",
            "/assets/"
        };

        return pathsToSkip.Any(skip => path.StartsWith(skip, StringComparison.OrdinalIgnoreCase));
    }

    private bool IsQuickSpamDetected(HttpContext context, string ipAddress, string? userId, string endpoint)
    {
        // ?? IMPROVEMENT: Get configurable limits
        var config = _configuration.GetSection("Security:AntiSpam");
        var ipLimit = config.GetValue<int>("MaxRequestsPerMinutePerIP", 400);
        var userLimit = config.GetValue<int>("MaxRequestsPerMinutePerUser", 600);
        var windowMinutes = config.GetValue<int>("QuickDetectionWindowMinutes", 2);

        lock (_lockObject)
        {
            var now = DateTime.UtcNow;
            var timeWindow = TimeSpan.FromMinutes(windowMinutes);

            // ?? IMPROVEMENT: Much higher thresholds and longer time windows
            // Check IP-based spam with higher threshold
            if (_ipTrackers.TryGetValue(ipAddress, out var ipTracker))
            {
                // Remove old entries
                ipTracker.Requests.RemoveAll(r => now - r > timeWindow);
                
                // Check if too many requests in the time window
                if (ipTracker.Requests.Count >= ipLimit)
                {
                    // ?? IMPROVEMENT: Only treat as spam if it's really excessive
                    var recentRequests = ipTracker.Requests.Count(r => now - r < TimeSpan.FromSeconds(10));
                    if (recentRequests >= 50) // 50 requests in 10 seconds = definitely spam
                    {
                        _logger.LogWarning("SPAM DETECTED: IP {IpAddress} made {Count} requests in last {Window} minutes ({Recent} in last 10 seconds)",
                            ipAddress, ipTracker.Requests.Count, windowMinutes, recentRequests);
                        return true;
                    }
                }

                ipTracker.Requests.Add(now);
            }
            else
            {
                _ipTrackers[ipAddress] = new SpamTracker { Requests = new List<DateTime> { now } };
            }

            // Check user-based spam if authenticated (even more lenient)
            if (!string.IsNullOrEmpty(userId))
            {
                if (_userTrackers.TryGetValue(userId, out var userTracker))
                {
                    userTracker.Requests.RemoveAll(r => now - r > timeWindow);
                    
                    // ?? IMPROVEMENT: Very high threshold for authenticated users
                    if (userTracker.Requests.Count >= userLimit)
                    {
                        var recentRequests = userTracker.Requests.Count(r => now - r < TimeSpan.FromSeconds(10));
                        if (recentRequests >= 100) // 100 requests in 10 seconds for authenticated users
                        {
                            _logger.LogWarning("SPAM DETECTED: User {UserId} made {Count} requests in last {Window} minutes ({Recent} in last 10 seconds)",
                                userId, userTracker.Requests.Count, windowMinutes, recentRequests);
                            return true;
                        }
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

    private bool ShouldCheckDatabaseSpam(HttpContext context)
    {
        // ?? IMPROVEMENT: Skip database checks for authenticated GET requests
        if (context.Request.Method == "GET" && context.User?.Identity?.IsAuthenticated == true)
        {
            return false;
        }

        // ?? IMPROVEMENT: Only check every 5th request to reduce database load
        return DateTime.UtcNow.Millisecond % 5 == 0;
    }

    private bool ShouldLogRequest(HttpContext context, int statusCode)
    {
        // ?? IMPROVEMENT: Only log specific types of requests
        return statusCode >= 400 || // All error responses
               context.Request.Method != "GET" || // All non-GET requests
               DateTime.UtcNow.Second % 30 == 0; // Every 30th second for GET requests
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
        
        // ?? IMPROVEMENT: Clean up less frequently (every 10 minutes)
        if (now - _lastCleanup < TimeSpan.FromMinutes(10))
            return;

        lock (_lockObject)
        {
            _lastCleanup = now;
            var cutoff = now.AddMinutes(-30); // Remove trackers older than 30 minutes

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
        // ?? IMPROVEMENT: Shorter block duration
        var blockDuration = _configuration.GetSection("Security:AntiSpam").GetValue<int>("BlockDurationMinutes", 5);
        
        _logger.LogWarning("[{RequestId}] SPAM DETECTED: {Reason} from IP {IpAddress}, User {UserId} - Blocked for {Duration} minutes",
            requestId, reason, ipAddress, userId ?? "Anonymous", blockDuration);

        var response = context.Response;
        response.StatusCode = (int)HttpStatusCode.TooManyRequests;
        response.ContentType = "application/json";

        // Add security headers
        response.Headers["X-Rate-Limit-Exceeded"] = "true";
        response.Headers["X-Spam-Detected"] = "true";
        response.Headers["Retry-After"] = (blockDuration * 60).ToString(); // Convert minutes to seconds

        var errorResponse = new ApiResponse<object>
        {
            Success = false,
            Message = "Too many requests - please slow down",
            Errors = new List<string>
            {
                "Your request frequency is too high for our security systems",
                $"Please wait {blockDuration} minutes before trying again",
                "If you need to make many requests, consider using our bulk APIs",
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