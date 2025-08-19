using Microsoft.EntityFrameworkCore;
using SSS.BE.Domain.Entities;
using SSS.BE.Models.Employee;
using SSS.BE.Persistence;
using SSS.BE.Services.Common;

namespace SSS.BE.Services.DepartmentService;

public interface IDepartmentService
{
    Task<PagedResponse<DepartmentDto>> GetDepartmentsAsync(int pageNumber, int pageSize, string? search, bool includeEmployees);
    Task<ApiResponse<DepartmentDto?>> GetDepartmentByIdAsync(int id, bool includeEmployees);
    Task<ApiResponse<DepartmentDto>> CreateDepartmentAsync(CreateDepartmentRequest request);
    Task<ApiResponse<DepartmentDto>> UpdateDepartmentAsync(int id, UpdateDepartmentRequest request);
    Task<ApiResponse<object>> DeleteDepartmentAsync(int id);
    Task<PagedResponse<EmployeeDto>> GetDepartmentEmployeesAsync(int id, int pageNumber, int pageSize, string? search);
    Task<ApiResponse<object>> AssignTeamLeaderAsync(int id, string employeeCode);
    Task<ApiResponse<object>> RemoveTeamLeaderAsync(int id);

    // ?? NEW: Flexible department management methods
    Task<PagedResponse<DepartmentDto>> GetDepartmentsWithoutTeamLeaderAsync(int pageNumber, int pageSize);
    Task<ApiResponse<DepartmentDto>> AssignTeamLeaderAdvancedAsync(int id, AssignTeamLeaderRequest request);
    Task<ApiResponse<DepartmentAssignmentResult>> BulkAssignEmployeesToDepartmentAsync(int id, BulkAssignToDepartmentRequest request);
    Task<ApiResponse<DepartmentAssignmentResult>> ManageDepartmentEmployeesAsync(int id, ManageDepartmentEmployeesRequest request);
}

public class DepartmentService : BaseService, IDepartmentService
{
    private readonly ApplicationDbContext _context;

    public DepartmentService(ApplicationDbContext context, ILogger<DepartmentService> logger) 
        : base(logger)
    {
        _context = context;
    }

    public async Task<PagedResponse<DepartmentDto>> GetDepartmentsAsync(int pageNumber, int pageSize, string? search, bool includeEmployees)
    {
        return await HandlePagedOperationAsync(async () =>
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

            return (departments, totalCount);
        }, pageNumber, pageSize, "Get departments");
    }

    public async Task<ApiResponse<DepartmentDto?>> GetDepartmentByIdAsync(int id, bool includeEmployees)
    {
        return await HandleOperationAsync(async () =>
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

            return department;
        }, "Get department by ID");
    }

    // ?? NEW: Get departments without team leaders
    public async Task<PagedResponse<DepartmentDto>> GetDepartmentsWithoutTeamLeaderAsync(int pageNumber, int pageSize)
    {
        return await HandlePagedOperationAsync(async () =>
        {
            var query = _context.Departments
                .Where(d => d.IsActive && d.TeamLeaderId == null);

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
                    TeamLeaderEmployeeCode = null,
                    TeamLeaderFullName = null,
                    EmployeeCount = d.Employees.Count(e => e.IsActive),
                    Employees = null
                })
                .ToListAsync();

            return (departments, totalCount);
        }, pageNumber, pageSize, "Get departments without team leader");
    }

    // ?? NEW: Advanced team leader assignment with auto-transfer
    public async Task<ApiResponse<DepartmentDto>> AssignTeamLeaderAdvancedAsync(int id, AssignTeamLeaderRequest request)
    {
        return await HandleOperationAsync(async () =>
        {
            var department = await _context.Departments
                .Include(d => d.TeamLeader)
                .FirstOrDefaultAsync(d => d.Id == id && d.IsActive);

            if (department == null)
            {
                throw new InvalidOperationException("Department not found");
            }

            var newTeamLeader = await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeCode == request.EmployeeCode && e.IsActive);

            if (newTeamLeader == null)
            {
                throw new InvalidOperationException("Employee not found or inactive");
            }

            // Validate team leader constraints
            await ValidateTeamLeaderAssignmentAsync(newTeamLeader, id);

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
            newTeamLeader.UpdatedAt = DateTime.UtcNow;

            // Auto-transfer employee to department if requested
            if (request.TransferEmployeeToDepartment)
            {
                newTeamLeader.DepartmentId = department.Id;
            }

            await _context.SaveChangesAsync();

            // Return updated department
            var updatedDepartment = await GetDepartmentByIdAsync(id, false);
            return updatedDepartment.Data!;
        }, "Assign team leader advanced");
    }

    // ?? NEW: Bulk assign employees to department
    public async Task<ApiResponse<DepartmentAssignmentResult>> BulkAssignEmployeesToDepartmentAsync(int id, BulkAssignToDepartmentRequest request)
    {
        return await HandleOperationAsync(async () =>
        {
            var department = await _context.Departments
                .Include(d => d.TeamLeader)
                .FirstOrDefaultAsync(d => d.Id == id && d.IsActive);

            if (department == null)
            {
                throw new InvalidOperationException("Department not found");
            }

            var result = new DepartmentAssignmentResult
            {
                DepartmentId = department.Id,
                DepartmentName = department.Name,
                EmployeeResults = new List<EmployeeAssignmentResult>(),
                PreviousTeamLeaderCode = department.TeamLeaderId
            };

            // Get all employees to be assigned
            var employees = await _context.Employees
                .Where(e => request.EmployeeCodes.Contains(e.EmployeeCode) && e.IsActive)
                .ToListAsync();

            foreach (var employeeCode in request.EmployeeCodes)
            {
                var employee = employees.FirstOrDefault(e => e.EmployeeCode == employeeCode);
                var employeeResult = new EmployeeAssignmentResult
                {
                    EmployeeCode = employeeCode,
                    EmployeeName = employee?.FullName ?? "Unknown",
                    Action = "Added"
                };

                try
                {
                    if (employee == null)
                    {
                        employeeResult.Success = false;
                        employeeResult.Message = "Employee not found or inactive";
                    }
                    else
                    {
                        // Remove from current team leader role if needed
                        if (employee.IsTeamLeader && employee.DepartmentId != id)
                        {
                            await ClearEmployeeTeamLeaderRole(employee);
                        }

                        // Assign to department
                        employee.DepartmentId = id;
                        employee.UpdatedAt = DateTime.UtcNow;

                        employeeResult.Success = true;
                        employeeResult.Message = "Successfully assigned to department";
                    }
                }
                catch (Exception ex)
                {
                    employeeResult.Success = false;
                    employeeResult.Message = ex.Message;
                }

                result.EmployeeResults.Add(employeeResult);
            }

            // Handle team leader assignment if specified
            if (!string.IsNullOrEmpty(request.TeamLeaderEmployeeCode))
            {
                var teamLeaderEmployee = employees.FirstOrDefault(e => e.EmployeeCode == request.TeamLeaderEmployeeCode);
                if (teamLeaderEmployee != null)
                {
                    // Remove current team leader if exists
                    if (department.TeamLeader != null && department.TeamLeader.EmployeeCode != request.TeamLeaderEmployeeCode)
                    {
                        department.TeamLeader.IsTeamLeader = false;
                        department.TeamLeader.UpdatedAt = DateTime.UtcNow;
                    }

                    // Set new team leader
                    department.TeamLeaderId = teamLeaderEmployee.EmployeeCode;
                    teamLeaderEmployee.IsTeamLeader = true;
                    teamLeaderEmployee.DepartmentId = id;
                    teamLeaderEmployee.UpdatedAt = DateTime.UtcNow;

                    result.NewTeamLeaderCode = request.TeamLeaderEmployeeCode;

                    // Update the employee result for team leader
                    var teamLeaderResult = result.EmployeeResults.FirstOrDefault(r => r.EmployeeCode == request.TeamLeaderEmployeeCode);
                    if (teamLeaderResult != null)
                    {
                        teamLeaderResult.Action = "Added as Team Leader";
                    }
                }
            }

            await _context.SaveChangesAsync();

            return result;
        }, "Bulk assign employees to department");
    }

    // ?? NEW: Manage department employees (add, remove, change team leader)
    public async Task<ApiResponse<DepartmentAssignmentResult>> ManageDepartmentEmployeesAsync(int id, ManageDepartmentEmployeesRequest request)
    {
        return await HandleOperationAsync(async () =>
        {
            var department = await _context.Departments
                .Include(d => d.TeamLeader)
                .FirstOrDefaultAsync(d => d.Id == id && d.IsActive);

            if (department == null)
            {
                throw new InvalidOperationException("Department not found");
            }

            var result = new DepartmentAssignmentResult
            {
                DepartmentId = department.Id,
                DepartmentName = department.Name,
                EmployeeResults = new List<EmployeeAssignmentResult>(),
                PreviousTeamLeaderCode = department.TeamLeaderId
            };

            // Add employees
            if (request.AddEmployees.Any())
            {
                var employeesToAdd = await _context.Employees
                    .Where(e => request.AddEmployees.Contains(e.EmployeeCode) && e.IsActive)
                    .ToListAsync();

                foreach (var employeeCode in request.AddEmployees)
                {
                    var employee = employeesToAdd.FirstOrDefault(e => e.EmployeeCode == employeeCode);
                    var employeeResult = new EmployeeAssignmentResult
                    {
                        EmployeeCode = employeeCode,
                        EmployeeName = employee?.FullName ?? "Unknown",
                        Action = "Added"
                    };

                    try
                    {
                        if (employee == null)
                        {
                            employeeResult.Success = false;
                            employeeResult.Message = "Employee not found or inactive";
                        }
                        else
                        {
                            // Remove from current team leader role if needed
                            if (employee.IsTeamLeader && employee.DepartmentId != id)
                            {
                                await ClearEmployeeTeamLeaderRole(employee);
                            }

                            employee.DepartmentId = id;
                            employee.UpdatedAt = DateTime.UtcNow;

                            employeeResult.Success = true;
                            employeeResult.Message = "Successfully added to department";
                        }
                    }
                    catch (Exception ex)
                    {
                        employeeResult.Success = false;
                        employeeResult.Message = ex.Message;
                    }

                    result.EmployeeResults.Add(employeeResult);
                }
            }

            // Remove employees
            if (request.RemoveEmployees.Any())
            {
                var employeesToRemove = await _context.Employees
                    .Where(e => request.RemoveEmployees.Contains(e.EmployeeCode) && e.DepartmentId == id && e.IsActive)
                    .ToListAsync();

                foreach (var employeeCode in request.RemoveEmployees)
                {
                    var employee = employeesToRemove.FirstOrDefault(e => e.EmployeeCode == employeeCode);
                    var employeeResult = new EmployeeAssignmentResult
                    {
                        EmployeeCode = employeeCode,
                        EmployeeName = employee?.FullName ?? "Unknown",
                        Action = "Removed"
                    };

                    try
                    {
                        if (employee == null)
                        {
                            employeeResult.Success = false;
                            employeeResult.Message = "Employee not found or not in this department";
                        }
                        else
                        {
                            // Remove team leader status if applicable
                            if (employee.IsTeamLeader)
                            {
                                employee.IsTeamLeader = false;
                                department.TeamLeaderId = null;
                            }

                            employee.DepartmentId = null;
                            employee.UpdatedAt = DateTime.UtcNow;

                            employeeResult.Success = true;
                            employeeResult.Message = "Successfully removed from department";
                        }
                    }
                    catch (Exception ex)
                    {
                        employeeResult.Success = false;
                        employeeResult.Message = ex.Message;
                    }

                    result.EmployeeResults.Add(employeeResult);
                }
            }

            // Change team leader
            if (!string.IsNullOrEmpty(request.NewTeamLeaderCode))
            {
                var newTeamLeader = await _context.Employees
                    .FirstOrDefaultAsync(e => e.EmployeeCode == request.NewTeamLeaderCode && e.IsActive);

                if (newTeamLeader != null)
                {
                    // Remove current team leader
                    if (department.TeamLeader != null)
                    {
                        department.TeamLeader.IsTeamLeader = false;
                        department.TeamLeader.UpdatedAt = DateTime.UtcNow;
                    }

                    // Set new team leader
                    department.TeamLeaderId = newTeamLeader.EmployeeCode;
                    newTeamLeader.IsTeamLeader = true;
                    newTeamLeader.DepartmentId = id;
                    newTeamLeader.UpdatedAt = DateTime.UtcNow;

                    result.NewTeamLeaderCode = request.NewTeamLeaderCode;

                    // Add or update result for new team leader
                    var teamLeaderResult = result.EmployeeResults.FirstOrDefault(r => r.EmployeeCode == request.NewTeamLeaderCode);
                    if (teamLeaderResult != null)
                    {
                        teamLeaderResult.Action += " and Set as Team Leader";
                    }
                    else
                    {
                        result.EmployeeResults.Add(new EmployeeAssignmentResult
                        {
                            EmployeeCode = request.NewTeamLeaderCode,
                            EmployeeName = newTeamLeader.FullName,
                            Action = "Set as Team Leader",
                            Success = true,
                            Message = "Successfully assigned as team leader"
                        });
                    }
                }
            }

            await _context.SaveChangesAsync();

            return result;
        }, "Manage department employees");
    }

    public async Task<ApiResponse<DepartmentDto>> CreateDepartmentAsync(CreateDepartmentRequest request)
    {
        return await HandleOperationAsync(async () =>
        {
            // Validate department code uniqueness
            if (!string.IsNullOrEmpty(request.DepartmentCode))
            {
                await ValidateDepartmentCodeUniquenessAsync(request.DepartmentCode);
            }

            // Validate team leader if provided
            Employee? teamLeader = null;
            if (!string.IsNullOrEmpty(request.TeamLeaderEmployeeCode))
            {
                teamLeader = await ValidateAndGetTeamLeaderAsync(request.TeamLeaderEmployeeCode);
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
                await SetEmployeeAsTeamLeaderAsync(teamLeader, department.Id);
            }

            // Return created department
            var createdDepartment = await GetDepartmentByIdAsync(department.Id, false);
            return createdDepartment.Data!;
        }, "Create department");
    }

    public async Task<ApiResponse<DepartmentDto>> UpdateDepartmentAsync(int id, UpdateDepartmentRequest request)
    {
        return await HandleOperationAsync(async () =>
        {
            var department = await _context.Departments
                .Include(d => d.TeamLeader)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (department == null)
            {
                throw new InvalidOperationException("Department not found");
            }

            // Validate department code uniqueness if changed
            if (!string.IsNullOrEmpty(request.DepartmentCode) && 
                request.DepartmentCode != department.DepartmentCode)
            {
                await ValidateDepartmentCodeUniquenessAsync(request.DepartmentCode, id);
            }

            // Handle team leader changes
            var oldTeamLeader = department.TeamLeader;
            Employee? newTeamLeader = null;

            if (!string.IsNullOrEmpty(request.TeamLeaderEmployeeCode))
            {
                newTeamLeader = await ValidateAndGetTeamLeaderAsync(request.TeamLeaderEmployeeCode, department.TeamLeaderId);
            }

            // Update department
            department.Name = request.Name;
            department.DepartmentCode = request.DepartmentCode;
            department.Description = request.Description;
            department.TeamLeaderId = newTeamLeader?.EmployeeCode;
            department.IsActive = request.IsActive;
            department.UpdatedAt = DateTime.UtcNow;

            // Handle team leader transitions
            await HandleTeamLeaderTransitionAsync(oldTeamLeader, newTeamLeader, department.Id);

            await _context.SaveChangesAsync();

            // Return updated department
            var updatedDepartment = await GetDepartmentByIdAsync(id, false);
            return updatedDepartment.Data!;
        }, "Update department");
    }

    public async Task<ApiResponse<object>> DeleteDepartmentAsync(int id)
    {
        return await HandleOperationAsync<object>(async () =>
        {
            var department = await _context.Departments
                .Include(d => d.Employees)
                .Include(d => d.TeamLeader)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (department == null)
            {
                throw new InvalidOperationException("Department not found");
            }

            // Check if department has active employees
            var activeEmployees = department.Employees.Where(e => e.IsActive).ToList();
            if (activeEmployees.Any())
            {
                throw new InvalidOperationException($"Cannot delete department with {activeEmployees.Count} active employees. Please reassign or deactivate them first.");
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

            return (object)new { Message = "Department deleted successfully" };
        }, "Delete department");
    }

    public async Task<PagedResponse<EmployeeDto>> GetDepartmentEmployeesAsync(int id, int pageNumber, int pageSize, string? search)
    {
        return await HandlePagedOperationAsync(async () =>
        {
            var department = await _context.Departments
                .FirstOrDefaultAsync(d => d.Id == id && d.IsActive);

            if (department == null)
            {
                throw new InvalidOperationException("Department not found");
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

            return (employees, totalCount);
        }, pageNumber, pageSize, "Get department employees");
    }

    public async Task<ApiResponse<object>> AssignTeamLeaderAsync(int id, string employeeCode)
    {
        return await HandleOperationAsync<object>(async () =>
        {
            var department = await _context.Departments
                .Include(d => d.TeamLeader)
                .FirstOrDefaultAsync(d => d.Id == id && d.IsActive);

            if (department == null)
            {
                throw new InvalidOperationException("Department not found");
            }

            var newTeamLeader = await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeCode == employeeCode && e.IsActive);

            if (newTeamLeader == null)
            {
                throw new InvalidOperationException("Employee not found or inactive");
            }

            // Validate team leader constraints
            await ValidateTeamLeaderAssignmentAsync(newTeamLeader, id);

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

            return (object)new { Message = "Team leader assigned successfully" };
        }, "Assign team leader");
    }

    public async Task<ApiResponse<object>> RemoveTeamLeaderAsync(int id)
    {
        return await HandleOperationAsync<object>(async () =>
        {
            var department = await _context.Departments
                .Include(d => d.TeamLeader)
                .FirstOrDefaultAsync(d => d.Id == id && d.IsActive);

            if (department == null)
            {
                throw new InvalidOperationException("Department not found");
            }

            if (department.TeamLeader == null)
            {
                throw new InvalidOperationException("Department has no team leader assigned");
            }

            // Remove team leader
            department.TeamLeader.IsTeamLeader = false;
            department.TeamLeader.UpdatedAt = DateTime.UtcNow;

            department.TeamLeaderId = null;
            department.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return (object)new { Message = "Team leader removed successfully" };
        }, "Remove team leader");
    }

    // Private helper methods
    private async Task ValidateDepartmentCodeUniquenessAsync(string departmentCode, int? excludeId = null)
    {
        var existing = await _context.Departments
            .FirstOrDefaultAsync(d => d.DepartmentCode == departmentCode && 
                                     (excludeId == null || d.Id != excludeId));
        
        if (existing != null)
        {
            throw new InvalidOperationException("Department code already exists");
        }
    }

    private async Task<Employee> ValidateAndGetTeamLeaderAsync(string employeeCode, string? excludeCurrentTeamLeader = null)
    {
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.EmployeeCode == employeeCode && e.IsActive);
        
        if (employee == null)
        {
            throw new InvalidOperationException("Employee not found or inactive");
        }

        // Check if employee is already a team leader elsewhere
        if (employee.IsTeamLeader && employeeCode != excludeCurrentTeamLeader)
        {
            var existingDepartment = await _context.Departments
                .FirstOrDefaultAsync(d => d.TeamLeaderId == employeeCode && d.IsActive);
            
            if (existingDepartment != null)
            {
                throw new InvalidOperationException($"Employee is already team leader of department '{existingDepartment.Name}'");
            }
        }

        return employee;
    }

    private async Task ValidateTeamLeaderAssignmentAsync(Employee employee, int departmentId)
    {
        if (employee.IsTeamLeader)
        {
            var existingDepartment = await _context.Departments
                .FirstOrDefaultAsync(d => d.TeamLeaderId == employee.EmployeeCode && d.IsActive);
            
            if (existingDepartment != null && existingDepartment.Id != departmentId)
            {
                throw new InvalidOperationException("Employee is already a team leader of another department");
            }
        }
    }

    private async Task SetEmployeeAsTeamLeaderAsync(Employee employee, int departmentId)
    {
        employee.IsTeamLeader = true;
        employee.DepartmentId = departmentId;
        employee.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    private Task HandleTeamLeaderTransitionAsync(Employee? oldTeamLeader, Employee? newTeamLeader, int departmentId)
    {
        // Remove old team leader status
        if (oldTeamLeader != null && oldTeamLeader.EmployeeCode != newTeamLeader?.EmployeeCode)
        {
            oldTeamLeader.IsTeamLeader = false;
            oldTeamLeader.UpdatedAt = DateTime.UtcNow;
        }

        // Set new team leader status
        if (newTeamLeader != null && newTeamLeader.EmployeeCode != oldTeamLeader?.EmployeeCode)
        {
            newTeamLeader.IsTeamLeader = true;
            newTeamLeader.DepartmentId = departmentId;
            newTeamLeader.UpdatedAt = DateTime.UtcNow;
        }

        return Task.CompletedTask;
    }

    private async Task ClearEmployeeTeamLeaderRole(Employee employee)
    {
        if (employee.IsTeamLeader && employee.DepartmentId.HasValue)
        {
            // Clear team leader reference in the old department
            var oldDepartment = await _context.Departments
                .FirstOrDefaultAsync(d => d.Id == employee.DepartmentId.Value);
            
            if (oldDepartment != null && oldDepartment.TeamLeaderId == employee.EmployeeCode)
            {
                oldDepartment.TeamLeaderId = null;
                oldDepartment.UpdatedAt = DateTime.UtcNow;
            }

            employee.IsTeamLeader = false;
        }
    }
}