using System.Globalization;

namespace SSS.BE.Infrastructure.Configuration;

public static class GlobalizationConfig
{
    public static readonly CultureInfo DefaultCulture = new("en-US");
    
    public static void ConfigureEnglish(this IServiceCollection services)
    {
        services.Configure<RequestLocalizationOptions>(options =>
        {
            var supportedCultures = new[]
            {
                DefaultCulture,
                new CultureInfo("en-GB") // Additional English variant
            };

            options.DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(DefaultCulture);
            options.SupportedCultures = supportedCultures;
            options.SupportedUICultures = supportedCultures;
            
            // Set culture providers priority
            options.RequestCultureProviders.Clear();
            options.RequestCultureProviders.Add(new Microsoft.AspNetCore.Localization.QueryStringRequestCultureProvider());
            options.RequestCultureProviders.Add(new Microsoft.AspNetCore.Localization.CookieRequestCultureProvider());
        });
    }

    public static void UseEnglish(this WebApplication app)
    {
        // Set default culture for the application
        CultureInfo.DefaultThreadCurrentCulture = DefaultCulture;
        CultureInfo.DefaultThreadCurrentUICulture = DefaultCulture;
        
        app.UseRequestLocalization();
    }

    /// <summary>
    /// Format number using US standard
    /// </summary>
    public static string FormatNumber(this decimal number)
    {
        return number.ToString("N2", DefaultCulture);
    }

    /// <summary>
    /// Format currency using US standard
    /// </summary>
    public static string FormatCurrency(this decimal amount)
    {
        return amount.ToString("C", DefaultCulture);
    }

    /// <summary>
    /// Format date using US standard
    /// </summary>
    public static string FormatDate(this DateTime date)
    {
        return date.ToString("MM/dd/yyyy", DefaultCulture);
    }

    /// <summary>
    /// Format date and time using US standard
    /// </summary>
    public static string FormatDateTime(this DateTime dateTime)
    {
        return dateTime.ToString("MM/dd/yyyy HH:mm:ss", DefaultCulture);
    }
}