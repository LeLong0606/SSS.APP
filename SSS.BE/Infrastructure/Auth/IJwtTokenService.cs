using SSS.BE.Infrastructure.Identity;

namespace SSS.BE.Infrastructure.Auth;

public interface IJwtTokenService
{
    Task<string> GenerateTokenAsync(ApplicationUser user);
    Task<string> GenerateTokenAsync(ApplicationUser user, IList<string> roles);
    string GenerateRefreshToken();
    Task<bool> ValidateRefreshTokenAsync(ApplicationUser user, string refreshToken);
    Task<string> GetRefreshTokenAsync(ApplicationUser user);
    Task SetRefreshTokenAsync(ApplicationUser user, string refreshToken);
    Task RemoveRefreshTokenAsync(ApplicationUser user);
}