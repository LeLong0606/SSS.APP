using SSS.BE.Infrastructure.Identity;

namespace SSS.BE.Infrastructure.Auth;

public interface IJwtTokenService
{
    Task<string> GenerateTokenAsync(ApplicationUser user, IList<string> roles);
    string GenerateRefreshToken();
    Task<string> GenerateTokenAsync(ApplicationUser user);
}