using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using SSS.BE.Infrastructure.Identity;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace SSS.BE.Infrastructure.Auth;

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;
    private readonly UserManager<ApplicationUser> _userManager;

    public JwtTokenService(IConfiguration configuration, UserManager<ApplicationUser> userManager)
    {
        _configuration = configuration;
        _userManager = userManager;
    }

    public async Task<string> GenerateTokenAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        return await GenerateTokenAsync(user, roles);
    }

    public Task<string> GenerateTokenAsync(ApplicationUser user, IList<string> roles)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            jwtSettings["SecretKey"] ?? "YourSuperSecretKeyThatShouldBeAtLeast32Characters!"));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var jti = Guid.NewGuid().ToString();
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Jti, jti),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.UniqueName, user.UserName ?? string.Empty),
            new("FullName", user.FullName ?? string.Empty),
            new("EmployeeCode", user.EmployeeCode ?? string.Empty),
            new("UserId", user.Id)
        };

        // Add role claims
        foreach (var role in roles)
        {
            claims.Add(new Claim("role", role));
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var expiry = DateTime.UtcNow.AddHours(double.Parse(jwtSettings["ExpiryInHours"] ?? "24"));

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"] ?? "SSS.BE",
            audience: jwtSettings["Audience"] ?? "SSS.BE.Users",
            claims: claims,
            expires: expiry,
            signingCredentials: credentials
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        return Task.FromResult(tokenString);
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }
}