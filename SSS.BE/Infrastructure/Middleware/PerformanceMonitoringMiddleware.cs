using System.Diagnostics;

namespace SSS.BE.Infrastructure.Middleware;

/// <summary>
/// Performance monitoring middleware that tracks system metrics
/// </summary>
public class PerformanceMonitoringMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PerformanceMonitoringMiddleware> _logger;
    private static readonly Dictionary<string, PerformanceMetrics> _endpointMetrics = new();
    private static readonly object _lock = new();
    private static DateTime _lastCleanup = DateTime.UtcNow;

    public PerformanceMonitoringMiddleware(RequestDelegate next, ILogger<PerformanceMonitoringMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var endpoint = GetEndpointName(context);
        var requestId = context.Items["RequestId"]?.ToString() ?? Guid.NewGuid().ToString("N")[..8];

        Exception? thrownException = null;
        
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            thrownException = ex;
            throw;
        }
        finally
        {
            stopwatch.Stop();
            var elapsedMs = stopwatch.ElapsedMilliseconds;
            var statusCode = context.Response.StatusCode;
            var success = thrownException == null && statusCode < 400;

            // Update metrics
            UpdateMetrics(endpoint, elapsedMs, success);

            // Log performance warnings
            LogPerformanceWarnings(context, requestId, endpoint, elapsedMs, statusCode);

            // Add performance headers for debugging
            AddPerformanceHeaders(context, elapsedMs);

            // Periodic cleanup and reporting
            await PeriodicMaintenanceAsync(context.RequestServices);
        }
    }

    private string GetEndpointName(HttpContext context)
    {
        var method = context.Request.Method;
        var path = context.Request.Path.Value ?? "/";
        
        // Normalize paths with IDs
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < segments.Length; i++)
        {
            if (int.TryParse(segments[i], out _) || Guid.TryParse(segments[i], out _))
            {
                segments[i] = "{id}";
            }
        }
        
        return $"{method} /{string.Join("/", segments)}";
    }

    private void UpdateMetrics(string endpoint, long elapsedMs, bool success)
    {
        lock (_lock)
        {
            if (!_endpointMetrics.ContainsKey(endpoint))
            {
                _endpointMetrics[endpoint] = new PerformanceMetrics();
            }

            var metrics = _endpointMetrics[endpoint];
            metrics.TotalRequests++;
            metrics.TotalDurationMs += elapsedMs;
            
            if (success)
                metrics.SuccessfulRequests++;
            else
                metrics.FailedRequests++;

            metrics.MinDurationMs = Math.Min(metrics.MinDurationMs, elapsedMs);
            metrics.MaxDurationMs = Math.Max(metrics.MaxDurationMs, elapsedMs);
            
            // Update recent requests for rolling average
            metrics.RecentRequests.Add(new RequestMetric 
            { 
                Timestamp = DateTime.UtcNow, 
                DurationMs = elapsedMs,
                Success = success
            });

            // Keep only last 100 requests for rolling metrics
            if (metrics.RecentRequests.Count > 100)
            {
                metrics.RecentRequests.RemoveAt(0);
            }
        }
    }

    private void LogPerformanceWarnings(HttpContext context, string requestId, string endpoint, long elapsedMs, int statusCode)
    {
        // Log slow requests
        if (elapsedMs > 2000)
        {
            _logger.LogWarning(
                "[{RequestId}] SLOW REQUEST: {Endpoint} took {ElapsedMs}ms (Status: {StatusCode})",
                requestId, endpoint, elapsedMs, statusCode
            );
        }
        else if (elapsedMs > 5000)
        {
            _logger.LogError(
                "[{RequestId}] VERY SLOW REQUEST: {Endpoint} took {ElapsedMs}ms (Status: {StatusCode})",
                requestId, endpoint, elapsedMs, statusCode
            );
        }

        // Log memory usage warnings
        var memoryUsage = GC.GetTotalMemory(false);
        if (memoryUsage > 500_000_000) // 500MB
        {
            _logger.LogWarning(
                "[{RequestId}] HIGH MEMORY USAGE: {MemoryMB}MB after processing {Endpoint}",
                requestId, memoryUsage / 1024 / 1024, endpoint
            );
        }
    }

    private void AddPerformanceHeaders(HttpContext context, long elapsedMs)
    {
        if (!context.Response.HasStarted)
        {
            context.Response.Headers.Add("X-Response-Time", $"{elapsedMs}ms");
            context.Response.Headers.Add("X-Server-Time", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
            
            var memoryMB = GC.GetTotalMemory(false) / 1024 / 1024;
            context.Response.Headers.Add("X-Memory-Usage", $"{memoryMB}MB");
        }
    }

    private async Task PeriodicMaintenanceAsync(IServiceProvider serviceProvider)
    {
        var now = DateTime.UtcNow;
        
        // Run maintenance every 5 minutes
        if (now - _lastCleanup > TimeSpan.FromMinutes(5))
        {
            _lastCleanup = now;
            
            lock (_lock)
            {
                // Clean up old metrics
                CleanupOldMetrics(now);
                
                // Log performance summary
                LogPerformanceSummary();
            }

            // Force garbage collection if memory usage is high
            var memoryUsage = GC.GetTotalMemory(false);
            if (memoryUsage > 300_000_000) // 300MB
            {
                _logger.LogInformation("High memory usage detected ({MemoryMB}MB), forcing garbage collection", 
                    memoryUsage / 1024 / 1024);
                
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                
                var afterGC = GC.GetTotalMemory(false);
                _logger.LogInformation("Garbage collection completed. Memory usage: {BeforeMB}MB -> {AfterMB}MB", 
                    memoryUsage / 1024 / 1024, afterGC / 1024 / 1024);
            }
        }
    }

    private void CleanupOldMetrics(DateTime now)
    {
        var cutoff = now - TimeSpan.FromMinutes(30); // Keep metrics for last 30 minutes
        
        foreach (var metrics in _endpointMetrics.Values)
        {
            metrics.RecentRequests.RemoveAll(r => r.Timestamp < cutoff);
        }

        // Remove endpoints with no recent activity
        var inactiveEndpoints = _endpointMetrics
            .Where(kvp => kvp.Value.RecentRequests.Count == 0)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var endpoint in inactiveEndpoints)
        {
            _endpointMetrics.Remove(endpoint);
        }
    }

    private void LogPerformanceSummary()
    {
        if (_endpointMetrics.Count == 0) return;

        _logger.LogInformation("=== PERFORMANCE SUMMARY ===");
        
        foreach (var (endpoint, metrics) in _endpointMetrics.OrderByDescending(x => x.Value.TotalRequests))
        {
            if (metrics.RecentRequests.Count == 0) continue;

            var avgDuration = metrics.RecentRequests.Average(r => r.DurationMs);
            var successRate = (double)metrics.RecentRequests.Count(r => r.Success) / metrics.RecentRequests.Count * 100;
            var recentCount = metrics.RecentRequests.Count;

            _logger.LogInformation(
                "?? {Endpoint}: {RecentCount} reqs, Avg: {AvgMs:F1}ms, Min: {MinMs}ms, Max: {MaxMs}ms, Success: {SuccessRate:F1}%",
                endpoint, recentCount, avgDuration, metrics.MinDurationMs, metrics.MaxDurationMs, successRate
            );
        }

        var totalMemory = GC.GetTotalMemory(false) / 1024 / 1024;
        _logger.LogInformation("?? Memory Usage: {MemoryMB}MB", totalMemory);
        _logger.LogInformation("==============================");
    }
}

/// <summary>
/// Performance metrics for an endpoint
/// </summary>
internal class PerformanceMetrics
{
    public long TotalRequests { get; set; }
    public long SuccessfulRequests { get; set; }
    public long FailedRequests { get; set; }
    public long TotalDurationMs { get; set; }
    public long MinDurationMs { get; set; } = long.MaxValue;
    public long MaxDurationMs { get; set; }
    public List<RequestMetric> RecentRequests { get; set; } = new();

    public double AverageDurationMs => TotalRequests > 0 ? (double)TotalDurationMs / TotalRequests : 0;
    public double SuccessRate => TotalRequests > 0 ? (double)SuccessfulRequests / TotalRequests * 100 : 0;
}

/// <summary>
/// Individual request metric
/// </summary>
internal class RequestMetric
{
    public DateTime Timestamp { get; set; }
    public long DurationMs { get; set; }
    public bool Success { get; set; }
}