using SSS.BE.Models.Employee;
using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;

namespace SSS.BE.Infrastructure.Middleware;

/// <summary>
/// Rate limiting middleware to prevent API abuse
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

        var rateLimitKey = $"{clientId}:{endpoint}";
        var client = _clients.GetOrAdd(rateLimitKey, _ => new ClientRateLimit());

        lock (client)
        {
            var now = DateTime.UtcNow;
            
            // Remove old requests outside the time window
            client.Requests.RemoveAll(r => now - r > _options.TimeWindow);
            
            // Check if rate limit exceeded
            if (client.Requests.Count >= _options.MaxRequests)
            {
                HandleRateLimitExceeded(context, clientId, endpoint);
                return;
            }
            
            // Add current request
            client.Requests.Add(now);
        }

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

    private string GetEndpoint(HttpContext context)
    {
        var method = context.Request.Method;
        var path = context.Request.Path.Value ?? "/";
        
        // Group similar endpoints (e.g., /api/employee/1, /api/employee/2 -> /api/employee/{id})
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < segments.Length; i++)
        {
            if (int.TryParse(segments[i], out _) || Guid.TryParse(segments[i], out _))
            {
                segments[i] = "{id}";
            }
        }
        
        return $"{method}:/{string.Join("/", segments)}";
    }

    private bool ShouldSkipRateLimit(string path)
    {
        var pathsToSkip = new[]
        {
            "/health",
            "/swagger",
            "/api-docs",
            "/.well-known"
        };

        // Skip OPTIONS requests (CORS preflight)
        if (path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return pathsToSkip.Any(skip => path.StartsWith(skip, StringComparison.OrdinalIgnoreCase));
    }

    private async void HandleRateLimitExceeded(HttpContext context, string clientId, string endpoint)
    {
        var requestId = context.Items["RequestId"]?.ToString() ?? Guid.NewGuid().ToString("N")[..8];
        
        _logger.LogWarning(
            "[{RequestId}] Rate limit exceeded for client {ClientId} on endpoint {Endpoint}. " +
            "Limit: {MaxRequests} requests per {TimeWindow}",
            requestId,
            clientId,
            endpoint,
            _options.MaxRequests,
            _options.TimeWindow
        );

        var response = context.Response;
        response.StatusCode = (int)HttpStatusCode.TooManyRequests;
        response.ContentType = "application/json";

        // Add rate limit headers
        response.Headers["X-RateLimit-Limit"] = _options.MaxRequests.ToString();
        response.Headers["X-RateLimit-Remaining"] = "0";
        response.Headers["X-RateLimit-Reset"] = DateTimeOffset.UtcNow.Add(_options.TimeWindow).ToUnixTimeSeconds().ToString();
        response.Headers["Retry-After"] = ((int)_options.TimeWindow.TotalSeconds).ToString();

        var errorResponse = new ApiResponse<object>
        {
            Success = false,
            Message = "Rate limit exceeded",
            Errors = new List<string>
            {
                $"Maximum {_options.MaxRequests} requests allowed per {_options.TimeWindow.TotalMinutes} minutes",
                $"Please try again in {_options.TimeWindow.TotalSeconds} seconds",
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
        if (DateTime.UtcNow.Second % 30 == 0) // Clean up every 30 seconds
        {
            var cutoff = DateTime.UtcNow - TimeSpan.FromMinutes(10); // Remove clients inactive for 10 minutes
            var keysToRemove = _clients
                .Where(kvp => kvp.Value.Requests.Count == 0 || kvp.Value.Requests.All(r => r < cutoff))
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in keysToRemove)
            {
                _clients.TryRemove(key, out _);
            }
        }
    }
}

/// <summary>
/// Rate limiting configuration options
/// </summary>
public class RateLimitOptions
{
    public int MaxRequests { get; set; } = 100;
    public TimeSpan TimeWindow { get; set; } = TimeSpan.FromMinutes(1);
}

/// <summary>
/// Client rate limit tracking
/// </summary>
internal class ClientRateLimit
{
    public List<DateTime> Requests { get; } = new();
}