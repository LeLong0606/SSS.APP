using SSS.BE.Models.Employee;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SSS.BE.Infrastructure.Middleware;

/// <summary>
/// Request validation middleware for security and data validation
/// </summary>
public class RequestValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestValidationMiddleware> _logger;
    private readonly RequestValidationOptions _options;

    // ? FIXED: More specific malicious patterns that won't match legitimate HTTP headers
    private static readonly string[] MaliciousPatterns = 
    {
        "<script", "javascript:", "vbscript:", "onload=", "onerror=",
        "DROP TABLE", "DELETE FROM", "UNION SELECT", 
        "xp_cmdshell", "sp_executesql", "EXEC(", "EXECUTE(",
        "../", "..\\", "/etc/passwd", "\\windows\\system32",
        "<?php", "<%", "eval(", "exec(",
        "base64_decode", "file_get_contents", "__FILE__", "__DIR__"
    };

    // ? FIXED: More specific dangerous SQL patterns
    private static readonly string[] SqlInjectionPatterns = 
    {
        @"\b(DROP|ALTER|CREATE)\s+TABLE\b",
        @"\bDELETE\s+FROM\b",
        @"\bUNION\s+SELECT\b",
        @"\bINSERT\s+INTO\b",
        @"\bUPDATE\s+SET\b",
        @"--\s",
        @"/\*.*\*/"
    };

    // Suspicious headers that might indicate malicious requests
    private static readonly string[] SuspiciousHeaders = 
    {
        "X-Forwarded-Host", "X-Originating-IP", "X-Remote-IP",
        "X-Remote-Addr", "X-ProxyUser-Ip", "X-Original-URL"
    };

    public RequestValidationMiddleware(RequestDelegate next, ILogger<RequestValidationMiddleware> logger, RequestValidationOptions options)
    {
        _next = next;
        _logger = logger;
        _options = options;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var requestId = context.Items["RequestId"]?.ToString() ?? Guid.NewGuid().ToString("N")[..8];
        
        try
        {
            // Skip validation for certain paths
            if (ShouldSkipValidation(context.Request.Path))
            {
                await _next(context);
                return;
            }

            // Validate request
            var validationResult = await ValidateRequestAsync(context, requestId);
            if (!validationResult.IsValid)
            {
                await HandleValidationFailure(context, requestId, validationResult);
                return;
            }

            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{RequestId}] Error in request validation middleware", requestId);
            throw;
        }
    }

    private bool ShouldSkipValidation(string path)
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

    private async Task<ValidationResult> ValidateRequestAsync(HttpContext context, string requestId)
    {
        var result = new ValidationResult { IsValid = true };
        var request = context.Request;

        // Skip validation for OPTIONS requests (CORS preflight)
        if (request.Method == "OPTIONS")
        {
            return result; // Always allow OPTIONS requests
        }

        // 1. Validate Content-Length
        if (request.ContentLength > _options.MaxRequestSize)
        {
            result.AddError($"Request size ({request.ContentLength} bytes) exceeds maximum allowed ({_options.MaxRequestSize} bytes)");
            _logger.LogWarning("[{RequestId}] Request size validation failed: {ContentLength} > {MaxSize}",
                requestId, request.ContentLength, _options.MaxRequestSize);
        }

        // 2. Validate Headers - ? FIXED: Only check for truly malicious content
        ValidateHeaders(request, result, requestId);

        // 3. Validate Query Parameters
        ValidateQueryParameters(request, result, requestId);

        // 4. Validate Request Body (only if request has content)
        if (request.ContentLength > 0 && 
            (request.Method == "POST" || request.Method == "PUT" || request.Method == "PATCH"))
        {
            await ValidateRequestBody(request, result, requestId);
        }

        // 5. Validate Content-Type for POST/PUT/PATCH requests
        if ((request.Method == "POST" || request.Method == "PUT" || request.Method == "PATCH"))
        {
            ValidateContentType(request, result, requestId);
        }

        return result;
    }

    // ? FIXED: Improved header validation that doesn't flag legitimate HTTP headers
    private void ValidateHeaders(HttpRequest request, ValidationResult result, string requestId)
    {
        foreach (var header in request.Headers)
        {
            var headerName = header.Key;
            var headerValues = header.Value.Where(v => v != null).ToArray();
            var headerValue = string.Join(",", headerValues);

            // ? FIX: Skip validation for standard HTTP headers that commonly contain patterns like */*
            if (IsStandardHttpHeader(headerName))
            {
                // Only check for extremely dangerous patterns in standard headers
                if (ContainsExtremelyDangerousPattern(headerValue))
                {
                    result.AddError($"Dangerous pattern detected in header '{headerName}'");
                    _logger.LogWarning("[{RequestId}] Dangerous pattern in header {HeaderName}: {HeaderValue}",
                        requestId, headerName, headerValue);
                }
            }
            else
            {
                // For non-standard headers, apply full validation
                if (ContainsMaliciousPattern(headerValue))
                {
                    result.AddError($"Malicious pattern detected in header '{headerName}'");
                    _logger.LogWarning("[{RequestId}] Malicious pattern in header {HeaderName}: {HeaderValue}",
                        requestId, headerName, headerValue);
                }
            }

            // Check for suspicious headers
            if (SuspiciousHeaders.Any(sh => sh.Equals(headerName, StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogWarning("[{RequestId}] Suspicious header detected: {HeaderName} = {HeaderValue}",
                    requestId, headerName, headerValue);
            }

            // Validate header value length
            if (headerValue.Length > _options.MaxHeaderValueLength)
            {
                result.AddError($"Header '{headerName}' value is too long ({headerValue.Length} > {_options.MaxHeaderValueLength} chars)");
            }
        }

        // Only require Content-Type header when the request actually has content
        if (request.Method == "POST" || request.Method == "PUT" || request.Method == "PATCH")
        {
            var hasContent = request.ContentLength > 0 || !string.IsNullOrEmpty(request.ContentType);
            var isAuthEndpointWithoutBody = IsAuthEndpointWithoutBody(request.Path, request.Method);
            
            if (_options.RequireContentTypeHeader && !isAuthEndpointWithoutBody && hasContent && string.IsNullOrEmpty(request.ContentType))
            {
                result.AddError("Content-Type header is required for this request");
            }
        }
    }

    // ? NEW: Check if header is a standard HTTP header that should have relaxed validation
    private bool IsStandardHttpHeader(string headerName)
    {
        var standardHeaders = new[]
        {
            "Accept", "Accept-Encoding", "Accept-Language", "Accept-Charset",
            "Authorization", "Content-Type", "Content-Length", "Content-Encoding",
            "User-Agent", "Referer", "Host", "Origin", "Cache-Control",
            "Connection", "Upgrade", "Sec-WebSocket-Key", "Sec-WebSocket-Version",
            "X-Requested-With", "If-Modified-Since", "If-None-Match"
        };

        return standardHeaders.Any(sh => sh.Equals(headerName, StringComparison.OrdinalIgnoreCase));
    }

    // ? NEW: Check for extremely dangerous patterns that shouldn't appear even in standard headers
    private bool ContainsExtremelyDangerousPattern(string input)
    {
        if (string.IsNullOrEmpty(input))
            return false;

        var dangerousPatterns = new[]
        {
            "<script", "javascript:", "vbscript:", "onload=", "onerror=",
            "<?php", "<%", "eval(", "exec(",
            "base64_decode", "file_get_contents"
        };

        var lowercaseInput = input.ToLowerInvariant();
        return dangerousPatterns.Any(pattern => lowercaseInput.Contains(pattern.ToLowerInvariant()));
    }

    private bool IsAuthEndpointWithoutBody(string path, string method)
    {
        var authEndpointsWithoutBody = new[]
        {
            ("/api/auth/logout", "POST"),
            // Add other endpoints that don't require body but are POST requests
        };

        return authEndpointsWithoutBody.Any(endpoint => 
            path.Equals(endpoint.Item1, StringComparison.OrdinalIgnoreCase) && 
            method.Equals(endpoint.Item2, StringComparison.OrdinalIgnoreCase));
    }

    private void ValidateQueryParameters(HttpRequest request, ValidationResult result, string requestId)
    {
        foreach (var param in request.Query)
        {
            var paramName = param.Key;
            var paramValues = param.Value.Where(v => v != null).ToArray();
            var paramValue = string.Join(",", paramValues);

            // Check for malicious patterns
            if (ContainsMaliciousPattern(paramName) || ContainsMaliciousPattern(paramValue))
            {
                result.AddError($"Malicious pattern detected in query parameter '{paramName}'");
                _logger.LogWarning("[{RequestId}] Malicious pattern in query param {ParamName}: {ParamValue}",
                    requestId, paramName, paramValue);
            }

            // Check parameter value length
            if (paramValue.Length > _options.MaxQueryParameterLength)
            {
                result.AddError($"Query parameter '{paramName}' value is too long ({paramValue.Length} > {_options.MaxQueryParameterLength} chars)");
            }
        }

        // Check total query string length
        var queryString = request.QueryString.ToString();
        if (queryString.Length > _options.MaxQueryStringLength)
        {
            result.AddError($"Query string is too long ({queryString.Length} > {_options.MaxQueryStringLength} chars)");
        }
    }

    private async Task ValidateRequestBody(HttpRequest request, ValidationResult result, string requestId)
    {
        if (!request.Body.CanSeek)
        {
            request.EnableBuffering();
        }

        var originalPosition = request.Body.Position;
        request.Body.Position = 0;

        using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        
        request.Body.Position = originalPosition;

        if (string.IsNullOrEmpty(body))
            return;

        // Check for malicious patterns in body
        if (ContainsMaliciousPattern(body))
        {
            result.AddError("Malicious pattern detected in request body");
            _logger.LogWarning("[{RequestId}] Malicious pattern detected in request body", requestId);
        }

        // Try to validate JSON structure if Content-Type suggests JSON
        if (request.ContentType?.Contains("application/json") == true)
        {
            try
            {
                JsonDocument.Parse(body);
            }
            catch (JsonException)
            {
                result.AddError("Invalid JSON format in request body");
            }
        }
    }

    private void ValidateContentType(HttpRequest request, ValidationResult result, string requestId)
    {
        if (string.IsNullOrEmpty(request.ContentType))
        {
            if (request.ContentLength == 0 || !request.ContentLength.HasValue)
            {
                return; // Allow requests without body to have no Content-Type
            }
        }

        var contentType = request.ContentType?.ToLowerInvariant();
        
        var allowedContentTypes = new[]
        {
            "application/json",
            "application/x-www-form-urlencoded",
            "multipart/form-data",
            "text/plain"
        };

        if (!string.IsNullOrEmpty(contentType) && !allowedContentTypes.Any(ct => contentType.StartsWith(ct)))
        {
            result.AddError($"Content-Type '{request.ContentType}' is not allowed");
            _logger.LogWarning("[{RequestId}] Disallowed Content-Type: {ContentType}", requestId, request.ContentType);
        }
    }

    // ? FIXED: More intelligent malicious pattern detection
    private bool ContainsMaliciousPattern(string input)
    {
        if (string.IsNullOrEmpty(input))
            return false;

        var lowercaseInput = input.ToLowerInvariant();
        
        // Check simple string patterns first
        if (MaliciousPatterns.Any(pattern => lowercaseInput.Contains(pattern.ToLowerInvariant())))
        {
            return true;
        }

        // Check SQL injection patterns using regex for more precise matching
        foreach (var pattern in SqlInjectionPatterns)
        {
            try
            {
                if (Regex.IsMatch(lowercaseInput, pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline))
                {
                    return true;
                }
            }
            catch (RegexMatchTimeoutException)
            {
                // If regex times out, skip this pattern
                continue;
            }
        }

        return false;
    }

    private async Task HandleValidationFailure(HttpContext context, string requestId, ValidationResult validationResult)
    {
        _logger.LogWarning("[{RequestId}] Request validation failed: {Errors}",
            requestId, string.Join(", ", validationResult.Errors));

        var response = context.Response;
        response.StatusCode = (int)HttpStatusCode.BadRequest;
        response.ContentType = "application/json";

        var errorResponse = new ApiResponse<object>
        {
            Success = false,
            Message = "Request validation failed",
            Errors = validationResult.Errors.ToList()
        };

        errorResponse.Errors.Add($"Request ID: {requestId}");

        var jsonResponse = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await response.WriteAsync(jsonResponse);
    }
}

/// <summary>
/// Request validation configuration options
/// </summary>
public class RequestValidationOptions
{
    public long MaxRequestSize { get; set; } = 10 * 1024 * 1024; // 10MB
    public int MaxHeaderValueLength { get; set; } = 4096; // 4KB
    public int MaxQueryParameterLength { get; set; } = 2048; // 2KB
    public int MaxQueryStringLength { get; set; } = 8192; // 8KB
    public bool RequireContentTypeHeader { get; set; } = true;
}

/// <summary>
/// Validation result
/// </summary>
internal class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; } = new();

    public void AddError(string error)
    {
        IsValid = false;
        Errors.Add(error);
    }
}