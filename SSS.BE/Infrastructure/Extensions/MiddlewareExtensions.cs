using SSS.BE.Infrastructure.Middleware;

namespace SSS.BE.Infrastructure.Extensions;

/// <summary>
/// Extension methods for configuring middleware
/// </summary>
public static class MiddlewareExtensions
{
    /// <summary>
    /// Add all custom middleware to the application builder based on configuration
    /// </summary>
    public static IApplicationBuilder UseCustomMiddleware(this IApplicationBuilder app, IConfiguration configuration)
    {
        var middlewareConfig = configuration.GetSection("Middleware");
        
        // ? FIXED: Only add middleware if explicitly enabled in configuration
        if (middlewareConfig.GetValue<bool>("EnableRequestValidation", true))
        {
            app.UseMiddleware<RequestValidationMiddleware>(GetRequestValidationOptions(configuration));
        }

        if (middlewareConfig.GetValue<bool>("EnableRateLimiting", true))
        {
            app.UseMiddleware<RateLimitingMiddleware>(GetRateLimitOptions(configuration));
        }

        // Global exception handling is always enabled (critical for production)
        app.UseMiddleware<GlobalExceptionMiddleware>();

        if (middlewareConfig.GetValue<bool>("EnableRequestLogging", true))
        {
            app.UseMiddleware<RequestLoggingMiddleware>();
        }

        if (middlewareConfig.GetValue<bool>("EnablePerformanceMonitoring", true))
        {
            app.UseMiddleware<PerformanceMonitoringMiddleware>();
        }

        // ? NEW: Spam prevention middleware (configurable)
        if (middlewareConfig.GetValue<bool>("EnableSpamPrevention", true))
        {
            app.UseMiddleware<SpamPreventionMiddleware>();
        }

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

    public static IApplicationBuilder UseSpamPrevention(this IApplicationBuilder app)
    {
        return app.UseMiddleware<SpamPreventionMiddleware>();
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
        var rateLimitSection = configuration.GetSection("RateLimit");
        
        if (rateLimitSection.Exists())
        {
            options.MaxRequests = rateLimitSection.GetValue<int>("MaxRequests", 100);
            
            // Handle TimeWindowMinutes from config
            var timeWindowMinutes = rateLimitSection.GetValue<int>("TimeWindowMinutes", 1);
            options.TimeWindow = TimeSpan.FromMinutes(timeWindowMinutes);
        }
        
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