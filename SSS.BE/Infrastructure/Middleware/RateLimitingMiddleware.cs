using SSS.BE.Models.Employee;
using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;

namespace SSS.BE.Infrastructure.Middleware;

/// <summary>
/// Rate limiting middleware to prevent API abuse (Updated for less sensitivity)
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly RateLimitOptions _options;
    private static readonly ConcurrentDictionary<string, ClientRateLimit> _clients = new();

    public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger, RateLimitOptions options)
    {
        _next = next;
        _logger = logger;
        _options = options;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var clientId = GetClientId(context);
        var endpoint = GetEndpoint(context);
        
        // Skip rate limiting for health checks, swagger, and OPTIONS requests (CORS preflight)
        if (ShouldSkipRateLimit(context.Request.Path) || context.Request.Method == "OPTIONS")
        {
            await _next(context);
            return;
        }

        // ?? IMPROVEMENT: Use different rate limits for authenticated vs anonymous users
        var maxRequests = GetMaxRequestsForClient(context);
        var rateLimitKey = $"{clientId}:{endpoint}";
        var client = _clients.GetOrAdd(rateLimitKey, _ => new ClientRateLimit());

        bool rateLimitExceeded = false;
        lock (client)
        {
            var now = DateTime.UtcNow;
            
            // Remove old requests outside the time window
            client.Requests.RemoveAll(r => now - r > _options.TimeWindow);
            
            // Check if rate limit exceeded
            if (client.Requests.Count >= maxRequests)
            {
                rateLimitExceeded = true;
            }
            else
            {
                // Add current request
                client.Requests.Add(now);
            }
        }

        if (rateLimitExceeded)
        {
            // ?? IMPROVEMENT: Only log every 10th violation to reduce log spam
            if (client.Requests.Count % 10 == 0)
            {
                HandleRateLimitExceeded(context, clientId, endpoint, maxRequests);
            }
            else
            {
                await SendRateLimitResponse(context, maxRequests);
            }
            return;
        }

        // Add rate limit headers for transparency
        context.Response.OnStarting(() =>
        {
            var remaining = Math.Max(0, maxRequests - client.Requests.Count);
            context.Response.Headers["X-RateLimit-Limit"] = maxRequests.ToString();
            context.Response.Headers["X-RateLimit-Remaining"] = remaining.ToString();
            context.Response.Headers["X-RateLimit-Reset"] = DateTimeOffset.UtcNow.Add(_options.TimeWindow).ToUnixTimeSeconds().ToString();
            return Task.CompletedTask;
        });

        // Clean up old clients periodically
        CleanupOldClients();

        await _next(context);
    }

    private string GetClientId(HttpContext context)
    {
        // Try to get user ID first (for authenticated users)
        var userId = context.User?.Identity?.Name;
        if (!string.IsNullOrEmpty(userId))
        {
            return $"user:{userId}";
        }

        // Fall back to IP address
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        
        // Check if behind proxy
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            ipAddress = forwardedFor.Split(',')[0].Trim();
        }

        return $"ip:{ipAddress}";
    }

    private int GetMaxRequestsForClient(HttpContext context)
    {
        // ?? IMPROVEMENT: Higher limits for authenticated users
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            // Authenticated users get 2x the base limit
            return _options.MaxRequests * 2;
        }

        // ?? IMPROVEMENT: Special handling for GET requests (less restrictive)
        if (context.Request.Method.Equals("GET", StringComparison.OrdinalIgnoreCase))
        {
            return (int)(_options.MaxRequests * 1.5);
        }

        return _options.MaxRequests;
    }

    private string GetEndpoint(HttpContext context)
    {
        var method = context.Request.Method;
        var path = context.Request.Path.Value ?? "/";
        
        // ?? IMPROVEMENT: Group similar endpoints more intelligently
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < segments.Length; i++)
        {
            if (int.TryParse(segments[i], out _) || Guid.TryParse(segments[i], out _))
            {
                segments[i] = "{id}";
            }
        }

        // ?? IMPROVEMENT: Don't differentiate between similar read operations too strictly
        var normalizedPath = $"/{string.Join("/", segments)}";
        if (method == "GET" && normalizedPath.Contains("/api/"))
        {
            // Group all GET requests to the same controller
            var pathParts = normalizedPath.Split('/');
            if (pathParts.Length >= 3)
            {
                return $"{method}:/{pathParts[1]}/{pathParts[2]}/*";
            }
        }
        
        return $"{method}:{normalizedPath}";
    }

    private bool ShouldSkipRateLimit(string path)
    {
        var pathsToSkip = new[]
        {
            "/health",
            "/swagger",
            "/api-docs",
            "/.well-known",
            "/favicon.ico"
        };

        return pathsToSkip.Any(skip => path.StartsWith(skip, StringComparison.OrdinalIgnoreCase));
    }

    private void HandleRateLimitExceeded(HttpContext context, string clientId, string endpoint, int maxRequests)
    {
        var requestId = context.Items["RequestId"]?.ToString() ?? Guid.NewGuid().ToString("N")[..8];
        
        _logger.LogWarning(
            "[{RequestId}] Rate limit exceeded for client {ClientId} on endpoint {Endpoint}. " +
            "Limit: {MaxRequests} requests per {TimeWindow}",
            requestId,
            clientId,
            endpoint,
            maxRequests,
            _options.TimeWindow
        );
    }

    private async Task SendRateLimitResponse(HttpContext context, int maxRequests)
    {
        var requestId = context.Items["RequestId"]?.ToString() ?? Guid.NewGuid().ToString("N")[..8];
        
        var response = context.Response;
        response.StatusCode = (int)HttpStatusCode.TooManyRequests;
        response.ContentType = "application/json";

        // Add rate limit headers
        response.Headers["X-RateLimit-Limit"] = maxRequests.ToString();
        response.Headers["X-RateLimit-Remaining"] = "0";
        response.Headers["X-RateLimit-Reset"] = DateTimeOffset.UtcNow.Add(_options.TimeWindow).ToUnixTimeSeconds().ToString();
        response.Headers["Retry-After"] = ((int)_options.TimeWindow.TotalSeconds).ToString();

        var errorResponse = new ApiResponse<object>
        {
            Success = false,
            Message = "Rate limit exceeded",
            Errors = new List<string>
            {
                $"Maximum {maxRequests} requests allowed per {_options.TimeWindow.TotalMinutes} minutes",
                $"Please wait {_options.TimeWindow.TotalSeconds} seconds before trying again",
                $"Request ID: {requestId}"
            }
        };

        var jsonResponse = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await response.WriteAsync(jsonResponse);
    }

    private void CleanupOldClients()
    {
        if (DateTime.UtcNow.Second % 60 == 0) // Clean up every minute instead of 30 seconds
        {
            var cutoff = DateTime.UtcNow - TimeSpan.FromMinutes(15); // Remove clients inactive for 15 minutes
            var keysToRemove = _clients
                .Where(kvp => kvp.Value.Requests.Count == 0 || kvp.Value.Requests.All(r => r < cutoff))
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in keysToRemove)
            {
                _clients.TryRemove(key, out _);
            }

            if (keysToRemove.Count > 0)
            {
                _logger.LogDebug("Cleaned up {Count} inactive rate limit clients", keysToRemove.Count);
            }
        }
    }
}

/// <summary>
/// Rate limiting configuration options
/// </summary>
public class RateLimitOptions
{
    public int MaxRequests { get; set; } = 300; // Increased from 100 to 300
    public TimeSpan TimeWindow { get; set; } = TimeSpan.FromMinutes(1);
}

/// <summary>
/// Client rate limit tracking
/// </summary>
internal class ClientRateLimit
{
    public List<DateTime> Requests { get; } = new();
}