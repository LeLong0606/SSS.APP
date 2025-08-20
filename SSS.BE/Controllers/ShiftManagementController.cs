using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SSS.BE.Models.Attendance;
using SSS.BE.Models.Employee;

namespace SSS.BE.Controllers;

/// <summary>
/// Controller for work shift management and shift assignment for employees
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class ShiftManagementController : ControllerBase
{
    private readonly ILogger<ShiftManagementController> _logger;

    public ShiftManagementController(ILogger<ShiftManagementController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get work shift templates list
    /// </summary>
    [HttpGet("templates")]
    public async Task<ActionResult<PagedResponse<ShiftTemplateDto>>> GetShiftTemplates(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool? isActive = true)
    {
        try
        {
            // TODO: Implement shift management service
            var result = new PagedResponse<ShiftTemplateDto>
            {
                Success = true,
                Message = "Shift templates retrieved successfully",
                Data = new List<ShiftTemplateDto>(),
                TotalCount = 0,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting shift templates");
            return StatusCode(500, new PagedResponse<ShiftTemplateDto>
            {
                Success = false,
                Message = "Error retrieving work shift templates",
                Data = new List<ShiftTemplateDto>(),
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Create new work shift template (Admin, Director)
    /// </summary>
    [HttpPost("templates")]
    [Authorize(Roles = "Administrator,Director")]
    public async Task<ActionResult<ApiResponse<ShiftTemplateDto>>> CreateShiftTemplate([FromBody] CreateShiftTemplateRequest request)
    {
        try
        {
            // TODO: Implement shift management service
            var result = new ApiResponse<ShiftTemplateDto>
            {
                Success = true,
                Message = "Work shift template created successfully",
                Data = new ShiftTemplateDto
                {
                    Id = 1,
                    Name = request.Name,
                    Code = request.Code,
                    StartTime = request.StartTime,
                    EndTime = request.EndTime,
                    StandardHours = request.StandardHours,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            };

            _logger.LogInformation("Shift template {Name} created successfully", request.Name);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating shift template");
            return StatusCode(500, new ApiResponse<ShiftTemplateDto>
            {
                Success = false,
                Message = "Error creating work shift template",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Get employee's shift calendar by month (personal)
    /// </summary>
    [HttpGet("my-calendar")]
    public async Task<ActionResult<ApiResponse<object>>> GetMyShiftCalendar(
        [FromQuery] int year = 0,
        [FromQuery] int month = 0)
    {
        try
        {
            var employeeCode = GetCurrentEmployeeCode();
            var targetYear = year > 0 ? year : DateTime.Now.Year;
            var targetMonth = month > 0 ? month : DateTime.Now.Month;
            
            // TODO: Implement shift management service
            var result = new ApiResponse<object>
            {
                Success = true,
                Message = "Work shift calendar retrieved successfully",
                Data = new 
                { 
                    EmployeeCode = employeeCode,
                    Year = targetYear,
                    Month = targetMonth,
                    Calendar = new List<object>()
                }
            };
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting employee shift calendar");
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "Error retrieving work shift calendar",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    private string GetCurrentEmployeeCode()
    {
        return User.FindFirst("EmployeeCode")?.Value ?? 
               User.Identity?.Name ?? "UNKNOWN";
    }
}