using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SSS.BE.Domain.Entities;
using SSS.BE.Models.Employee;
using SSS.BE.Persistence;

namespace SSS.BE.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EmployeeController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<EmployeeController> _logger;

    public EmployeeController(ApplicationDbContext context, ILogger<EmployeeController> logger)
    {
        _context = context;
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
        try
        {
            var query = _context.Employees
                .Include(e => e.Department)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(e => e.FullName.Contains(search) || 
                                        e.EmployeeCode.Contains(search) ||
                                        (e.Position != null && e.Position.Contains(search)));
            }

            if (departmentId.HasValue)
            {
                query = query.Where(e => e.DepartmentId == departmentId.Value);
            }

            if (isTeamLeader.HasValue)
            {
                query = query.Where(e => e.IsTeamLeader == isTeamLeader.Value);
            }

            // Only show active employees by default
            query = query.Where(e => e.IsActive);

            var totalCount = await query.CountAsync();
            
            var employees = await query
                .OrderBy(e => e.EmployeeCode)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(e => new EmployeeDto
                {
                    Id = e.Id,
                    EmployeeCode = e.EmployeeCode,
                    FullName = e.FullName,
                    Position = e.Position,
                    PhoneNumber = e.PhoneNumber,
                    Address = e.Address,
                    HireDate = e.HireDate,
                    Salary = e.Salary,
                    IsActive = e.IsActive,
                    IsTeamLeader = e.IsTeamLeader,
                    CreatedAt = e.CreatedAt,
                    UpdatedAt = e.UpdatedAt,
                    DepartmentId = e.DepartmentId,
                    DepartmentName = e.Department != null ? e.Department.Name : null,
                    DepartmentCode = e.Department != null ? e.Department.DepartmentCode : null
                })
                .ToListAsync();

            return Ok(new PagedResponse<EmployeeDto>
            {
                Success = true,
                Message = "Employees retrieved successfully",
                Data = employees,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving employees");
            return StatusCode(500, new PagedResponse<EmployeeDto>
            {
                Success = false,
                Message = "An error occurred while retrieving employees",
                Errors = new List<string> { "Please try again later" }
            });
        }
    }

    /// <summary>
    /// Get employee by ID (All authenticated users can view)
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<EmployeeDto>>> GetEmployee(int id)
    {
        try
        {
            var employee = await _context.Employees
                .Include(e => e.Department)
                .Where(e => e.Id == id)
                .Select(e => new EmployeeDto
                {
                    Id = e.Id,
                    EmployeeCode = e.EmployeeCode,
                    FullName = e.FullName,
                    Position = e.Position,
                    PhoneNumber = e.PhoneNumber,
                    Address = e.Address,
                    HireDate = e.HireDate,
                    Salary = e.Salary,
                    IsActive = e.IsActive,
                    IsTeamLeader = e.IsTeamLeader,
                    CreatedAt = e.CreatedAt,
                    UpdatedAt = e.UpdatedAt,
                    DepartmentId = e.DepartmentId,
                    DepartmentName = e.Department != null ? e.Department.Name : null,
                    DepartmentCode = e.Department != null ? e.Department.DepartmentCode : null
                })
                .FirstOrDefaultAsync();

            if (employee == null)
            {
                return NotFound(new ApiResponse<EmployeeDto>
                {
                    Success = false,
                    Message = "Employee not found"
                });
            }

            return Ok(new ApiResponse<EmployeeDto>
            {
                Success = true,
                Message = "Employee retrieved successfully",
                Data = employee
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving employee {EmployeeId}", id);
            return StatusCode(500, new ApiResponse<EmployeeDto>
            {
                Success = false,
                Message = "An error occurred while retrieving employee"
            });
        }
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
            // Check if employee code already exists
            var existingEmployee = await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeCode == request.EmployeeCode);
            
            if (existingEmployee != null)
            {
                return BadRequest(new ApiResponse<EmployeeDto>
                {
                    Success = false,
                    Message = "Employee code already exists",
                    Errors = new List<string> { "This employee code is already in use" }
                });
            }

            // Validate department if provided
            if (request.DepartmentId.HasValue)
            {
                var department = await _context.Departments
                    .FirstOrDefaultAsync(d => d.Id == request.DepartmentId.Value && d.IsActive);
                
                if (department == null)
                {
                    return BadRequest(new ApiResponse<EmployeeDto>
                    {
                        Success = false,
                        Message = "Invalid department",
                        Errors = new List<string> { "Department not found or inactive" }
                    });
                }

                // Check if trying to set as team leader when department already has one
                if (request.IsTeamLeader)
                {
                    var existingTeamLeader = await _context.Employees
                        .FirstOrDefaultAsync(e => e.DepartmentId == request.DepartmentId.Value && 
                                                 e.IsTeamLeader && e.IsActive);
                    
                    if (existingTeamLeader != null)
                    {
                        return BadRequest(new ApiResponse<EmployeeDto>
                        {
                            Success = false,
                            Message = "Department already has a team leader",
                            Errors = new List<string> { $"Employee {existingTeamLeader.EmployeeCode} is already the team leader for this department" }
                        });
                    }
                }
            }

            var employee = new Employee
            {
                EmployeeCode = request.EmployeeCode,
                FullName = request.FullName,
                Position = request.Position,
                PhoneNumber = request.PhoneNumber,
                Address = request.Address,
                HireDate = request.HireDate,
                Salary = request.Salary,
                DepartmentId = request.DepartmentId,
                IsTeamLeader = request.IsTeamLeader,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            // Update department team leader if this employee is set as team leader
            if (request.IsTeamLeader && request.DepartmentId.HasValue)
            {
                var department = await _context.Departments
                    .FirstOrDefaultAsync(d => d.Id == request.DepartmentId.Value);
                
                if (department != null)
                {
                    department.TeamLeaderId = employee.EmployeeCode;
                    department.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }
            }

            // Load the created employee with department info
            var createdEmployee = await _context.Employees
                .Include(e => e.Department)
                .Where(e => e.Id == employee.Id)
                .Select(e => new EmployeeDto
                {
                    Id = e.Id,
                    EmployeeCode = e.EmployeeCode,
                    FullName = e.FullName,
                    Position = e.Position,
                    PhoneNumber = e.PhoneNumber,
                    Address = e.Address,
                    HireDate = e.HireDate,
                    Salary = e.Salary,
                    IsActive = e.IsActive,
                    IsTeamLeader = e.IsTeamLeader,
                    CreatedAt = e.CreatedAt,
                    UpdatedAt = e.UpdatedAt,
                    DepartmentId = e.DepartmentId,
                    DepartmentName = e.Department != null ? e.Department.Name : null,
                    DepartmentCode = e.Department != null ? e.Department.DepartmentCode : null
                })
                .FirstAsync();

            _logger.LogInformation("Employee {EmployeeCode} created successfully", request.EmployeeCode);

            return CreatedAtAction(nameof(GetEmployee), new { id = employee.Id }, new ApiResponse<EmployeeDto>
            {
                Success = true,
                Message = "Employee created successfully",
                Data = createdEmployee
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating employee {EmployeeCode}", request.EmployeeCode);
            return StatusCode(500, new ApiResponse<EmployeeDto>
            {
                Success = false,
                Message = "An error occurred while creating employee"
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
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.Id == id);

            if (employee == null)
            {
                return NotFound(new ApiResponse<EmployeeDto>
                {
                    Success = false,
                    Message = "Employee not found"
                });
            }

            // Validate department if provided
            if (request.DepartmentId.HasValue)
            {
                var department = await _context.Departments
                    .FirstOrDefaultAsync(d => d.Id == request.DepartmentId.Value && d.IsActive);
                
                if (department == null)
                {
                    return BadRequest(new ApiResponse<EmployeeDto>
                    {
                        Success = false,
                        Message = "Invalid department",
                        Errors = new List<string> { "Department not found or inactive" }
                    });
                }

                // Check team leader constraints
                if (request.IsTeamLeader)
                {
                    var existingTeamLeader = await _context.Employees
                        .FirstOrDefaultAsync(e => e.DepartmentId == request.DepartmentId.Value && 
                                                 e.IsTeamLeader && e.IsActive && e.Id != id);
                    
                    if (existingTeamLeader != null)
                    {
                        return BadRequest(new ApiResponse<EmployeeDto>
                        {
                            Success = false,
                            Message = "Department already has a team leader",
                            Errors = new List<string> { $"Employee {existingTeamLeader.EmployeeCode} is already the team leader for this department" }
                        });
                    }
                }
            }

            // Handle team leader changes
            var wasTeamLeader = employee.IsTeamLeader;
            var oldDepartmentId = employee.DepartmentId;

            // Update employee properties
            employee.FullName = request.FullName;
            employee.Position = request.Position;
            employee.PhoneNumber = request.PhoneNumber;
            employee.Address = request.Address;
            employee.HireDate = request.HireDate;
            employee.Salary = request.Salary;
            employee.DepartmentId = request.DepartmentId;
            employee.IsTeamLeader = request.IsTeamLeader;
            employee.IsActive = request.IsActive;
            employee.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Update department team leader references
            if (wasTeamLeader && oldDepartmentId.HasValue)
            {
                var oldDepartment = await _context.Departments
                    .FirstOrDefaultAsync(d => d.Id == oldDepartmentId.Value);
                if (oldDepartment != null && oldDepartment.TeamLeaderId == employee.EmployeeCode)
                {
                    oldDepartment.TeamLeaderId = null;
                    oldDepartment.UpdatedAt = DateTime.UtcNow;
                }
            }

            if (request.IsTeamLeader && request.DepartmentId.HasValue)
            {
                var newDepartment = await _context.Departments
                    .FirstOrDefaultAsync(d => d.Id == request.DepartmentId.Value);
                if (newDepartment != null)
                {
                    newDepartment.TeamLeaderId = employee.EmployeeCode;
                    newDepartment.UpdatedAt = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();

            // Return updated employee
            var updatedEmployee = await _context.Employees
                .Include(e => e.Department)
                .Where(e => e.Id == id)
                .Select(e => new EmployeeDto
                {
                    Id = e.Id,
                    EmployeeCode = e.EmployeeCode,
                    FullName = e.FullName,
                    Position = e.Position,
                    PhoneNumber = e.PhoneNumber,
                    Address = e.Address,
                    HireDate = e.HireDate,
                    Salary = e.Salary,
                    IsActive = e.IsActive,
                    IsTeamLeader = e.IsTeamLeader,
                    CreatedAt = e.CreatedAt,
                    UpdatedAt = e.UpdatedAt,
                    DepartmentId = e.DepartmentId,
                    DepartmentName = e.Department != null ? e.Department.Name : null,
                    DepartmentCode = e.Department != null ? e.Department.DepartmentCode : null
                })
                .FirstAsync();

            _logger.LogInformation("Employee {EmployeeCode} updated successfully", employee.EmployeeCode);

            return Ok(new ApiResponse<EmployeeDto>
            {
                Success = true,
                Message = "Employee updated successfully",
                Data = updatedEmployee
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating employee {EmployeeId}", id);
            return StatusCode(500, new ApiResponse<EmployeeDto>
            {
                Success = false,
                Message = "An error occurred while updating employee"
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
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.Id == id);

            if (employee == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Employee not found"
                });
            }

            // Handle team leader removal
            if (employee.IsTeamLeader && employee.DepartmentId.HasValue)
            {
                var department = await _context.Departments
                    .FirstOrDefaultAsync(d => d.Id == employee.DepartmentId.Value);
                if (department != null && department.TeamLeaderId == employee.EmployeeCode)
                {
                    department.TeamLeaderId = null;
                    department.UpdatedAt = DateTime.UtcNow;
                }
            }

            employee.IsActive = false;
            employee.IsTeamLeader = false;
            employee.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Employee {EmployeeCode} deleted successfully", employee.EmployeeCode);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Employee deleted successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting employee {EmployeeId}", id);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while deleting employee"
            });
        }
    }

    /// <summary>
    /// Get employee by employee code (All authenticated users can view)
    /// </summary>
    [HttpGet("code/{employeeCode}")]
    public async Task<ActionResult<ApiResponse<EmployeeDto>>> GetEmployeeByCode(string employeeCode)
    {
        try
        {
            var employee = await _context.Employees
                .Include(e => e.Department)
                .Where(e => e.EmployeeCode == employeeCode && e.IsActive)
                .Select(e => new EmployeeDto
                {
                    Id = e.Id,
                    EmployeeCode = e.EmployeeCode,
                    FullName = e.FullName,
                    Position = e.Position,
                    PhoneNumber = e.PhoneNumber,
                    Address = e.Address,
                    HireDate = e.HireDate,
                    Salary = e.Salary,
                    IsActive = e.IsActive,
                    IsTeamLeader = e.IsTeamLeader,
                    CreatedAt = e.CreatedAt,
                    UpdatedAt = e.UpdatedAt,
                    DepartmentId = e.DepartmentId,
                    DepartmentName = e.Department != null ? e.Department.Name : null,
                    DepartmentCode = e.Department != null ? e.Department.DepartmentCode : null
                })
                .FirstOrDefaultAsync();

            if (employee == null)
            {
                return NotFound(new ApiResponse<EmployeeDto>
                {
                    Success = false,
                    Message = "Employee not found"
                });
            }

            return Ok(new ApiResponse<EmployeeDto>
            {
                Success = true,
                Message = "Employee retrieved successfully",
                Data = employee
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving employee {EmployeeCode}", employeeCode);
            return StatusCode(500, new ApiResponse<EmployeeDto>
            {
                Success = false,
                Message = "An error occurred while retrieving employee"
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
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.Id == id);

            if (employee == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Employee not found"
                });
            }

            employee.IsActive = isActive;
            employee.UpdatedAt = DateTime.UtcNow;

            // If deactivating and employee is a team leader, remove team leader status
            if (!isActive && employee.IsTeamLeader && employee.DepartmentId.HasValue)
            {
                var department = await _context.Departments
                    .FirstOrDefaultAsync(d => d.Id == employee.DepartmentId.Value);
                if (department != null && department.TeamLeaderId == employee.EmployeeCode)
                {
                    department.TeamLeaderId = null;
                    department.UpdatedAt = DateTime.UtcNow;
                }
                employee.IsTeamLeader = false;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Employee {EmployeeCode} status changed to {Status}", 
                employee.EmployeeCode, isActive ? "Active" : "Inactive");

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = $"Employee {(isActive ? "activated" : "deactivated")} successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing employee status {EmployeeId}", id);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while changing employee status"
            });
        }
    }
}