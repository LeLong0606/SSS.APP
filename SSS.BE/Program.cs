using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SSS.BE.Infrastructure.Identity;
using SSS.BE.Infrastructure.Auth;
using SSS.BE.Infrastructure.Data;
using SSS.BE.Infrastructure.Configuration;
using SSS.BE.Infrastructure.Extensions;
using SSS.BE.Persistence;
using SSS.BE.Services.AuthService;
using SSS.BE.Services.EmployeeService;
using SSS.BE.Services.DepartmentService;
using SSS.BE.Services.WorkLocationService;
using SSS.BE.Services.WorkShiftService;
using SSS.BE.Services.Security;
using SSS.BE.Services.Database;
using System.Text;
using System.IdentityModel.Tokens.Jwt;

var builder = WebApplication.CreateBuilder(args);

// ===== Configure Globalization (English US) =====
builder.Services.ConfigureEnglish();

// ===== Add Custom Middleware Services =====
builder.Services.AddCustomMiddleware(builder.Configuration);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Server=(localdb)\\mssqllocaldb;Database=SSSBE;Trusted_Connection=true;MultipleActiveResultSets=true";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorNumbersToAdd: null);
        sqlOptions.CommandTimeout(30);
    }));

// Add Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 6;
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false;
    
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = false;
    
    // Account lockout for security
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Add JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var key = Encoding.ASCII.GetBytes(jwtSettings["SecretKey"] ?? "YourSuperSecretKeyThatShouldBeAtLeast32Characters!");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"] ?? "SSS.BE",
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"] ?? "SSS.BE.Users",
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero,
        NameClaimType = JwtRegisteredClaimNames.Sub,
        RoleClaimType = "role"
    };
    
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = context =>
        {
            var tokenRevocationService = context.HttpContext.RequestServices.GetRequiredService<ITokenRevocationService>();
            var jti = context.Principal?.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
            
            if (!string.IsNullOrEmpty(jti) && tokenRevocationService.IsTokenRevoked(jti))
            {
                context.Fail("Token has been revoked");
            }
            
            return Task.CompletedTask;
        }
    };
});

// ===== Simple Role-Based Authorization =====
builder.Services.AddAuthorization();

// ===== Register Infrastructure Services =====
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddSingleton<ITokenRevocationService, TokenRevocationService>();

// ===== Register Security Services =====
builder.Services.AddScoped<IAntiSpamService, SecurityService>();
builder.Services.AddScoped<IDuplicatePreventionService, SecurityService>();
builder.Services.AddScoped<IAuditService, SecurityService>();
builder.Services.AddScoped<SecurityService>();

// ===== Register Database Services =====
builder.Services.AddScoped<IDatabaseOptimizationService, DatabaseOptimizationService>();

// ===== Register Business Services =====
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IDepartmentService, DepartmentService>();
builder.Services.AddScoped<IWorkLocationService, WorkLocationService>();
builder.Services.AddScoped<IWorkShiftService, WorkShiftService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ===== Swagger with JWT Authentication =====
builder.Services.AddSwaggerWithJwtAuth();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

// ===== Background Services for Maintenance =====
builder.Services.AddHostedService<DatabaseMaintenanceService>();

var app = builder.Build();

// ===== Use Custom Middleware Pipeline (BEFORE other middleware) =====
app.UseCustomMiddleware(builder.Configuration);

// ===== Add Spam Prevention Middleware =====
app.UseMiddleware<SSS.BE.Infrastructure.Middleware.SpamPreventionMiddleware>();

// ===== Use English Configuration =====
app.UseEnglish();

// Seed roles and admin user
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await DataSeeder.SeedAsync(services);
}

// ===== Default Swagger =====
app.UseSwaggerDefault();

app.UseHttpsRedirection();
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Add enhanced health check endpoint with system info
app.MapGet("/health", async (IServiceProvider services) => 
{
    using var scope = services.CreateScope();
    var dbHealth = scope.ServiceProvider.GetRequiredService<IDatabaseOptimizationService>();
    var healthReport = await dbHealth.GetDatabaseHealthAsync();
    
    return new 
    { 
        Status = healthReport.IsHealthy ? "Healthy" : "Degraded",
        Timestamp = DateTime.UtcNow.FormatDateTime(),
        Culture = System.Globalization.CultureInfo.CurrentCulture.Name,
        Version = "2.0.0",
        Environment = app.Environment.EnvironmentName,
        MemoryUsage = $"{GC.GetTotalMemory(false) / 1024 / 1024}MB",
        MachineName = Environment.MachineName,
        ProcessorCount = Environment.ProcessorCount,
        Uptime = $"{DateTime.UtcNow - System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime():hh\\:mm\\:ss}",
        Database = new
        {
            healthReport.IsHealthy,
            healthReport.HealthScore,
            healthReport.EmployeeCount,
            healthReport.DepartmentCount,
            healthReport.WorkShiftCount,
            healthReport.RecentSpamCount,
            healthReport.RecentDuplicateAttempts,
            healthReport.AverageResponseTime
        }
    };
}).WithTags("Health");

// Add system metrics endpoint (for monitoring)
app.MapGet("/metrics", () => new
{
    Memory = new
    {
        TotalMemoryMB = GC.GetTotalMemory(false) / 1024 / 1024,
        Gen0Collections = GC.CollectionCount(0),
        Gen1Collections = GC.CollectionCount(1),
        Gen2Collections = GC.CollectionCount(2)
    },
    System = new
    {
        ProcessorCount = Environment.ProcessorCount,
        WorkingSetMB = Environment.WorkingSet / 1024 / 1024,
        ThreadCount = System.Diagnostics.Process.GetCurrentProcess().Threads.Count
    },
    Application = new
    {
        Version = "2.0.0",
        Environment = app.Environment.EnvironmentName,
        StartTime = System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime(),
        Uptime = DateTime.UtcNow - System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime()
    }
}).WithTags("Monitoring")
  .RequireAuthorization("Administrator"); // Only administrators can access metrics

// Add database health endpoint (Admin only)
app.MapGet("/admin/database-health", async (IDatabaseOptimizationService dbService) =>
{
    var healthReport = await dbService.GetDatabaseHealthAsync();
    return Results.Ok(healthReport);
}).WithTags("Admin")
  .RequireAuthorization("Administrator");

// Add database optimization endpoint (Admin only)
app.MapPost("/admin/optimize-database", async (IDatabaseOptimizationService dbService) =>
{
    await dbService.OptimizeIndexesAsync();
    await dbService.AnalyzeTableStatisticsAsync();
    await dbService.CleanupOldDataAsync();
    
    return Results.Ok(new { Message = "Database optimization completed", Timestamp = DateTime.UtcNow });
}).WithTags("Admin")
  .RequireAuthorization("Administrator");

app.Run();

// ===== Background Service for Database Maintenance =====
public class DatabaseMaintenanceService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseMaintenanceService> _logger;

    public DatabaseMaintenanceService(IServiceProvider serviceProvider, ILogger<DatabaseMaintenanceService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromHours(6), stoppingToken); // Run every 6 hours

                using var scope = _serviceProvider.CreateScope();
                var dbService = scope.ServiceProvider.GetRequiredService<IDatabaseOptimizationService>();
                var antiSpamService = scope.ServiceProvider.GetRequiredService<IAntiSpamService>();

                _logger.LogInformation("Starting scheduled database maintenance...");

                // Cleanup old logs
                await antiSpamService.CleanupOldLogsAsync();
                await dbService.CleanupOldDataAsync();

                // Optimize indexes (once per day at 3 AM)
                if (DateTime.Now.Hour == 3)
                {
                    await dbService.OptimizeIndexesAsync();
                    await dbService.AnalyzeTableStatisticsAsync();
                }

                _logger.LogInformation("Scheduled database maintenance completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during scheduled database maintenance");
            }
        }
    }
}