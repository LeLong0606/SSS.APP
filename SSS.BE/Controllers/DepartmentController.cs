using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SSS.BE.Models.Employee;
using SSS.BE.Services.DepartmentService;

namespace SSS.BE.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DepartmentController : ControllerBase
{
    private readonly IDepartmentService _departmentService;
    private readonly ILogger<DepartmentController> _logger;

    public DepartmentController(IDepartmentService departmentService, ILogger<DepartmentController> logger)
    {
        _departmentService = departmentService;
        _logger = logger;
    }

    /// <summary>
    /// Get all departments with pagination (All authenticated users can view)
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResponse<DepartmentDto>>> GetDepartments(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] bool includeEmployees = false)
    {
        var result = await _departmentService.GetDepartmentsAsync(pageNumber, pageSize, search, includeEmployees);
        return Ok(result);
    }

    /// <summary>
    /// Get department by ID (All authenticated users can view)
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<DepartmentDto>>> GetDepartment(int id, [FromQuery] bool includeEmployees = false)
    {
        var result = await _departmentService.GetDepartmentByIdAsync(id, includeEmployees);
        
        if (!result.Success || result.Data == null)
        {
            return NotFound(new ApiResponse<DepartmentDto>
            {
                Success = false,
                Message = "Department not found"
            });
        }

        return Ok(result);
    }

    /// <summary>
    /// Create a new department (Administrator and Director can create)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Administrator,Director")]
    public async Task<ActionResult<ApiResponse<DepartmentDto>>> CreateDepartment([FromBody] CreateDepartmentRequest request)
    {
        try
        {
            var result = await _departmentService.CreateDepartmentAsync(request);
            
            if (!result.Success)
            {
                return BadRequest(result);
            }

            _logger.LogInformation("Department {DepartmentName} created successfully", request.Name);

            return CreatedAtAction(nameof(GetDepartment), new { id = result.Data!.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponse<DepartmentDto>
            {
                Success = false,
                Message = ex.Message,
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Update a department (Administrator and Director can update)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Administrator,Director")]
    public async Task<ActionResult<ApiResponse<DepartmentDto>>> UpdateDepartment(int id, [FromBody] UpdateDepartmentRequest request)
    {
        try
        {
            var result = await _departmentService.UpdateDepartmentAsync(id, request);
            
            if (!result.Success)
            {
                return BadRequest(result);
            }

            _logger.LogInformation("Department {DepartmentId} updated successfully", id);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message.Contains("not found"))
            {
                return NotFound(new ApiResponse<DepartmentDto>
                {
                    Success = false,
                    Message = ex.Message
                });
            }

            return BadRequest(new ApiResponse<DepartmentDto>
            {
                Success = false,
                Message = ex.Message,
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Delete a department - soft delete (Administrator only can delete)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteDepartment(int id)
    {
        try
        {
            var result = await _departmentService.DeleteDepartmentAsync(id);
            
            if (!result.Success)
            {
                return BadRequest(result);
            }

            _logger.LogInformation("Department {DepartmentId} deleted successfully", id);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message.Contains("not found"))
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = ex.Message
                });
            }

            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message,
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Get department employees (All authenticated users can view)
    /// </summary>
    [HttpGet("{id}/employees")]
    public async Task<ActionResult<PagedResponse<EmployeeDto>>> GetDepartmentEmployees(
        int id,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null)
    {
        try
        {
            var result = await _departmentService.GetDepartmentEmployeesAsync(id, pageNumber, pageSize, search);
            
            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new PagedResponse<EmployeeDto>
            {
                Success = false,
                Message = ex.Message,
                Data = new List<EmployeeDto>(),
                TotalCount = 0,
                PageNumber = pageNumber,
                PageSize = pageSize
            });
        }
    }

    /// <summary>
    /// Assign team leader to department (Administrator and Director can assign)
    /// </summary>
    [HttpPost("{id}/assign-team-leader")]
    [Authorize(Roles = "Administrator,Director")]
    public async Task<ActionResult<ApiResponse<object>>> AssignTeamLeader(int id, [FromBody] string employeeCode)
    {
        try
        {
            var result = await _departmentService.AssignTeamLeaderAsync(id, employeeCode);
            
            if (!result.Success)
            {
                return BadRequest(result);
            }

            _logger.LogInformation("Employee {EmployeeCode} assigned as team leader for department {DepartmentId}", 
                employeeCode, id);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message.Contains("not found"))
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = ex.Message
                });
            }

            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message,
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Remove team leader from department (Administrator and Director can remove)
    /// </summary>
    [HttpDelete("{id}/remove-team-leader")]
    [Authorize(Roles = "Administrator,Director")]
    public async Task<ActionResult<ApiResponse<object>>> RemoveTeamLeader(int id)
    {
        try
        {
            var result = await _departmentService.RemoveTeamLeaderAsync(id);
            
            if (!result.Success)
            {
                return BadRequest(result);
            }

            _logger.LogInformation("Team leader removed from department {DepartmentId}", id);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message.Contains("not found"))
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = ex.Message
                });
            }

            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message,
                Errors = new List<string> { ex.Message }
            });
        }
    }
}