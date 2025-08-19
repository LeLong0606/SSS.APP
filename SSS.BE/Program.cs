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

// CORS - Updated for Frontend Integration with proper preflight handling
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:50503", "https://localhost:50503") // Angular frontend ports
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials()
               .SetPreflightMaxAge(TimeSpan.FromMinutes(10)); // Cache preflight for 10 minutes
    });
    
    // Keep AllowAll for development but use AllowFrontend in production
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader()
               .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
    });
});

// ===== Background Services for Maintenance =====
builder.Services.AddHostedService<DatabaseMaintenanceService>();

var app = builder.Build();

// ===== IMPORTANT: Enable Swagger BEFORE other middleware =====
app.UseSwaggerDefault();

// ===== CORS must be early in pipeline - AFTER Swagger =====
app.UseCors("AllowFrontend");

// ===== ADD STATIC FILES SUPPORT FOR SWAGGER CUSTOM CSS/JS =====
app.UseStaticFiles(); // Enable serving static files from wwwroot

// ===== Use Custom Middleware Pipeline (AFTER CORS & Static Files) =====
app.UseCustomMiddleware(builder.Configuration);

// ===== Add Spam Prevention Middleware =====
app.UseMiddleware<SSS.BE.Infrastructure.Middleware.SpamPreventionMiddleware>();

// ===== Use English Configuration =====
app.UseEnglish();

// ===== ?? CODE-FIRST DATABASE MIGRATION & AUTO SEEDING =====
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    
    try
    {
        logger.LogInformation("?? Starting SSS.BE with Code-First Database Migration & Auto Seeding...");
        
        // Get required services
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        
        // ===== 1. DATABASE MIGRATION =====
        logger.LogInformation("?? Ensuring database is created and migrated...");
        await context.Database.MigrateAsync(); // This will create database and run migrations
        logger.LogInformation("? Database migration completed successfully!");
        
        // ===== 2. COMPREHENSIVE DATA SEEDING =====
        logger.LogInformation("?? Starting comprehensive data seeding...");
        await DataSeeder.SeedAsync(context, userManager, roleManager, logger);
        logger.LogInformation("? Data seeding completed successfully!");
        
        // ===== 3. PRINT SUCCESS MESSAGE =====
        logger.LogInformation("?? SSS.BE started successfully with full attendance management system!");
        logger.LogInformation("?? Default Admin Account: EMP001@sss.company.com / Password@123");
        logger.LogInformation("?? Features Available:");
        logger.LogInformation("   ? Employee Management");
        logger.LogInformation("   ? Department Management");
        logger.LogInformation("   ? Work Shift Management");
        logger.LogInformation("   ? Attendance Management (NEW)");
        logger.LogInformation("   ? Image Management (NEW)");
        logger.LogInformation("   ? Payroll Periods (NEW)");
        logger.LogInformation("   ? Leave Requests (NEW)");
        logger.LogInformation("   ? Overtime Management (NEW)");
        logger.LogInformation("   ? Excel Export for TCHC (NEW)");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "? An error occurred during database migration or data seeding");
        logger.LogError("??  The application will continue to start, but some features may not work correctly");
        logger.LogError("?? Please check your database connection and ensure SQL Server is running");
        
        // Don't stop the application, just log the error
        // This allows the app to start even if database is temporarily unavailable
    }
}

app.UseHttpsRedirection();

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
        Version = "2.1.0 - Attendance Management",
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
        },
        Features = new
        {
            AttendanceManagement = true,
            ImageManagement = true,
            PayrollPeriods = true,
            LeaveRequests = true,
            OvertimeManagement = true,
            ExcelExport = true,
            SelfAttendance = true,
            FaceRecognition = false // Future feature
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
        Version = "2.1.0 - Attendance Management",
        Environment = app.Environment.EnvironmentName,
        StartTime = System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime(),
        Uptime = DateTime.UtcNow - System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime(),
        Features = new
        {
            AttendanceManagement = true,
            ImageManagement = true,
            PayrollExport = true,
            SelfAttendance = true
        }
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

// ===== ?? NEW: Attendance System Status Endpoint =====
app.MapGet("/attendance-system/status", async (ApplicationDbContext context) =>
{
    try
    {
        var stats = new
        {
            ShiftTemplates = await context.ShiftTemplates.CountAsync(st => st.IsActive),
            ActiveShiftAssignments = await context.ShiftAssignments.CountAsync(sa => sa.IsActive),
            TodayAttendanceEvents = await context.AttendanceEvents
                .CountAsync(ae => ae.EventDateTime.Date == DateTime.Today),
            OpenPayrollPeriods = await context.PayrollPeriods.CountAsync(pp => pp.Status == "OPEN"),
            PendingLeaveRequests = await context.LeaveRequests.CountAsync(lr => lr.ApprovalStatus == "PENDING"),
            TotalImageFiles = await context.ImageFiles.CountAsync(img => img.IsActive),
            EmployeePhotos = await context.EmployeePhotos.CountAsync(ep => ep.IsActive),
            AttendancePhotosToday = await context.AttendancePhotos
                .CountAsync(ap => ap.TakenAt.Date == DateTime.Today),
            SystemReady = true,
            LastSeeded = DateTime.UtcNow,
            Version = "2.1.0"
        };

        return Results.Ok(stats);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error getting attendance system status: {ex.Message}");
    }
}).WithTags("Attendance")
  .RequireAuthorization();

// ===== ?? DEBUG: Simple Test Endpoint =====
app.MapGet("/test", () => new
{
    Message = "API is working!",
    Timestamp = DateTime.UtcNow,
    Version = "2.1.0"
}).WithTags("Test")
  .AllowAnonymous(); // Allow anonymous access for testing

// ===== ?? DEBUG: Database Data Check Endpoint =====  
app.MapGet("/debug/database-check", async (ApplicationDbContext context) =>
{
    try
    {
        var dbStats = new
        {
            // Existing tables
            Departments = await context.Departments.CountAsync(),
            Employees = await context.Employees.CountAsync(),
            WorkLocations = await context.WorkLocations.CountAsync(),
            WorkShifts = await context.WorkShifts.CountAsync(),
            AspNetUsers = await context.Users.CountAsync(),
            AspNetRoles = await context.Roles.CountAsync(),
            
            // New attendance tables
            ShiftTemplates = await context.ShiftTemplates.CountAsync(),
            ShiftAssignments = await context.ShiftAssignments.CountAsync(),
            ShiftCalendars = await context.ShiftCalendars.CountAsync(),
            AttendanceEvents = await context.AttendanceEvents.CountAsync(),
            AttendanceDaily = await context.AttendanceDaily.CountAsync(),
            LeaveRequests = await context.LeaveRequests.CountAsync(),
            OvertimeRequests = await context.OvertimeRequests.CountAsync(),
            Holidays = await context.Holidays.CountAsync(),
            PayrollPeriods = await context.PayrollPeriods.CountAsync(),
            PayrollSummaries = await context.PayrollSummaries.CountAsync(),
            
            // Image management tables
            ImageFiles = await context.ImageFiles.CountAsync(),
            EmployeePhotos = await context.EmployeePhotos.CountAsync(),
            AttendancePhotos = await context.AttendancePhotos.CountAsync(),
            LeaveRequestAttachments = await context.LeaveRequestAttachments.CountAsync(),
            
            DatabaseConnection = context.Database.CanConnect() ? "? Connected" : "? Not Connected",
            LastMigration = context.Database.GetAppliedMigrations().LastOrDefault(),
            PendingMigrations = context.Database.GetPendingMigrations().ToList(),
            DatabaseProvider = context.Database.ProviderName,
            Timestamp = DateTime.UtcNow
        };

        return Results.Ok(dbStats);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Database check failed: {ex.Message}");
    }
}).WithTags("Debug")
  .AllowAnonymous(); // Allow anonymous access for debugging

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

                _logger.LogInformation("?? Starting scheduled database maintenance...");

                // Cleanup old logs
                await antiSpamService.CleanupOldLogsAsync();
                await dbService.CleanupOldDataAsync();

                // Optimize indexes (once per day at 3 AM)
                if (DateTime.Now.Hour == 3)
                {
                    await dbService.OptimizeIndexesAsync();
                    await dbService.AnalyzeTableStatisticsAsync();
                    _logger.LogInformation("???  Database indexes optimized");
                }

                _logger.LogInformation("? Scheduled database maintenance completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "? Error during scheduled database maintenance");
            }
        }
    }
}
