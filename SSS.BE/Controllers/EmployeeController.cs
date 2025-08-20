using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SSS.BE.Models.Employee;
using SSS.BE.Services.EmployeeService;

namespace SSS.BE.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class EmployeeController : ControllerBase
{
    private readonly IEmployeeService _employeeService;
    private readonly ILogger<EmployeeController> _logger;

    public EmployeeController(IEmployeeService employeeService, ILogger<EmployeeController> logger)
    {
        _employeeService = employeeService;
        _logger = logger;
    }

    /// <summary>
    /// Get all employees with pagination (All authenticated users can view)
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResponse<EmployeeDto>>> GetEmployees(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] int? departmentId = null,
        [FromQuery] bool? isTeamLeader = null)
    {
        var result = await _employeeService.GetEmployeesAsync(pageNumber, pageSize, search, departmentId, isTeamLeader);
        return Ok(result);
    }

    /// <summary>
    /// Get employee by ID (All authenticated users can view)
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<EmployeeDto>>> GetEmployee(int id)
    {
        var result = await _employeeService.GetEmployeeByIdAsync(id);
        
        if (!result.Success || result.Data == null)
        {
            return NotFound(new ApiResponse<EmployeeDto>
            {
                Success = false,
                Message = "Employee not found"
            });
        }

        return Ok(result);
    }

    /// <summary>
    /// Get employee by employee code (All authenticated users can view)
    /// </summary>
    [HttpGet("code/{employeeCode}")]
    public async Task<ActionResult<ApiResponse<EmployeeDto>>> GetEmployeeByCode(string employeeCode)
    {
        var result = await _employeeService.GetEmployeeByCodeAsync(employeeCode);
        
        if (!result.Success || result.Data == null)
        {
            return NotFound(new ApiResponse<EmployeeDto>
            {
                Success = false,
                Message = "Employee not found"
            });
        }

        return Ok(result);
    }

    /// <summary>
    /// ?? Get employees without department assignment (All authenticated users can view)
    /// </summary>
    [HttpGet("unassigned")]
    public async Task<ActionResult<PagedResponse<EmployeeDto>>> GetUnassignedEmployees(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null)
    {
        var result = await _employeeService.GetUnassignedEmployeesAsync(pageNumber, pageSize, search);
        return Ok(result);
    }

    /// <summary>
    /// Create a new employee (Administrator, Director, TeamLeader can create)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Administrator,Director,TeamLeader")]
    public async Task<ActionResult<ApiResponse<EmployeeDto>>> CreateEmployee([FromBody] CreateEmployeeRequest request)
    {
        try
        {
            var result = await _employeeService.CreateEmployeeAsync(request);
            
            if (!result.Success)
            {
                return BadRequest(result);
            }

            _logger.LogInformation("Employee {EmployeeCode} created successfully", request.EmployeeCode);

            return CreatedAtAction(nameof(GetEmployee), new { id = result.Data!.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponse<EmployeeDto>
            {
                Success = false,
                Message = ex.Message,
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// ?? Assign employee to department after creation (Administrator, Director, TeamLeader can assign)
    /// </summary>
    [HttpPost("{employeeCode}/assign-department")]
    [Authorize(Roles = "Administrator,Director,TeamLeader")]
    public async Task<ActionResult<ApiResponse<EmployeeDto>>> AssignEmployeeToDepartment(
        string employeeCode, 
        [FromBody] AssignEmployeeToDepartmentRequest request)
    {
        try
        {
            var result = await _employeeService.AssignEmployeeToDepartmentAsync(employeeCode, request);
            
            if (!result.Success)
            {
                return BadRequest(result);
            }

            _logger.LogInformation("Employee {EmployeeCode} assigned to department {DepartmentId} as {Role}", 
                employeeCode, request.DepartmentId, request.SetAsTeamLeader ? "Team Leader" : "Member");

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message.Contains("not found"))
            {
                return NotFound(new ApiResponse<EmployeeDto>
                {
                    Success = false,
                    Message = ex.Message
                });
            }

            return BadRequest(new ApiResponse<EmployeeDto>
            {
                Success = false,
                Message = ex.Message,
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// ?? Update employee's department assignment (Administrator, Director, TeamLeader can update)
    /// </summary>
    [HttpPut("{employeeCode}/department")]
    [Authorize(Roles = "Administrator,Director,TeamLeader")]
    public async Task<ActionResult<ApiResponse<EmployeeDto>>> UpdateEmployeeDepartment(
        string employeeCode, 
        [FromBody] UpdateEmployeeDepartmentRequest request)
    {
        try
        {
            var result = await _employeeService.UpdateEmployeeDepartmentAsync(employeeCode, request);
            
            if (!result.Success)
            {
                return BadRequest(result);
            }

            _logger.LogInformation("Employee {EmployeeCode} department assignment updated", employeeCode);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message.Contains("not found"))
            {
                return NotFound(new ApiResponse<EmployeeDto>
                {
                    Success = false,
                    Message = ex.Message
                });
            }

            return BadRequest(new ApiResponse<EmployeeDto>
            {
                Success = false,
                Message = ex.Message,
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// ?? Remove employee from department (Administrator, Director, TeamLeader can remove)
    /// </summary>
    [HttpDelete("{employeeCode}/remove-department")]
    [Authorize(Roles = "Administrator,Director,TeamLeader")]
    public async Task<ActionResult<ApiResponse<EmployeeDto>>> RemoveEmployeeFromDepartment(string employeeCode)
    {
        try
        {
            var result = await _employeeService.RemoveEmployeeFromDepartmentAsync(employeeCode);
            
            if (!result.Success)
            {
                return BadRequest(result);
            }

            _logger.LogInformation("Employee {EmployeeCode} removed from department", employeeCode);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message.Contains("not found"))
            {
                return NotFound(new ApiResponse<EmployeeDto>
                {
                    Success = false,
                    Message = ex.Message
                });
            }

            return BadRequest(new ApiResponse<EmployeeDto>
            {
                Success = false,
                Message = ex.Message,
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Update an employee (Administrator, Director, TeamLeader can update)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Administrator,Director,TeamLeader")]
    public async Task<ActionResult<ApiResponse<EmployeeDto>>> UpdateEmployee(int id, [FromBody] UpdateEmployeeRequest request)
    {
        try
        {
            var result = await _employeeService.UpdateEmployeeAsync(id, request);
            
            if (!result.Success)
            {
                return BadRequest(result);
            }

            _logger.LogInformation("Employee {EmployeeId} updated successfully", id);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message.Contains("not found"))
            {
                return NotFound(new ApiResponse<EmployeeDto>
                {
                    Success = false,
                    Message = ex.Message
                });
            }

            return BadRequest(new ApiResponse<EmployeeDto>
            {
                Success = false,
                Message = ex.Message,
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Delete an employee - soft delete (Administrator and Director can delete)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Administrator,Director")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteEmployee(int id)
    {
        try
        {
            var result = await _employeeService.DeleteEmployeeAsync(id);
            
            if (!result.Success)
            {
                return BadRequest(result);
            }

            _logger.LogInformation("Employee {EmployeeId} deleted successfully", id);

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
    /// Activate/Deactivate employee (Administrator and Director can manage status)
    /// </summary>
    [HttpPatch("{id}/status")]
    [Authorize(Roles = "Administrator,Director")]
    public async Task<ActionResult<ApiResponse<object>>> ToggleEmployeeStatus(int id, [FromBody] bool isActive)
    {
        try
        {
            var result = await _employeeService.ToggleEmployeeStatusAsync(id, isActive);
            
            if (!result.Success)
            {
                return BadRequest(result);
            }

            _logger.LogInformation("Employee {EmployeeId} status changed to {Status}", id, isActive ? "Active" : "Inactive");

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
}