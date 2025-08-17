using SSS.BE.Infrastructure.Middleware;

namespace SSS.BE.Infrastructure.Extensions;

/// <summary>
/// Extension methods for configuring middleware
/// </summary>
public static class MiddlewareExtensions
{
    /// <summary>
    /// Add all custom middleware to the application builder
    /// </summary>
    public static IApplicationBuilder UseCustomMiddleware(this IApplicationBuilder app, IConfiguration configuration)
    {
        // 1. Request validation (should be early in pipeline)
        app.UseMiddleware<RequestValidationMiddleware>(GetRequestValidationOptions(configuration));

        // 2. Rate limiting (before authentication)
        app.UseMiddleware<RateLimitingMiddleware>(GetRateLimitOptions(configuration));

        // 3. Global exception handling
        app.UseMiddleware<GlobalExceptionMiddleware>();

        // 4. Request logging (after exception handling)
        app.UseMiddleware<RequestLoggingMiddleware>();

        // 5. Performance monitoring (last, to measure total processing time)
        app.UseMiddleware<PerformanceMonitoringMiddleware>();

        return app;
    }

    /// <summary>
    /// Add individual middleware with custom configuration
    /// </summary>
    public static IApplicationBuilder UseRequestValidation(this IApplicationBuilder app, RequestValidationOptions? options = null)
    {
        return app.UseMiddleware<RequestValidationMiddleware>(options ?? new RequestValidationOptions());
    }

    public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder app, RateLimitOptions? options = null)
    {
        return app.UseMiddleware<RateLimitingMiddleware>(options ?? new RateLimitOptions());
    }

    public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder app)
    {
        return app.UseMiddleware<GlobalExceptionMiddleware>();
    }

    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RequestLoggingMiddleware>();
    }

    public static IApplicationBuilder UsePerformanceMonitoring(this IApplicationBuilder app)
    {
        return app.UseMiddleware<PerformanceMonitoringMiddleware>();
    }

    /// <summary>
    /// Get request validation options from configuration
    /// </summary>
    private static RequestValidationOptions GetRequestValidationOptions(IConfiguration configuration)
    {
        var options = new RequestValidationOptions();
        configuration.GetSection("RequestValidation").Bind(options);
        return options;
    }

    /// <summary>
    /// Get rate limiting options from configuration
    /// </summary>
    private static RateLimitOptions GetRateLimitOptions(IConfiguration configuration)
    {
        var options = new RateLimitOptions();
        configuration.GetSection("RateLimit").Bind(options);
        return options;
    }

    /// <summary>
    /// Configure middleware-specific services
    /// </summary>
    public static IServiceCollection AddCustomMiddleware(this IServiceCollection services, IConfiguration configuration)
    {
        // Register middleware options as singletons
        services.Configure<RequestValidationOptions>(configuration.GetSection("RequestValidation"));
        services.Configure<RateLimitOptions>(configuration.GetSection("RateLimit"));

        return services;
    }
}