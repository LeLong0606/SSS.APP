using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SSS.BE.Models.Employee;
using SSS.BE.Models.WorkShift;
using SSS.BE.Services.WorkShiftService;
using System.Security.Claims;

namespace SSS.BE.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class WorkShiftController : ControllerBase
{
    private readonly IWorkShiftService _workShiftService;
    private readonly ILogger<WorkShiftController> _logger;

    public WorkShiftController(IWorkShiftService workShiftService, ILogger<WorkShiftController> logger)
    {
        _workShiftService = workShiftService;
        _logger = logger;
    }

    private string GetCurrentEmployeeCode()
    {
        return User.FindFirst("EmployeeCode")?.Value ?? 
               User.FindFirst(ClaimTypes.Name)?.Value ?? 
               "SYSTEM";
    }

    private string[] GetUserRoles()
    {
        return User.Claims.Where(c => c.Type == "role").Select(c => c.Value).ToArray();
    }

    /// <summary>
    /// Get work shifts with filters (All authenticated users can view their department's shifts)
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResponse<WorkShiftDto>>> GetWorkShifts(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? employeeCode = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int? workLocationId = null,
        [FromQuery] bool includeInactive = false)
    {
        try
        {
            var currentEmployeeCode = GetCurrentEmployeeCode();
            var userRoles = GetUserRoles();

            var result = await _workShiftService.GetWorkShiftsAsync(
                currentEmployeeCode, userRoles, pageNumber, pageSize, 
                employeeCode, fromDate, toDate, workLocationId, includeInactive);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new PagedResponse<WorkShiftDto>
            {
                Success = false,
                Message = ex.Message,
                Data = new List<WorkShiftDto>(),
                TotalCount = 0,
                PageNumber = pageNumber,
                PageSize = pageSize
            });
        }
    }

    /// <summary>
    /// Get weekly shifts for an employee
    /// </summary>
    [HttpGet("weekly/{employeeCode}")]
    public async Task<ActionResult<ApiResponse<WeeklyShiftsDto>>> GetWeeklyShifts(
        string employeeCode, 
        [FromQuery] DateTime weekStartDate)
    {
        try
        {
            var currentEmployeeCode = GetCurrentEmployeeCode();
            var userRoles = GetUserRoles();

            var result = await _workShiftService.GetWeeklyShiftsAsync(currentEmployeeCode, userRoles, employeeCode, weekStartDate);
            
            if (!result.Success || result.Data == null)
            {
                return NotFound(new ApiResponse<WeeklyShiftsDto>
                {
                    Success = false,
                    Message = result.Message
                });
            }

            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponse<WeeklyShiftsDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
    }

    /// <summary>
    /// Validate shift timing and conflicts
    /// </summary>
    [HttpPost("validate")]
    public async Task<ActionResult<ShiftValidationResponse>> ValidateShift([FromBody] ShiftValidationRequest request)
    {
        try
        {
            var validation = await _workShiftService.ValidateShiftAsync(request);
            return Ok(validation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating shift for employee {EmployeeCode}", request.EmployeeCode);
            return StatusCode(500, new ShiftValidationResponse
            {
                IsValid = false,
                ValidationErrors = new List<string> { "An error occurred during validation" }
            });
        }
    }

    /// <summary>
    /// Create weekly shifts (TeamLeader and above can create for their department employees)
    /// </summary>
    [HttpPost("weekly")]
    [Authorize(Roles = "Administrator,Director,TeamLeader")]
    public async Task<ActionResult<ApiResponse<WeeklyShiftsDto>>> CreateWeeklyShifts([FromBody] CreateWeeklyShiftsRequest request)
    {
        try
        {
            var currentEmployeeCode = GetCurrentEmployeeCode();
            var userRoles = GetUserRoles();

            var result = await _workShiftService.CreateWeeklyShiftsAsync(currentEmployeeCode, userRoles, request);
            
            if (!result.Success)
            {
                return BadRequest(result);
            }

            _logger.LogInformation("Weekly shifts created for employee {EmployeeCode} by {AssignedBy}", 
                request.EmployeeCode, currentEmployeeCode);

            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponse<WeeklyShiftsDto>
            {
                Success = false,
                Message = ex.Message,
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Update a work shift (TeamLeader and above can update, Director+ can override)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Administrator,Director,TeamLeader")]
    public async Task<ActionResult<ApiResponse<WorkShiftDto>>> UpdateWorkShift(int id, [FromBody] UpdateWorkShiftRequest request)
    {
        try
        {
            var currentEmployeeCode = GetCurrentEmployeeCode();
            var userRoles = GetUserRoles();

            var result = await _workShiftService.UpdateWorkShiftAsync(currentEmployeeCode, userRoles, id, request);
            
            if (!result.Success)
            {
                return BadRequest(result);
            }

            _logger.LogInformation("Work shift {ShiftId} updated by {ModifiedBy}", id, currentEmployeeCode);

            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message.Contains("not found"))
            {
                return NotFound(new ApiResponse<WorkShiftDto>
                {
                    Success = false,
                    Message = ex.Message
                });
            }

            return BadRequest(new ApiResponse<WorkShiftDto>
            {
                Success = false,
                Message = ex.Message,
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Delete a work shift - soft delete (Director and above can delete)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Administrator,Director")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteWorkShift(int id, [FromBody] string? reason = null)
    {
        try
        {
            var currentEmployeeCode = GetCurrentEmployeeCode();
            var userRoles = GetUserRoles();

            var result = await _workShiftService.DeleteWorkShiftAsync(currentEmployeeCode, userRoles, id, reason);
            
            if (!result.Success)
            {
                return BadRequest(result);
            }

            _logger.LogInformation("Work shift {ShiftId} deleted by {DeletedBy}", id, currentEmployeeCode);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
    }

    /// <summary>
    /// Get work shift logs for audit trail (Director and above can view all, TeamLeader can view their department)
    /// </summary>
    [HttpGet("{id}/logs")]
    [Authorize(Roles = "Administrator,Director,TeamLeader")]
    public async Task<ActionResult<PagedResponse<WorkShiftLogDto>>> GetWorkShiftLogs(
        int id,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var currentEmployeeCode = GetCurrentEmployeeCode();
            var userRoles = GetUserRoles();

            var result = await _workShiftService.GetWorkShiftLogsAsync(currentEmployeeCode, userRoles, id, pageNumber, pageSize);
            
            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new PagedResponse<WorkShiftLogDto>
            {
                Success = false,
                Message = ex.Message,
                Data = new List<WorkShiftLogDto>(),
                TotalCount = 0,
                PageNumber = pageNumber,
                PageSize = pageSize
            });
        }
    }
}