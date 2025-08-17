using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SSS.BE.Models.Auth;
using SSS.BE.Services.AuthService;
using System.Security.Claims;

namespace SSS.BE.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Register a new user with selected role
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [Consumes("application/json")]
    [Produces("application/json")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request);
        
        if (!result.Success)
        {
            return BadRequest(result);
        }

        _logger.LogInformation("User {Email} registered successfully with role {Role}", 
            request.Email, request.Role);

        return Ok(result);
    }

    /// <summary>
    /// Login and generate JWT token
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [Consumes("application/json")]
    [Produces("application/json")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        
        if (!result.Success)
        {
            return Unauthorized(result);
        }

        _logger.LogInformation("User {Email} logged in successfully", request.Email);

        return Ok(result);
    }

    /// <summary>
    /// Logout and revoke token (No request body required)
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    [Produces("application/json")]
    public ActionResult<AuthResponse> Logout()
    {
        try
        {
            var result = _authService.LogoutAsync(User).Result;

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                        User.FindFirst("UserId")?.Value;

            _logger.LogInformation("User {UserId} logged out successfully", userId);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            
            return Ok(new AuthResponse 
            { 
                Success = true, 
                Message = "Logout completed"
            });
        }
    }

    /// <summary>
    /// Get current user information (Any authenticated user)
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [Produces("application/json")]
    public async Task<ActionResult<AuthResponse>> GetCurrentUser()
    {
        var result = await _authService.GetCurrentUserAsync(User);
        
        if (!result.Success)
        {
            if (result.Message.Contains("Invalid token"))
            {
                return Unauthorized(result);
            }
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Change password (Any authenticated user)
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    [Consumes("application/json")]
    [Produces("application/json")]
    public async Task<ActionResult<AuthResponse>> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var result = await _authService.ChangePasswordAsync(User, request);
        
        if (!result.Success)
        {
            if (result.Message.Contains("Invalid token"))
            {
                return Unauthorized(result);
            }
            if (result.Message.Contains("not found"))
            {
                return NotFound(result);
            }
            return BadRequest(result);
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                    User.FindFirst("UserId")?.Value;

        _logger.LogInformation("User {UserId} changed password successfully", userId);

        return Ok(result);
    }

    /// <summary>
    /// Get available roles (Director and Administrator can see all roles)
    /// </summary>
    [HttpGet("roles")]
    [Authorize(Roles = "Administrator,Director")]
    [Produces("application/json")]
    public async Task<ActionResult<IEnumerable<string>>> GetAvailableRoles()
    {
        var roles = await _authService.GetAvailableRolesAsync();
        return Ok(roles);
    }

    /// <summary>
    /// Test endpoint - Administrator access only
    /// </summary>
    [HttpGet("test/admin")]
    [Authorize(Roles = "Administrator")]
    [Produces("application/json")]
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
    [Produces("application/json")]
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
    [Produces("application/json")]
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
    [Produces("application/json")]
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
    [Produces("application/json")]
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
    [Produces("application/json")]
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