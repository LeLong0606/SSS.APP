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
    private readonly ILogger<JwtTokenService> _logger;
    
    // Token provider names for AspNetUserTokens
    private const string JWT_TOKEN_PROVIDER = "SSS_JWT_PROVIDER";
    private const string REFRESH_TOKEN_NAME = "RefreshToken";
    private const string ACCESS_TOKEN_NAME = "AccessToken";

    public JwtTokenService(IConfiguration configuration, UserManager<ApplicationUser> userManager, ILogger<JwtTokenService> logger)
    {
        _configuration = configuration;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<string> GenerateTokenAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        return await GenerateTokenAsync(user, roles);
    }

    public async Task<string> GenerateTokenAsync(ApplicationUser user, IList<string> roles)
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
        
        // Store JWT token in AspNetUserTokens table
        await SetAccessTokenAsync(user, tokenString, expiry);
        
        return tokenString;
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    /// <summary>
    /// Validate refresh token from AspNetUserTokens table
    /// </summary>
    public async Task<bool> ValidateRefreshTokenAsync(ApplicationUser user, string refreshToken)
    {
        try
        {
            var storedToken = await _userManager.GetAuthenticationTokenAsync(user, JWT_TOKEN_PROVIDER, REFRESH_TOKEN_NAME);
            
            if (string.IsNullOrEmpty(storedToken))
            {
                _logger.LogWarning("No refresh token found for user {UserId}", user.Id);
                return false;
            }

            var isValid = storedToken == refreshToken;
            
            if (!isValid)
            {
                _logger.LogWarning("Invalid refresh token for user {UserId}", user.Id);
            }
            
            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating refresh token for user {UserId}", user.Id);
            return false;
        }
    }

    /// <summary>
    /// Get current refresh token from AspNetUserTokens table
    /// </summary>
    public async Task<string> GetRefreshTokenAsync(ApplicationUser user)
    {
        try
        {
            var refreshToken = await _userManager.GetAuthenticationTokenAsync(user, JWT_TOKEN_PROVIDER, REFRESH_TOKEN_NAME);
            return refreshToken ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting refresh token for user {UserId}", user.Id);
            return string.Empty;
        }
    }

    /// <summary>
    /// Store refresh token in AspNetUserTokens table
    /// </summary>
    public async Task SetRefreshTokenAsync(ApplicationUser user, string refreshToken)
    {
        try
        {
            // Remove existing refresh token first
            await _userManager.RemoveAuthenticationTokenAsync(user, JWT_TOKEN_PROVIDER, REFRESH_TOKEN_NAME);
            
            // Set new refresh token
            await _userManager.SetAuthenticationTokenAsync(user, JWT_TOKEN_PROVIDER, REFRESH_TOKEN_NAME, refreshToken);
            
            _logger.LogInformation("Refresh token set for user {UserId}", user.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting refresh token for user {UserId}", user.Id);
            throw;
        }
    }

    /// <summary>
    /// Remove refresh token from AspNetUserTokens table
    /// </summary>
    public async Task RemoveRefreshTokenAsync(ApplicationUser user)
    {
        try
        {
            // Remove refresh token
            await _userManager.RemoveAuthenticationTokenAsync(user, JWT_TOKEN_PROVIDER, REFRESH_TOKEN_NAME);
            
            // Remove access token as well
            await _userManager.RemoveAuthenticationTokenAsync(user, JWT_TOKEN_PROVIDER, ACCESS_TOKEN_NAME);
            
            _logger.LogInformation("Tokens removed for user {UserId}", user.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing tokens for user {UserId}", user.Id);
            throw;
        }
    }

    /// <summary>
    /// Store access token in AspNetUserTokens table (private method)
    /// </summary>
    private async Task SetAccessTokenAsync(ApplicationUser user, string accessToken, DateTime expiry)
    {
        try
        {
            // Remove existing access token first
            await _userManager.RemoveAuthenticationTokenAsync(user, JWT_TOKEN_PROVIDER, ACCESS_TOKEN_NAME);
            
            // Store token with expiry information
            var tokenData = $"{accessToken}|{expiry:yyyy-MM-ddTHH:mm:ssZ}";
            await _userManager.SetAuthenticationTokenAsync(user, JWT_TOKEN_PROVIDER, ACCESS_TOKEN_NAME, tokenData);
            
            _logger.LogInformation("Access token stored for user {UserId}, expires at {Expiry}", user.Id, expiry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing access token for user {UserId}", user.Id);
            // Don't throw here as token generation should still work
        }
    }

    /// <summary>
    /// Get stored access token from AspNetUserTokens table
    /// </summary>
    public async Task<(string token, DateTime expiry)?> GetAccessTokenAsync(ApplicationUser user)
    {
        try
        {
            var tokenData = await _userManager.GetAuthenticationTokenAsync(user, JWT_TOKEN_PROVIDER, ACCESS_TOKEN_NAME);
            
            if (string.IsNullOrEmpty(tokenData))
                return null;

            var parts = tokenData.Split('|');
            if (parts.Length != 2)
                return null;

            if (DateTime.TryParse(parts[1], out var expiry))
            {
                return (parts[0], expiry);
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting access token for user {UserId}", user.Id);
            return null;
        }
    }

    /// <summary>
    /// Check if stored access token is still valid
    /// </summary>
    public async Task<bool> IsAccessTokenValidAsync(ApplicationUser user)
    {
        var tokenInfo = await GetAccessTokenAsync(user);
        if (tokenInfo == null)
            return false;

        return DateTime.UtcNow < tokenInfo.Value.expiry;
    }
}