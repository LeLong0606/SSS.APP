using Microsoft.EntityFrameworkCore;
using SSS.BE.Domain.Entities;
using SSS.BE.Models.Employee;
using SSS.BE.Persistence;
using SSS.BE.Services.Common;

namespace SSS.BE.Services.EmployeeService;

public interface IEmployeeService
{
    Task<PagedResponse<EmployeeDto>> GetEmployeesAsync(int pageNumber, int pageSize, string? search, int? departmentId, bool? isTeamLeader);
    Task<ApiResponse<EmployeeDto?>> GetEmployeeByIdAsync(int id);
    Task<ApiResponse<EmployeeDto?>> GetEmployeeByCodeAsync(string employeeCode);
    Task<ApiResponse<EmployeeDto>> CreateEmployeeAsync(CreateEmployeeRequest request);
    Task<ApiResponse<EmployeeDto>> UpdateEmployeeAsync(int id, UpdateEmployeeRequest request);
    Task<ApiResponse<object>> DeleteEmployeeAsync(int id);
    Task<ApiResponse<object>> ToggleEmployeeStatusAsync(int id, bool isActive);
    
    // ?? NEW: Flexible employee management methods
    Task<PagedResponse<EmployeeDto>> GetUnassignedEmployeesAsync(int pageNumber, int pageSize, string? search);
    Task<ApiResponse<EmployeeDto>> AssignEmployeeToDepartmentAsync(string employeeCode, AssignEmployeeToDepartmentRequest request);
    Task<ApiResponse<EmployeeDto>> UpdateEmployeeDepartmentAsync(string employeeCode, UpdateEmployeeDepartmentRequest request);
    Task<ApiResponse<EmployeeDto>> RemoveEmployeeFromDepartmentAsync(string employeeCode);
}

public class EmployeeService : BaseService, IEmployeeService
{
    private readonly ApplicationDbContext _context;

    public EmployeeService(ApplicationDbContext context, ILogger<EmployeeService> logger) 
        : base(logger)
    {
        _context = context;
    }

    public async Task<PagedResponse<EmployeeDto>> GetEmployeesAsync(int pageNumber, int pageSize, string? search, int? departmentId, bool? isTeamLeader)
    {
        return await HandlePagedOperationAsync(async () =>
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

            return (employees, totalCount);
        }, pageNumber, pageSize, "Get employees");
    }

    public async Task<ApiResponse<EmployeeDto?>> GetEmployeeByIdAsync(int id)
    {
        return await HandleOperationAsync(async () =>
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

            return employee;
        }, "Get employee by ID");
    }

    public async Task<ApiResponse<EmployeeDto?>> GetEmployeeByCodeAsync(string employeeCode)
    {
        return await HandleOperationAsync(async () =>
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

            return employee;
        }, "Get employee by code");
    }

    // ?? NEW: Get employees without department assignment
    public async Task<PagedResponse<EmployeeDto>> GetUnassignedEmployeesAsync(int pageNumber, int pageSize, string? search)
    {
        return await HandlePagedOperationAsync(async () =>
        {
            var query = _context.Employees
                .Where(e => e.IsActive && e.DepartmentId == null)
                .AsQueryable();

            // Apply search filter
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(e => e.FullName.Contains(search) || 
                                        e.EmployeeCode.Contains(search) ||
                                        (e.Position != null && e.Position.Contains(search)));
            }

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
                    DepartmentId = null,
                    DepartmentName = null,
                    DepartmentCode = null
                })
                .ToListAsync();

            return (employees, totalCount);
        }, pageNumber, pageSize, "Get unassigned employees");
    }

    // ?? NEW: Assign employee to department after creation
    public async Task<ApiResponse<EmployeeDto>> AssignEmployeeToDepartmentAsync(string employeeCode, AssignEmployeeToDepartmentRequest request)
    {
        return await HandleOperationAsync(async () =>
        {
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeCode == employeeCode && e.IsActive);

            if (employee == null)
            {
                throw new InvalidOperationException("Employee not found or inactive");
            }

            // Validate department
            await ValidateDepartmentAsync(request.DepartmentId);

            // Check team leader constraint if setting as team leader
            if (request.SetAsTeamLeader)
            {
                await ValidateTeamLeaderConstraintAsync(request.DepartmentId, employee.Id);
            }

            // Remove from current department team leader role if needed
            if (employee.IsTeamLeader && employee.DepartmentId.HasValue)
            {
                await ClearDepartmentTeamLeaderAsync(employee.DepartmentId.Value, employee.EmployeeCode);
            }

            // Update employee
            employee.DepartmentId = request.DepartmentId;
            employee.IsTeamLeader = request.SetAsTeamLeader;
            employee.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Update department team leader if needed
            if (request.SetAsTeamLeader)
            {
                await UpdateDepartmentTeamLeaderAsync(request.DepartmentId, employee.EmployeeCode);
            }

            // Return updated employee
            var updatedEmployee = await GetEmployeeByIdAsync(employee.Id);
            return updatedEmployee.Data!;
        }, "Assign employee to department");
    }

    // ?? NEW: Update employee's department assignment
    public async Task<ApiResponse<EmployeeDto>> UpdateEmployeeDepartmentAsync(string employeeCode, UpdateEmployeeDepartmentRequest request)
    {
        return await HandleOperationAsync(async () =>
        {
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeCode == employeeCode && e.IsActive);

            if (employee == null)
            {
                throw new InvalidOperationException("Employee not found or inactive");
            }

            // Handle removing from department
            if (!request.DepartmentId.HasValue)
            {
                return await RemoveEmployeeFromDepartmentInternal(employee);
            }

            // Validate new department
            await ValidateDepartmentAsync(request.DepartmentId.Value);

            // Check team leader constraint if setting as team leader
            if (request.IsTeamLeader)
            {
                await ValidateTeamLeaderConstraintAsync(request.DepartmentId.Value, employee.Id);
            }

            var oldDepartmentId = employee.DepartmentId;
            var wasTeamLeader = employee.IsTeamLeader;

            // Remove from old department team leader role if needed
            if (wasTeamLeader && oldDepartmentId.HasValue)
            {
                await ClearDepartmentTeamLeaderAsync(oldDepartmentId.Value, employee.EmployeeCode);
            }

            // Update employee
            employee.DepartmentId = request.DepartmentId;
            employee.IsTeamLeader = request.IsTeamLeader;
            employee.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Update new department team leader if needed
            if (request.IsTeamLeader)
            {
                await UpdateDepartmentTeamLeaderAsync(request.DepartmentId.Value, employee.EmployeeCode);
            }

            // Return updated employee
            var updatedEmployee = await GetEmployeeByIdAsync(employee.Id);
            return updatedEmployee.Data!;
        }, "Update employee department");
    }

    // ?? NEW: Remove employee from department
    public async Task<ApiResponse<EmployeeDto>> RemoveEmployeeFromDepartmentAsync(string employeeCode)
    {
        return await HandleOperationAsync(async () =>
        {
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeCode == employeeCode && e.IsActive);

            if (employee == null)
            {
                throw new InvalidOperationException("Employee not found or inactive");
            }

            return await RemoveEmployeeFromDepartmentInternal(employee);
        }, "Remove employee from department");
    }

    private async Task<EmployeeDto> RemoveEmployeeFromDepartmentInternal(Employee employee)
    {
        // Remove from department team leader role if needed
        if (employee.IsTeamLeader && employee.DepartmentId.HasValue)
        {
            await ClearDepartmentTeamLeaderAsync(employee.DepartmentId.Value, employee.EmployeeCode);
        }

        // Update employee
        employee.DepartmentId = null;
        employee.IsTeamLeader = false;
        employee.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Return updated employee
        var updatedEmployee = await GetEmployeeByIdAsync(employee.Id);
        return updatedEmployee.Data!;
    }

    public async Task<ApiResponse<EmployeeDto>> CreateEmployeeAsync(CreateEmployeeRequest request)
    {
        return await HandleOperationAsync(async () =>
        {
            // Validate employee code uniqueness
            var existingEmployee = await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeCode == request.EmployeeCode);
            
            if (existingEmployee != null)
            {
                throw new InvalidOperationException("Employee code already exists");
            }

            // Validate department if provided
            if (request.DepartmentId.HasValue)
            {
                await ValidateDepartmentAsync(request.DepartmentId.Value);
                
                // Check team leader constraint
                if (request.IsTeamLeader)
                {
                    await ValidateTeamLeaderConstraintAsync(request.DepartmentId.Value);
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

            // Update department team leader if needed
            if (request.IsTeamLeader && request.DepartmentId.HasValue)
            {
                await UpdateDepartmentTeamLeaderAsync(request.DepartmentId.Value, employee.EmployeeCode);
            }

            // Return created employee
            var createdEmployee = await GetEmployeeByIdAsync(employee.Id);
            return createdEmployee.Data!;
        }, "Create employee");
    }

    public async Task<ApiResponse<EmployeeDto>> UpdateEmployeeAsync(int id, UpdateEmployeeRequest request)
    {
        return await HandleOperationAsync(async () =>
        {
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.Id == id);

            if (employee == null)
            {
                throw new InvalidOperationException("Employee not found");
            }

            // Validate department if changed
            if (request.DepartmentId.HasValue)
            {
                await ValidateDepartmentAsync(request.DepartmentId.Value);

                if (request.IsTeamLeader)
                {
                    await ValidateTeamLeaderConstraintAsync(request.DepartmentId.Value, id);
                }
            }

            // Handle team leader changes
            var wasTeamLeader = employee.IsTeamLeader;
            var oldDepartmentId = employee.DepartmentId;

            // Update employee
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

            // Update department references
            await HandleDepartmentTeamLeaderChangesAsync(employee, wasTeamLeader, oldDepartmentId, request.DepartmentId, request.IsTeamLeader);

            // Return updated employee
            var updatedEmployee = await GetEmployeeByIdAsync(id);
            return updatedEmployee.Data!;
        }, "Update employee");
    }

    public async Task<ApiResponse<object>> DeleteEmployeeAsync(int id)
    {
        return await HandleOperationAsync<object>(async () =>
        {
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.Id == id);

            if (employee == null)
            {
                throw new InvalidOperationException("Employee not found");
            }

            // Handle team leader removal
            if (employee.IsTeamLeader && employee.DepartmentId.HasValue)
            {
                await ClearDepartmentTeamLeaderAsync(employee.DepartmentId.Value, employee.EmployeeCode);
            }

            employee.IsActive = false;
            employee.IsTeamLeader = false;
            employee.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return (object)new { Message = "Employee deleted successfully" };
        }, "Delete employee");
    }

    public async Task<ApiResponse<object>> ToggleEmployeeStatusAsync(int id, bool isActive)
    {
        return await HandleOperationAsync<object>(async () =>
        {
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.Id == id);

            if (employee == null)
            {
                throw new InvalidOperationException("Employee not found");
            }

            employee.IsActive = isActive;
            employee.UpdatedAt = DateTime.UtcNow;

            // Handle team leader status when deactivating
            if (!isActive && employee.IsTeamLeader && employee.DepartmentId.HasValue)
            {
                await ClearDepartmentTeamLeaderAsync(employee.DepartmentId.Value, employee.EmployeeCode);
                employee.IsTeamLeader = false;
            }

            await _context.SaveChangesAsync();

            return (object)new { Message = $"Employee {(isActive ? "activated" : "deactivated")} successfully" };
        }, "Toggle employee status");
    }

    // Private helper methods
    private async Task ValidateDepartmentAsync(int departmentId)
    {
        var department = await _context.Departments
            .FirstOrDefaultAsync(d => d.Id == departmentId && d.IsActive);
        
        if (department == null)
        {
            throw new InvalidOperationException("Department not found or inactive");
        }
    }

    private async Task ValidateTeamLeaderConstraintAsync(int departmentId, int? excludeEmployeeId = null)
    {
        var existingTeamLeader = await _context.Employees
            .FirstOrDefaultAsync(e => e.DepartmentId == departmentId && 
                                     e.IsTeamLeader && 
                                     e.IsActive && 
                                     (excludeEmployeeId == null || e.Id != excludeEmployeeId));
        
        if (existingTeamLeader != null)
        {
            throw new InvalidOperationException($"Department already has a team leader: {existingTeamLeader.EmployeeCode}");
        }
    }

    private async Task UpdateDepartmentTeamLeaderAsync(int departmentId, string employeeCode)
    {
        var department = await _context.Departments
            .FirstOrDefaultAsync(d => d.Id == departmentId);
        
        if (department != null)
        {
            department.TeamLeaderId = employeeCode;
            department.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    private async Task ClearDepartmentTeamLeaderAsync(int departmentId, string employeeCode)
    {
        var department = await _context.Departments
            .FirstOrDefaultAsync(d => d.Id == departmentId);
        
        if (department != null && department.TeamLeaderId == employeeCode)
        {
            department.TeamLeaderId = null;
            department.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    private async Task HandleDepartmentTeamLeaderChangesAsync(Employee employee, bool wasTeamLeader, int? oldDepartmentId, int? newDepartmentId, bool isTeamLeader)
    {
        // Clear old department team leader if needed
        if (wasTeamLeader && oldDepartmentId.HasValue)
        {
            await ClearDepartmentTeamLeaderAsync(oldDepartmentId.Value, employee.EmployeeCode);
        }

        // Set new department team leader if needed
        if (isTeamLeader && newDepartmentId.HasValue)
        {
            await UpdateDepartmentTeamLeaderAsync(newDepartmentId.Value, employee.EmployeeCode);
        }
    }
}