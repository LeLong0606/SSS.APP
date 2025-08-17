using System.Diagnostics;
using System.Text;

namespace SSS.BE.Infrastructure.Middleware;

/// <summary>
/// Middleware for logging HTTP requests and responses with performance metrics
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestId = Guid.NewGuid().ToString("N")[..8];
        
        // Add request ID to context for tracing
        context.Items["RequestId"] = requestId;
        
        // Log request
        await LogRequestAsync(context, requestId);
        
        // Capture original response body stream
        var originalResponseBodyStream = context.Response.Body;
        
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;
        
        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            
            // Log response
            await LogResponseAsync(context, requestId, stopwatch.ElapsedMilliseconds);
            
            // Copy response back to original stream
            await responseBody.CopyToAsync(originalResponseBodyStream);
        }
    }

    private async Task LogRequestAsync(HttpContext context, string requestId)
    {
        try
        {
            var request = context.Request;
            var requestBody = string.Empty;

            // Read request body for POST/PUT requests
            if (request.ContentLength > 0 && 
                (request.Method == "POST" || request.Method == "PUT" || request.Method == "PATCH"))
            {
                request.EnableBuffering();
                var buffer = new byte[Convert.ToInt32(request.ContentLength)];
                await request.Body.ReadAsync(buffer, 0, buffer.Length);
                requestBody = Encoding.UTF8.GetString(buffer);
                request.Body.Position = 0; // Reset for next middleware
            }

            var userInfo = context.User?.Identity?.IsAuthenticated == true 
                ? $"User: {context.User.Identity.Name ?? "Unknown"}" 
                : "Anonymous";

            _logger.LogInformation(
                "[{RequestId}] REQUEST: {Method} {Path} | {UserInfo} | IP: {RemoteIP} | Body: {Body}",
                requestId,
                request.Method,
                request.Path + request.QueryString,
                userInfo,
                context.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                string.IsNullOrEmpty(requestBody) ? "Empty" : 
                    requestBody.Length > 500 ? requestBody[..500] + "..." : requestBody
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{RequestId}] Error logging request", requestId);
        }
    }

    private async Task LogResponseAsync(HttpContext context, string requestId, long elapsedMs)
    {
        try
        {
            var response = context.Response;
            var responseBody = string.Empty;

            // Read response body
            if (response.Body.CanSeek)
            {
                response.Body.Seek(0, SeekOrigin.Begin);
                responseBody = await new StreamReader(response.Body).ReadToEndAsync();
                response.Body.Seek(0, SeekOrigin.Begin);
            }

            var logLevel = response.StatusCode >= 400 ? LogLevel.Warning : LogLevel.Information;
            var statusEmoji = response.StatusCode switch
            {
                >= 200 and < 300 => "?",
                >= 400 and < 500 => "??",
                >= 500 => "?",
                _ => "??"
            };

            _logger.Log(logLevel,
                "[{RequestId}] RESPONSE: {StatusEmoji} {StatusCode} | {ElapsedMs}ms | Size: {ResponseSize}B | Body: {Body}",
                requestId,
                statusEmoji,
                response.StatusCode,
                elapsedMs,
                responseBody.Length,
                string.IsNullOrEmpty(responseBody) ? "Empty" : 
                    responseBody.Length > 300 ? responseBody[..300] + "..." : responseBody
            );

            // Log slow requests
            if (elapsedMs > 2000) // Requests taking more than 2 seconds
            {
                _logger.LogWarning(
                    "[{RequestId}] SLOW REQUEST: {Method} {Path} took {ElapsedMs}ms",
                    requestId,
                    context.Request.Method,
                    context.Request.Path,
                    elapsedMs
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{RequestId}] Error logging response", requestId);
        }
    }
}