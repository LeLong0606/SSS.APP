using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SSS.BE.Infrastructure.Auth;
using SSS.BE.Infrastructure.Identity;
using SSS.BE.Models.Auth;
using SSS.BE.Services.Common;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace SSS.BE.Services.AuthService;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> LogoutAsync(ClaimsPrincipal user);
    Task<AuthResponse> GetCurrentUserAsync(ClaimsPrincipal user);
    Task<AuthResponse> ChangePasswordAsync(ClaimsPrincipal user, ChangePasswordRequest request);
    Task<IEnumerable<string>> GetAvailableRolesAsync();
}

public class AuthService : BaseService, IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ITokenRevocationService _tokenRevocationService;

    private readonly string[] _availableRoles = 
    {
        "Administrator",
        "Director", 
        "TeamLeader",
        "Employee"
    };

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        RoleManager<IdentityRole> roleManager,
        IJwtTokenService jwtTokenService,
        ITokenRevocationService tokenRevocationService,
        ILogger<AuthService> logger) : base(logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _jwtTokenService = jwtTokenService;
        _tokenRevocationService = tokenRevocationService;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        try
        {
            // Validate role
            if (!_availableRoles.Contains(request.Role))
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Invalid role",
                    Errors = new List<string> { $"Role must be one of: {string.Join(", ", _availableRoles)}" }
                };
            }

            // Check if user already exists
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Email already in use",
                    Errors = new List<string> { "This email is already registered" }
                };
            }

            // Check if EmployeeCode already exists (if provided)
            if (!string.IsNullOrEmpty(request.EmployeeCode))
            {
                var existingUserByCode = await _userManager.Users
                    .FirstOrDefaultAsync(u => u.EmployeeCode == request.EmployeeCode);
                if (existingUserByCode != null)
                {
                    return new AuthResponse
                    {
                        Success = false,
                        Message = "Employee code already in use",
                        Errors = new List<string> { "This employee code already exists" }
                    };
                }
            }

            // Create new user
            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                FullName = request.FullName,
                EmployeeCode = request.EmployeeCode,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Registration failed",
                    Errors = result.Errors.Select(e => e.Description).ToList()
                };
            }

            // Add role to user
            await _userManager.AddToRoleAsync(user, request.Role);

            // Generate JWT token
            var roles = await _userManager.GetRolesAsync(user);
            var token = await _jwtTokenService.GenerateTokenAsync(user, roles);

            return new AuthResponse
            {
                Success = true,
                Message = "Registration successful",
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddHours(24),
                User = new UserInfo
                {
                    Id = user.Id,
                    Email = user.Email!,
                    FullName = user.FullName!,
                    EmployeeCode = user.EmployeeCode,
                    Roles = roles.ToList(),
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for {Email}", request.Email);
            return new AuthResponse
            {
                Success = false,
                Message = "An error occurred during registration",
                Errors = new List<string> { "Please try again later" }
            };
        }
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null || !user.IsActive)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Invalid email or password"
                };
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
            if (!result.Succeeded)
            {
                var message = result.IsLockedOut ? "Account is locked" :
                             result.IsNotAllowed ? "Account is not activated" :
                             "Invalid email or password";

                return new AuthResponse
                {
                    Success = false,
                    Message = message
                };
            }

            // Generate JWT token
            var roles = await _userManager.GetRolesAsync(user);
            var token = await _jwtTokenService.GenerateTokenAsync(user, roles);

            return new AuthResponse
            {
                Success = true,
                Message = "Login successful",
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddHours(24),
                User = new UserInfo
                {
                    Id = user.Id,
                    Email = user.Email!,
                    FullName = user.FullName!,
                    EmployeeCode = user.EmployeeCode,
                    Roles = roles.ToList(),
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for {Email}", request.Email);
            return new AuthResponse
            {
                Success = false,
                Message = "An error occurred during login",
                Errors = new List<string> { "Please try again later" }
            };
        }
    }

    public async Task<AuthResponse> LogoutAsync(ClaimsPrincipal user)
    {
        try
        {
            var jti = user.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
            if (!string.IsNullOrEmpty(jti))
            {
                _tokenRevocationService.RevokeToken(jti);
            }

            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                        user.FindFirst("UserId")?.Value;

            return new AuthResponse
            {
                Success = true,
                Message = "Logout successful"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return new AuthResponse
            {
                Success = false,
                Message = "An error occurred during logout"
            };
        }
    }

    public async Task<AuthResponse> GetCurrentUserAsync(ClaimsPrincipal user)
    {
        try
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                        user.FindFirst("UserId")?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Invalid token"
                };
            }

            var applicationUser = await _userManager.FindByIdAsync(userId);
            if (applicationUser == null || !applicationUser.IsActive)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "User not found or deactivated"
                };
            }

            var roles = await _userManager.GetRolesAsync(applicationUser);

            return new AuthResponse
            {
                Success = true,
                Message = "User information retrieved successfully",
                User = new UserInfo
                {
                    Id = applicationUser.Id,
                    Email = applicationUser.Email!,
                    FullName = applicationUser.FullName!,
                    EmployeeCode = applicationUser.EmployeeCode,
                    Roles = roles.ToList(),
                    IsActive = applicationUser.IsActive,
                    CreatedAt = applicationUser.CreatedAt
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user information");
            return new AuthResponse
            {
                Success = false,
                Message = "An error occurred while retrieving user information"
            };
        }
    }

    public async Task<AuthResponse> ChangePasswordAsync(ClaimsPrincipal user, ChangePasswordRequest request)
    {
        try
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                        user.FindFirst("UserId")?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Invalid token"
                };
            }

            var applicationUser = await _userManager.FindByIdAsync(userId);
            if (applicationUser == null)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "User not found"
                };
            }

            var result = await _userManager.ChangePasswordAsync(applicationUser, request.CurrentPassword, request.NewPassword);
            if (!result.Succeeded)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Password change failed",
                    Errors = result.Errors.Select(e => e.Description).ToList()
                };
            }

            // Revoke all existing tokens for this user
            await _tokenRevocationService.RevokeAllUserTokensAsync(userId);

            return new AuthResponse
            {
                Success = true,
                Message = "Password changed successfully. Please login again."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password");
            return new AuthResponse
            {
                Success = false,
                Message = "An error occurred while changing password"
            };
        }
    }

    public async Task<IEnumerable<string>> GetAvailableRolesAsync()
    {
        await Task.CompletedTask; // Make it async
        return _availableRoles;
    }
}