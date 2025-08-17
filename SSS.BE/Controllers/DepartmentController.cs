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
public class DepartmentController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DepartmentController> _logger;

    public DepartmentController(ApplicationDbContext context, ILogger<DepartmentController> logger)
    {
        _context = context;
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
        try
        {
            var query = _context.Departments
                .Include(d => d.TeamLeader)
                .AsQueryable();

            if (includeEmployees)
            {
                query = query.Include(d => d.Employees.Where(e => e.IsActive));
            }

            // Apply search filter
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(d => d.Name.Contains(search) || 
                                        (d.DepartmentCode != null && d.DepartmentCode.Contains(search)) ||
                                        (d.Description != null && d.Description.Contains(search)));
            }

            // Only show active departments by default
            query = query.Where(d => d.IsActive);

            var totalCount = await query.CountAsync();
            
            var departments = await query
                .OrderBy(d => d.Name)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(d => new DepartmentDto
                {
                    Id = d.Id,
                    Name = d.Name,
                    DepartmentCode = d.DepartmentCode,
                    Description = d.Description,
                    IsActive = d.IsActive,
                    CreatedAt = d.CreatedAt,
                    UpdatedAt = d.UpdatedAt,
                    TeamLeaderEmployeeCode = d.TeamLeader != null ? d.TeamLeader.EmployeeCode : null,
                    TeamLeaderFullName = d.TeamLeader != null ? d.TeamLeader.FullName : null,
                    EmployeeCount = d.Employees.Count(e => e.IsActive),
                    Employees = includeEmployees ? d.Employees.Where(e => e.IsActive).Select(e => new EmployeeDto
                    {
                        Id = e.Id,
                        EmployeeCode = e.EmployeeCode,
                        FullName = e.FullName,
                        Position = e.Position,
                        IsTeamLeader = e.IsTeamLeader,
                        IsActive = e.IsActive
                    }).ToList() : null
                })
                .ToListAsync();

            return Ok(new PagedResponse<DepartmentDto>
            {
                Success = true,
                Message = "Departments retrieved successfully",
                Data = departments,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving departments");
            return StatusCode(500, new PagedResponse<DepartmentDto>
            {
                Success = false,
                Message = "An error occurred while retrieving departments",
                Errors = new List<string> { "Please try again later" }
            });
        }
    }

    /// <summary>
    /// Get department by ID (All authenticated users can view)
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<DepartmentDto>>> GetDepartment(int id, [FromQuery] bool includeEmployees = false)
    {
        try
        {
            var query = _context.Departments
                .Include(d => d.TeamLeader)
                .Where(d => d.Id == id);

            if (includeEmployees)
            {
                query = query.Include(d => d.Employees.Where(e => e.IsActive));
            }

            var department = await query
                .Select(d => new DepartmentDto
                {
                    Id = d.Id,
                    Name = d.Name,
                    DepartmentCode = d.DepartmentCode,
                    Description = d.Description,
                    IsActive = d.IsActive,
                    CreatedAt = d.CreatedAt,
                    UpdatedAt = d.UpdatedAt,
                    TeamLeaderEmployeeCode = d.TeamLeader != null ? d.TeamLeader.EmployeeCode : null,
                    TeamLeaderFullName = d.TeamLeader != null ? d.TeamLeader.FullName : null,
                    EmployeeCount = d.Employees.Count(e => e.IsActive),
                    Employees = includeEmployees ? d.Employees.Where(e => e.IsActive).Select(e => new EmployeeDto
                    {
                        Id = e.Id,
                        EmployeeCode = e.EmployeeCode,
                        FullName = e.FullName,
                        Position = e.Position,
                        PhoneNumber = e.PhoneNumber,
                        Address = e.Address,
                        HireDate = e.HireDate,
                        Salary = e.Salary,
                        IsTeamLeader = e.IsTeamLeader,
                        IsActive = e.IsActive,
                        CreatedAt = e.CreatedAt,
                        UpdatedAt = e.UpdatedAt
                    }).ToList() : null
                })
                .FirstOrDefaultAsync();

            if (department == null)
            {
                return NotFound(new ApiResponse<DepartmentDto>
                {
                    Success = false,
                    Message = "Department not found"
                });
            }

            return Ok(new ApiResponse<DepartmentDto>
            {
                Success = true,
                Message = "Department retrieved successfully",
                Data = department
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving department {DepartmentId}", id);
            return StatusCode(500, new ApiResponse<DepartmentDto>
            {
                Success = false,
                Message = "An error occurred while retrieving department"
            });
        }
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
            // Check if department code already exists (if provided)
            if (!string.IsNullOrEmpty(request.DepartmentCode))
            {
                var existingDepartment = await _context.Departments
                    .FirstOrDefaultAsync(d => d.DepartmentCode == request.DepartmentCode);
                
                if (existingDepartment != null)
                {
                    return BadRequest(new ApiResponse<DepartmentDto>
                    {
                        Success = false,
                        Message = "Department code already exists",
                        Errors = new List<string> { "This department code is already in use" }
                    });
                }
            }

            // Validate team leader if provided
            Employee? teamLeader = null;
            if (!string.IsNullOrEmpty(request.TeamLeaderEmployeeCode))
            {
                teamLeader = await _context.Employees
                    .FirstOrDefaultAsync(e => e.EmployeeCode == request.TeamLeaderEmployeeCode && e.IsActive);
                
                if (teamLeader == null)
                {
                    return BadRequest(new ApiResponse<DepartmentDto>
                    {
                        Success = false,
                        Message = "Invalid team leader",
                        Errors = new List<string> { "Employee not found or inactive" }
                    });
                }

                // Check if employee is already a team leader in another department
                if (teamLeader.IsTeamLeader)
                {
                    var existingDepartment = await _context.Departments
                        .FirstOrDefaultAsync(d => d.TeamLeaderId == teamLeader.EmployeeCode && d.IsActive);
                    
                    if (existingDepartment != null)
                    {
                        return BadRequest(new ApiResponse<DepartmentDto>
                        {
                            Success = false,
                            Message = "Employee is already a team leader",
                            Errors = new List<string> { $"Employee is already team leader of department '{existingDepartment.Name}'" }
                        });
                    }
                }
            }

            var department = new Department
            {
                Name = request.Name,
                DepartmentCode = request.DepartmentCode,
                Description = request.Description,
                TeamLeaderId = teamLeader?.EmployeeCode,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.Departments.Add(department);
            await _context.SaveChangesAsync();

            // Update employee as team leader if specified
            if (teamLeader != null)
            {
                teamLeader.IsTeamLeader = true;
                teamLeader.DepartmentId = department.Id;
                teamLeader.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            // Return created department
            var createdDepartment = await _context.Departments
                .Include(d => d.TeamLeader)
                .Where(d => d.Id == department.Id)
                .Select(d => new DepartmentDto
                {
                    Id = d.Id,
                    Name = d.Name,
                    DepartmentCode = d.DepartmentCode,
                    Description = d.Description,
                    IsActive = d.IsActive,
                    CreatedAt = d.CreatedAt,
                    UpdatedAt = d.UpdatedAt,
                    TeamLeaderEmployeeCode = d.TeamLeader != null ? d.TeamLeader.EmployeeCode : null,
                    TeamLeaderFullName = d.TeamLeader != null ? d.TeamLeader.FullName : null,
                    EmployeeCount = 0
                })
                .FirstAsync();

            _logger.LogInformation("Department {DepartmentName} created successfully", request.Name);

            return CreatedAtAction(nameof(GetDepartment), new { id = department.Id }, new ApiResponse<DepartmentDto>
            {
                Success = true,
                Message = "Department created successfully",
                Data = createdDepartment
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating department {DepartmentName}", request.Name);
            return StatusCode(500, new ApiResponse<DepartmentDto>
            {
                Success = false,
                Message = "An error occurred while creating department"
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
            var department = await _context.Departments
                .Include(d => d.TeamLeader)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (department == null)
            {
                return NotFound(new ApiResponse<DepartmentDto>
                {
                    Success = false,
                    Message = "Department not found"
                });
            }

            // Check department code uniqueness if changed
            if (!string.IsNullOrEmpty(request.DepartmentCode) && 
                request.DepartmentCode != department.DepartmentCode)
            {
                var existingDepartment = await _context.Departments
                    .FirstOrDefaultAsync(d => d.DepartmentCode == request.DepartmentCode && d.Id != id);
                
                if (existingDepartment != null)
                {
                    return BadRequest(new ApiResponse<DepartmentDto>
                    {
                        Success = false,
                        Message = "Department code already exists",
                        Errors = new List<string> { "This department code is already in use" }
                    });
                }
            }

            // Handle team leader changes
            var oldTeamLeader = department.TeamLeader;
            Employee? newTeamLeader = null;

            if (!string.IsNullOrEmpty(request.TeamLeaderEmployeeCode))
            {
                newTeamLeader = await _context.Employees
                    .FirstOrDefaultAsync(e => e.EmployeeCode == request.TeamLeaderEmployeeCode && e.IsActive);
                
                if (newTeamLeader == null)
                {
                    return BadRequest(new ApiResponse<DepartmentDto>
                    {
                        Success = false,
                        Message = "Invalid team leader",
                        Errors = new List<string> { "Employee not found or inactive" }
                    });
                }

                // Check if new team leader is already leading another department
                if (newTeamLeader.EmployeeCode != department.TeamLeaderId && newTeamLeader.IsTeamLeader)
                {
                    var existingDepartment = await _context.Departments
                        .FirstOrDefaultAsync(d => d.TeamLeaderId == newTeamLeader.EmployeeCode && d.IsActive);
                    
                    if (existingDepartment != null)
                    {
                        return BadRequest(new ApiResponse<DepartmentDto>
                        {
                            Success = false,
                            Message = "Employee is already a team leader",
                            Errors = new List<string> { $"Employee is already team leader of department '{existingDepartment.Name}'" }
                        });
                    }
                }
            }

            // Update department properties
            department.Name = request.Name;
            department.DepartmentCode = request.DepartmentCode;
            department.Description = request.Description;
            department.TeamLeaderId = newTeamLeader?.EmployeeCode;
            department.IsActive = request.IsActive;
            department.UpdatedAt = DateTime.UtcNow;

            // Update old team leader
            if (oldTeamLeader != null && oldTeamLeader.EmployeeCode != newTeamLeader?.EmployeeCode)
            {
                oldTeamLeader.IsTeamLeader = false;
                oldTeamLeader.UpdatedAt = DateTime.UtcNow;
            }

            // Update new team leader
            if (newTeamLeader != null && newTeamLeader.EmployeeCode != oldTeamLeader?.EmployeeCode)
            {
                newTeamLeader.IsTeamLeader = true;
                newTeamLeader.DepartmentId = department.Id;
                newTeamLeader.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            // Return updated department
            var updatedDepartment = await _context.Departments
                .Include(d => d.TeamLeader)
                .Where(d => d.Id == id)
                .Select(d => new DepartmentDto
                {
                    Id = d.Id,
                    Name = d.Name,
                    DepartmentCode = d.DepartmentCode,
                    Description = d.Description,
                    IsActive = d.IsActive,
                    CreatedAt = d.CreatedAt,
                    UpdatedAt = d.UpdatedAt,
                    TeamLeaderEmployeeCode = d.TeamLeader != null ? d.TeamLeader.EmployeeCode : null,
                    TeamLeaderFullName = d.TeamLeader != null ? d.TeamLeader.FullName : null,
                    EmployeeCount = d.Employees.Count(e => e.IsActive)
                })
                .FirstAsync();

            _logger.LogInformation("Department {DepartmentName} updated successfully", request.Name);

            return Ok(new ApiResponse<DepartmentDto>
            {
                Success = true,
                Message = "Department updated successfully",
                Data = updatedDepartment
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating department {DepartmentId}", id);
            return StatusCode(500, new ApiResponse<DepartmentDto>
            {
                Success = false,
                Message = "An error occurred while updating department"
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
            var department = await _context.Departments
                .Include(d => d.Employees)
                .Include(d => d.TeamLeader)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (department == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Department not found"
                });
            }

            // Check if department has active employees
            var activeEmployees = department.Employees.Where(e => e.IsActive).ToList();
            if (activeEmployees.Any())
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Cannot delete department with active employees",
                    Errors = new List<string> { $"Department has {activeEmployees.Count} active employees. Please reassign or deactivate them first." }
                });
            }

            // Remove team leader status
            if (department.TeamLeader != null)
            {
                department.TeamLeader.IsTeamLeader = false;
                department.TeamLeader.UpdatedAt = DateTime.UtcNow;
            }

            department.IsActive = false;
            department.TeamLeaderId = null;
            department.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Department {DepartmentName} deleted successfully", department.Name);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Department deleted successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting department {DepartmentId}", id);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while deleting department"
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
            var department = await _context.Departments
                .FirstOrDefaultAsync(d => d.Id == id && d.IsActive);

            if (department == null)
            {
                return NotFound(new PagedResponse<EmployeeDto>
                {
                    Success = false,
                    Message = "Department not found"
                });
            }

            var query = _context.Employees
                .Where(e => e.DepartmentId == id && e.IsActive);

            // Apply search filter
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(e => e.FullName.Contains(search) || 
                                        e.EmployeeCode.Contains(search) ||
                                        (e.Position != null && e.Position.Contains(search)));
            }

            var totalCount = await query.CountAsync();
            
            var employees = await query
                .OrderBy(e => e.IsTeamLeader ? 0 : 1) // Team leader first
                .ThenBy(e => e.EmployeeCode)
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
                    DepartmentName = department.Name,
                    DepartmentCode = department.DepartmentCode
                })
                .ToListAsync();

            return Ok(new PagedResponse<EmployeeDto>
            {
                Success = true,
                Message = "Department employees retrieved successfully",
                Data = employees,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving employees for department {DepartmentId}", id);
            return StatusCode(500, new PagedResponse<EmployeeDto>
            {
                Success = false,
                Message = "An error occurred while retrieving department employees",
                Errors = new List<string> { "Please try again later" }
            });
        }
    }

    /// <summary>
    /// Assign team leader to department (Administrator and Director can assign)
    /// </summary>
    [HttpPost("{id}/assign-team-leader")]
    [Authorize(Roles = "Administrator,Director")]
    public async Task<ActionResult<ApiResponse<DepartmentDto>>> AssignTeamLeader(int id, [FromBody] string employeeCode)
    {
        try
        {
            var department = await _context.Departments
                .Include(d => d.TeamLeader)
                .FirstOrDefaultAsync(d => d.Id == id && d.IsActive);

            if (department == null)
            {
                return NotFound(new ApiResponse<DepartmentDto>
                {
                    Success = false,
                    Message = "Department not found"
                });
            }

            var newTeamLeader = await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeCode == employeeCode && e.IsActive);

            if (newTeamLeader == null)
            {
                return BadRequest(new ApiResponse<DepartmentDto>
                {
                    Success = false,
                    Message = "Employee not found or inactive"
                });
            }

            // Check if employee is already a team leader in another department
            if (newTeamLeader.IsTeamLeader)
            {
                var existingDepartment = await _context.Departments
                    .FirstOrDefaultAsync(d => d.TeamLeaderId == newTeamLeader.EmployeeCode && d.IsActive);
                
                if (existingDepartment != null && existingDepartment.Id != id)
                {
                    return BadRequest(new ApiResponse<DepartmentDto>
                    {
                        Success = false,
                        Message = "Employee is already a team leader of another department"
                    });
                }
            }

            // Remove current team leader if exists
            if (department.TeamLeader != null)
            {
                department.TeamLeader.IsTeamLeader = false;
                department.TeamLeader.UpdatedAt = DateTime.UtcNow;
            }

            // Assign new team leader
            department.TeamLeaderId = newTeamLeader.EmployeeCode;
            department.UpdatedAt = DateTime.UtcNow;

            newTeamLeader.IsTeamLeader = true;
            newTeamLeader.DepartmentId = department.Id;
            newTeamLeader.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Employee {EmployeeCode} assigned as team leader for department {DepartmentName}", 
                employeeCode, department.Name);

            return Ok(new ApiResponse<DepartmentDto>
            {
                Success = true,
                Message = "Team leader assigned successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning team leader for department {DepartmentId}", id);
            return StatusCode(500, new ApiResponse<DepartmentDto>
            {
                Success = false,
                Message = "An error occurred while assigning team leader"
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
            var department = await _context.Departments
                .Include(d => d.TeamLeader)
                .FirstOrDefaultAsync(d => d.Id == id && d.IsActive);

            if (department == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Department not found"
                });
            }

            if (department.TeamLeader == null)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Department has no team leader assigned"
                });
            }

            // Remove team leader
            department.TeamLeader.IsTeamLeader = false;
            department.TeamLeader.UpdatedAt = DateTime.UtcNow;

            department.TeamLeaderId = null;
            department.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Team leader removed from department {DepartmentName}", department.Name);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Team leader removed successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing team leader from department {DepartmentId}", id);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while removing team leader"
            });
        }
    }
}