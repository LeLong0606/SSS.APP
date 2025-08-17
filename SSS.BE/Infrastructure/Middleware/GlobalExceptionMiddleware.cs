using SSS.BE.Models.Employee;
using System.Net;
using System.Text.Json;

namespace SSS.BE.Infrastructure.Middleware;

/// <summary>
/// Global exception handling middleware that provides consistent error responses
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger, IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var requestId = context.Items["RequestId"]?.ToString() ?? Guid.NewGuid().ToString("N")[..8];
        
        _logger.LogError(exception,
            "[{RequestId}] Unhandled exception occurred: {ExceptionType} - {Message} | Path: {Path} | User: {User}",
            requestId,
            exception.GetType().Name,
            exception.Message,
            context.Request.Path,
            context.User?.Identity?.Name ?? "Anonymous"
        );

        var response = context.Response;
        response.ContentType = "application/json";

        var errorResponse = new ApiResponse<object>
        {
            Success = false,
            Errors = new List<string>()
        };

        switch (exception)
        {
            case UnauthorizedAccessException:
                response.StatusCode = (int)HttpStatusCode.Forbidden;
                errorResponse.Message = "Access denied";
                errorResponse.Errors.Add("You don't have permission to access this resource");
                break;

            case ArgumentNullException argNullEx:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse.Message = "Invalid request";
                errorResponse.Errors.Add($"Required parameter '{argNullEx.ParamName}' is missing");
                break;

            case ArgumentException argEx:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse.Message = "Invalid request";
                errorResponse.Errors.Add(argEx.Message);
                break;

            case InvalidOperationException invOpEx:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse.Message = "Operation failed";
                errorResponse.Errors.Add(invOpEx.Message);
                break;

            case KeyNotFoundException:
                response.StatusCode = (int)HttpStatusCode.NotFound;
                errorResponse.Message = "Resource not found";
                errorResponse.Errors.Add("The requested resource was not found");
                break;

            case TimeoutException:
                response.StatusCode = (int)HttpStatusCode.RequestTimeout;
                errorResponse.Message = "Request timeout";
                errorResponse.Errors.Add("The request took too long to process");
                break;

            case TaskCanceledException:
                response.StatusCode = (int)HttpStatusCode.RequestTimeout;
                errorResponse.Message = "Request cancelled";
                errorResponse.Errors.Add("The request was cancelled or timed out");
                break;

            default:
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                errorResponse.Message = "An error occurred while processing your request";
                
                if (_environment.IsDevelopment())
                {
                    // In development, show detailed error information
                    errorResponse.Errors.Add($"Exception: {exception.GetType().Name}");
                    errorResponse.Errors.Add($"Message: {exception.Message}");
                    if (exception.InnerException != null)
                    {
                        errorResponse.Errors.Add($"Inner Exception: {exception.InnerException.Message}");
                    }
                    errorResponse.Errors.Add($"Stack Trace: {exception.StackTrace}");
                }
                else
                {
                    // In production, show generic error message
                    errorResponse.Errors.Add("Please try again later or contact support if the problem persists");
                }
                
                errorResponse.Errors.Add($"Request ID: {requestId} (Use this ID when contacting support)");
                break;
        }

        var jsonResponse = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = _environment.IsDevelopment()
        });

        await response.WriteAsync(jsonResponse);
    }
}