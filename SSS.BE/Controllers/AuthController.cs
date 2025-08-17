using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SSS.BE.Infrastructure.Auth;
using SSS.BE.Infrastructure.Identity;
using SSS.BE.Models.Auth;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace SSS.BE.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ITokenRevocationService _tokenRevocationService;
    private readonly ILogger<AuthController> _logger;

    // 4 basic roles in English
    private readonly string[] _availableRoles = 
    {
        "Administrator",
        "Director", 
        "TeamLeader",
        "Employee"
    };

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        RoleManager<IdentityRole> roleManager,
        IJwtTokenService jwtTokenService,
        ITokenRevocationService tokenRevocationService,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _jwtTokenService = jwtTokenService;
        _tokenRevocationService = tokenRevocationService;
        _logger = logger;
    }

    /// <summary>
    /// Register a new user with selected role
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        try
        {
            // Validate role
            if (!_availableRoles.Contains(request.Role))
            {
                return BadRequest(new AuthResponse
                {
                    Success = false,
                    Message = "Invalid role",
                    Errors = new List<string> { $"Role must be one of: {string.Join(", ", _availableRoles)}" }
                });
            }

            // Check if user already exists
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return BadRequest(new AuthResponse
                {
                    Success = false,
                    Message = "Email already in use",
                    Errors = new List<string> { "This email is already registered" }
                });
            }

            // Check if EmployeeCode already exists (if provided)
            if (!string.IsNullOrEmpty(request.EmployeeCode))
            {
                var existingUserByCode = await _userManager.Users
                    .FirstOrDefaultAsync(u => u.EmployeeCode == request.EmployeeCode);
                if (existingUserByCode != null)
                {
                    return BadRequest(new AuthResponse
                    {
                        Success = false,
                        Message = "Employee code already in use",
                        Errors = new List<string> { "This employee code already exists" }
                    });
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
                return BadRequest(new AuthResponse
                {
                    Success = false,
                    Message = "Registration failed",
                    Errors = result.Errors.Select(e => e.Description).ToList()
                });
            }

            // Add role to user
            await _userManager.AddToRoleAsync(user, request.Role);

            // Generate JWT token
            var roles = await _userManager.GetRolesAsync(user);
            var token = await _jwtTokenService.GenerateTokenAsync(user, roles);

            _logger.LogInformation("User {Email} registered successfully with role {Role}", 
                request.Email, request.Role);

            return Ok(new AuthResponse
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
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for {Email}", request.Email);
            return StatusCode(500, new AuthResponse
            {
                Success = false,
                Message = "An error occurred during registration",
                Errors = new List<string> { "Please try again later" }
            });
        }
    }

    /// <summary>
    /// Login and generate JWT token
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null || !user.IsActive)
            {
                return Unauthorized(new AuthResponse
                {
                    Success = false,
                    Message = "Invalid email or password"
                });
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
            if (!result.Succeeded)
            {
                var message = result.IsLockedOut ? "Account is locked" :
                             result.IsNotAllowed ? "Account is not activated" :
                             "Invalid email or password";

                return Unauthorized(new AuthResponse
                {
                    Success = false,
                    Message = message
                });
            }

            // Generate JWT token
            var roles = await _userManager.GetRolesAsync(user);
            var token = await _jwtTokenService.GenerateTokenAsync(user, roles);

            _logger.LogInformation("User {Email} logged in successfully", request.Email);

            return Ok(new AuthResponse
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
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for {Email}", request.Email);
            return StatusCode(500, new AuthResponse
            {
                Success = false,
                Message = "An error occurred during login",
                Errors = new List<string> { "Please try again later" }
            });
        }
    }

    /// <summary>
    /// Logout and revoke token
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public ActionResult<AuthResponse> Logout()
    {
        try
        {
            var jti = User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
            if (!string.IsNullOrEmpty(jti))
            {
                _tokenRevocationService.RevokeToken(jti);
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                        User.FindFirst("UserId")?.Value;

            _logger.LogInformation("User {UserId} logged out", userId);

            return Ok(new AuthResponse
            {
                Success = true,
                Message = "Logout successful"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return StatusCode(500, new AuthResponse
            {
                Success = false,
                Message = "An error occurred during logout"
            });
        }
    }

    /// <summary>
    /// Get current user information (Any authenticated user)
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<AuthResponse>> GetCurrentUser()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                        User.FindFirst("UserId")?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new AuthResponse
                {
                    Success = false,
                    Message = "Invalid token"
                });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || !user.IsActive)
            {
                return NotFound(new AuthResponse
                {
                    Success = false,
                    Message = "User not found or deactivated"
                });
            }

            var roles = await _userManager.GetRolesAsync(user);

            return Ok(new AuthResponse
            {
                Success = true,
                Message = "User information retrieved successfully",
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
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user information");
            return StatusCode(500, new AuthResponse
            {
                Success = false,
                Message = "An error occurred while retrieving user information"
            });
        }
    }

    /// <summary>
    /// Change password (Any authenticated user)
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    public async Task<ActionResult<AuthResponse>> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                        User.FindFirst("UserId")?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new AuthResponse
                {
                    Success = false,
                    Message = "Invalid token"
                });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new AuthResponse
                {
                    Success = false,
                    Message = "User not found"
                });
            }

            var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
            if (!result.Succeeded)
            {
                return BadRequest(new AuthResponse
                {
                    Success = false,
                    Message = "Password change failed",
                    Errors = result.Errors.Select(e => e.Description).ToList()
                });
            }

            // Revoke all existing tokens for this user
            await _tokenRevocationService.RevokeAllUserTokensAsync(userId);

            _logger.LogInformation("User {UserId} changed password successfully", userId);

            return Ok(new AuthResponse
            {
                Success = true,
                Message = "Password changed successfully. Please login again."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password");
            return StatusCode(500, new AuthResponse
            {
                Success = false,
                Message = "An error occurred while changing password"
            });
        }
    }

    /// <summary>
    /// Get available roles (Director and Administrator can see all roles)
    /// </summary>
    [HttpGet("roles")]
    [Authorize(Roles = "Administrator,Director")]
    public ActionResult<IEnumerable<string>> GetAvailableRoles()
    {
        return Ok(_availableRoles);
    }

    /// <summary>
    /// Test endpoint - Administrator access only
    /// </summary>
    [HttpGet("test/admin")]
    [Authorize(Roles = "Administrator")]
    public IActionResult TestAdminOnly()
    {
        return Ok(new { 
            message = "Administrator access confirmed", 
            user = User.Identity?.Name,
            role = "Administrator"
        });
    }

    /// <summary>
    /// Test endpoint - Director access only
    /// </summary>
    [HttpGet("test/director")]
    [Authorize(Roles = "Director")]
    public IActionResult TestDirectorOnly()
    {
        return Ok(new { 
            message = "Director access confirmed", 
            user = User.Identity?.Name,
            role = "Director"
        });
    }

    /// <summary>
    /// Test endpoint - TeamLeader access only
    /// </summary>
    [HttpGet("test/teamleader")]
    [Authorize(Roles = "TeamLeader")]
    public IActionResult TestTeamLeaderOnly()
    {
        return Ok(new { 
            message = "TeamLeader access confirmed", 
            user = User.Identity?.Name,
            role = "TeamLeader"
        });
    }

    /// <summary>
    /// Test endpoint - Employee access only
    /// </summary>
    [HttpGet("test/employee")]
    [Authorize(Roles = "Employee")]
    public IActionResult TestEmployeeOnly()
    {
        return Ok(new { 
            message = "Employee access confirmed", 
            user = User.Identity?.Name,
            role = "Employee"
        });
    }

    /// <summary>
    /// Test endpoint - Multiple roles (Administrator or Director)
    /// </summary>
    [HttpGet("test/admin-director")]
    [Authorize(Roles = "Administrator,Director")]
    public IActionResult TestAdminOrDirector()
    {
        var userRoles = User.Claims.Where(c => c.Type == "role").Select(c => c.Value).ToList();
        return Ok(new { 
            message = "Administrator or Director access confirmed", 
            user = User.Identity?.Name,
            roles = userRoles
        });
    }

    /// <summary>
    /// Test endpoint - Management roles (Administrator, Director, TeamLeader)
    /// </summary>
    [HttpGet("test/management")]
    [Authorize(Roles = "Administrator,Director,TeamLeader")]
    public IActionResult TestManagementRoles()
    {
        var userRoles = User.Claims.Where(c => c.Type == "role").Select(c => c.Value).ToList();
        return Ok(new { 
            message = "Management access confirmed", 
            user = User.Identity?.Name,
            roles = userRoles
        });
    }
}