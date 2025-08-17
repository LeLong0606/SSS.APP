using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SSS.BE.Infrastructure.Identity;
using SSS.BE.Infrastructure.Auth;
using SSS.BE.Infrastructure.Data;
using SSS.BE.Infrastructure.Configuration;
using SSS.BE.Persistence;
using System.Text;
using System.IdentityModel.Tokens.Jwt;

var builder = WebApplication.CreateBuilder(args);

// ===== Configure Globalization (English US) =====
builder.Services.ConfigureEnglish();

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Server=(localdb)\\mssqllocaldb;Database=SSSBE;Trusted_Connection=true;MultipleActiveResultSets=true";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

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

// Register services
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddSingleton<ITokenRevocationService, TokenRevocationService>();

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

var app = builder.Build();

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

// Add health check endpoint
app.MapGet("/health", () => new 
{ 
    Status = "Healthy", 
    Timestamp = DateTime.UtcNow.FormatDateTime(),
    Culture = System.Globalization.CultureInfo.CurrentCulture.Name,
    Version = "1.0.0"
}).WithTags("Health");

app.Run();