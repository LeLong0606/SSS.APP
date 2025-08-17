using Microsoft.EntityFrameworkCore;
using SSS.BE.Domain.Entities;
using SSS.BE.Models.Employee;
using SSS.BE.Models.WorkShift;
using SSS.BE.Persistence;
using SSS.BE.Services.Common;
using System.Text.Json;

namespace SSS.BE.Services.WorkShiftService;

public interface IWorkShiftService
{
    Task<PagedResponse<WorkShiftDto>> GetWorkShiftsAsync(string currentEmployeeCode, string[] userRoles, int pageNumber, int pageSize, string? employeeCode, DateTime? fromDate, DateTime? toDate, int? workLocationId, bool includeInactive);
    Task<ApiResponse<WeeklyShiftsDto?>> GetWeeklyShiftsAsync(string currentEmployeeCode, string[] userRoles, string employeeCode, DateTime weekStartDate);
    Task<ShiftValidationResponse> ValidateShiftAsync(ShiftValidationRequest request);
    Task<ApiResponse<WeeklyShiftsDto?>> CreateWeeklyShiftsAsync(string currentEmployeeCode, string[] userRoles, CreateWeeklyShiftsRequest request);
    Task<ApiResponse<WorkShiftDto?>> UpdateWorkShiftAsync(string currentEmployeeCode, string[] userRoles, int id, UpdateWorkShiftRequest request);
    Task<ApiResponse<object>> DeleteWorkShiftAsync(string currentEmployeeCode, string[] userRoles, int id, string? reason);
    Task<PagedResponse<WorkShiftLogDto>> GetWorkShiftLogsAsync(string currentEmployeeCode, string[] userRoles, int id, int pageNumber, int pageSize);
}

public class WorkShiftService : BaseService, IWorkShiftService
{
    private readonly ApplicationDbContext _context;

    public WorkShiftService(ApplicationDbContext context, ILogger<WorkShiftService> logger) 
        : base(logger)
    {
        _context = context;
    }

    public async Task<PagedResponse<WorkShiftDto>> GetWorkShiftsAsync(string currentEmployeeCode, string[] userRoles, int pageNumber, int pageSize, string? employeeCode, DateTime? fromDate, DateTime? toDate, int? workLocationId, bool includeInactive)
    {
        return await HandlePagedOperationAsync(async () =>
        {
            var currentEmployee = await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeCode == currentEmployeeCode);

            if (currentEmployee == null)
            {
                throw new InvalidOperationException("Current employee not found");
            }

            var query = _context.WorkShifts
                .Include(w => w.Employee)
                .ThenInclude(e => e.Department)
                .Include(w => w.WorkLocation)
                .Include(w => w.AssignedByEmployee)
                .Include(w => w.ModifiedByEmployee)
                .AsQueryable();

            // Authorization filter
            if (userRoles.Contains("Employee"))
            {
                query = query.Where(w => w.EmployeeCode == currentEmployeeCode);
            }
            else if (userRoles.Contains("TeamLeader"))
            {
                query = query.Where(w => w.Employee.DepartmentId == currentEmployee.DepartmentId);
            }

            // Apply filters
            if (!string.IsNullOrEmpty(employeeCode))
            {
                query = query.Where(w => w.EmployeeCode == employeeCode);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(w => w.ShiftDate >= fromDate.Value.Date);
            }

            if (toDate.HasValue)
            {
                query = query.Where(w => w.ShiftDate <= toDate.Value.Date);
            }

            if (workLocationId.HasValue)
            {
                query = query.Where(w => w.WorkLocationId == workLocationId.Value);
            }

            if (!includeInactive)
            {
                query = query.Where(w => w.IsActive);
            }

            var totalCount = await query.CountAsync();
            
            var shifts = await query
                .OrderBy(w => w.ShiftDate)
                .ThenBy(w => w.StartTime)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(w => new WorkShiftDto
                {
                    Id = w.Id,
                    EmployeeCode = w.EmployeeCode,
                    EmployeeName = w.Employee.FullName,
                    EmployeeDepartment = w.Employee.Department != null ? w.Employee.Department.Name : null,
                    WorkLocationId = w.WorkLocationId,
                    WorkLocationName = w.WorkLocation.Name,
                    WorkLocationCode = w.WorkLocation.LocationCode,
                    WorkLocationAddress = w.WorkLocation.Address,
                    ShiftDate = w.ShiftDate,
                    StartTime = w.StartTime,
                    EndTime = w.EndTime,
                    TotalHours = w.TotalHours,
                    AssignedByEmployeeCode = w.AssignedByEmployeeCode,
                    AssignedByEmployeeName = w.AssignedByEmployee.FullName,
                    IsActive = w.IsActive,
                    CreatedAt = w.CreatedAt,
                    UpdatedAt = w.UpdatedAt,
                    IsModified = w.IsModified,
                    ModifiedByEmployeeCode = w.ModifiedByEmployeeCode,
                    ModifiedByEmployeeName = w.ModifiedByEmployee != null ? w.ModifiedByEmployee.FullName : null,
                    ModifiedAt = w.ModifiedAt,
                    ModificationReason = w.ModificationReason
                })
                .ToListAsync();

            return (shifts, totalCount);
        }, pageNumber, pageSize, "Get work shifts");
    }

    public async Task<ApiResponse<WeeklyShiftsDto?>> GetWeeklyShiftsAsync(string currentEmployeeCode, string[] userRoles, string employeeCode, DateTime weekStartDate)
    {
        return await HandleOperationAsync(async () =>
        {
            var monday = weekStartDate.Date.AddDays(-(int)weekStartDate.DayOfWeek + 1);
            var sunday = monday.AddDays(6);

            // Authorization check
            if (userRoles.Contains("Employee") && currentEmployeeCode != employeeCode)
            {
                throw new UnauthorizedAccessException("You can only view your own shifts");
            }

            if (userRoles.Contains("TeamLeader"))
            {
                var currentEmployee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.EmployeeCode == currentEmployeeCode);
                var targetEmployee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.EmployeeCode == employeeCode);

                if (currentEmployee?.DepartmentId != targetEmployee?.DepartmentId)
                {
                    throw new UnauthorizedAccessException("You can only view shifts for employees in your department");
                }
            }

            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeCode == employeeCode);

            if (employee == null)
            {
                throw new InvalidOperationException("Employee not found");
            }

            var shifts = await _context.WorkShifts
                .Include(w => w.Employee)
                .ThenInclude(e => e.Department)
                .Include(w => w.WorkLocation)
                .Include(w => w.AssignedByEmployee)
                .Include(w => w.ModifiedByEmployee)
                .Where(w => w.EmployeeCode == employeeCode && 
                           w.ShiftDate >= monday && 
                           w.ShiftDate <= sunday &&
                           w.IsActive)
                .OrderBy(w => w.ShiftDate)
                .ThenBy(w => w.StartTime)
                .Select(w => new WorkShiftDto
                {
                    Id = w.Id,
                    EmployeeCode = w.EmployeeCode,
                    EmployeeName = w.Employee.FullName,
                    EmployeeDepartment = w.Employee.Department != null ? w.Employee.Department.Name : null,
                    WorkLocationId = w.WorkLocationId,
                    WorkLocationName = w.WorkLocation.Name,
                    WorkLocationCode = w.WorkLocation.LocationCode,
                    WorkLocationAddress = w.WorkLocation.Address,
                    ShiftDate = w.ShiftDate,
                    StartTime = w.StartTime,
                    EndTime = w.EndTime,
                    TotalHours = w.TotalHours,
                    AssignedByEmployeeCode = w.AssignedByEmployeeCode,
                    AssignedByEmployeeName = w.AssignedByEmployee.FullName,
                    IsActive = w.IsActive,
                    CreatedAt = w.CreatedAt,
                    UpdatedAt = w.UpdatedAt,
                    IsModified = w.IsModified,
                    ModifiedByEmployeeCode = w.ModifiedByEmployeeCode,
                    ModifiedByEmployeeName = w.ModifiedByEmployee != null ? w.ModifiedByEmployee.FullName : null,
                    ModifiedAt = w.ModifiedAt,
                    ModificationReason = w.ModificationReason
                })
                .ToListAsync();

            return new WeeklyShiftsDto
            {
                EmployeeCode = employeeCode,
                EmployeeName = employee.FullName,
                WeekStartDate = monday,
                WeekEndDate = sunday,
                TotalWeeklyHours = shifts.Sum(s => s.TotalHours),
                DailyShifts = shifts
            };
        }, "Get weekly shifts");
    }

    public async Task<ShiftValidationResponse> ValidateShiftAsync(ShiftValidationRequest request)
    {
        var validation = new ShiftValidationResponse { IsValid = true };

        var startTime = request.StartTime;
        var endTime = request.EndTime;
        
        if (endTime <= startTime)
        {
            validation.IsValid = false;
            validation.ValidationErrors.Add("End time must be after start time");
            return validation;
        }

        var totalHours = (decimal)(endTime.ToTimeSpan() - startTime.ToTimeSpan()).TotalHours;
        
        if (totalHours > 8)
        {
            validation.IsValid = false;
            validation.ValidationErrors.Add("Shift cannot exceed 8 hours");
            return validation;
        }

        validation.TotalDailyHours = totalHours;

        var existingShifts = await _context.WorkShifts
            .Include(w => w.WorkLocation)
            .Where(w => w.EmployeeCode == request.EmployeeCode &&
                       w.ShiftDate == request.ShiftDate.Date &&
                       w.IsActive &&
                       (request.ExcludeShiftId == null || w.Id != request.ExcludeShiftId))
            .ToListAsync();

        var currentDailyHours = existingShifts.Sum(s => s.TotalHours);
        if (currentDailyHours + totalHours > 8)
        {
            validation.IsValid = false;
            validation.ValidationErrors.Add($"Total daily hours cannot exceed 8. Current: {currentDailyHours}, Adding: {totalHours}");
        }

        foreach (var shift in existingShifts)
        {
            if ((startTime < shift.EndTime && endTime > shift.StartTime))
            {
                validation.IsValid = false;
                validation.ValidationErrors.Add($"Time conflict with existing shift from {shift.StartTime} to {shift.EndTime}");
                
                validation.ConflictingShifts.Add(new WorkShiftDto
                {
                    Id = shift.Id,
                    ShiftDate = shift.ShiftDate,
                    StartTime = shift.StartTime,
                    EndTime = shift.EndTime,
                    TotalHours = shift.TotalHours,
                    WorkLocationName = shift.WorkLocation.Name
                });
            }
        }

        return validation;
    }

    public async Task<ApiResponse<WeeklyShiftsDto?>> CreateWeeklyShiftsAsync(string currentEmployeeCode, string[] userRoles, CreateWeeklyShiftsRequest request)
    {
        return await HandleOperationAsync<WeeklyShiftsDto?>(async () =>
        {
            var currentEmployee = await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeCode == currentEmployeeCode);

            if (currentEmployee == null)
            {
                throw new InvalidOperationException("Current employee not found");
            }

            var targetEmployee = await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeCode == request.EmployeeCode);

            if (targetEmployee == null)
            {
                throw new InvalidOperationException("Target employee not found");
            }

            // Authorization check
            if (userRoles.Contains("TeamLeader"))
            {
                if (currentEmployee.DepartmentId != targetEmployee.DepartmentId)
                {
                    throw new UnauthorizedAccessException("You can only assign shifts to employees in your department");
                }
            }

            var monday = request.WeekStartDate.Date.AddDays(-(int)request.WeekStartDate.DayOfWeek + 1);
            
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                foreach (var dailyShift in request.DailyShifts)
                {
                    var shiftDate = monday.AddDays(dailyShift.DayOfWeek - 1);
                    
                    var validation = await ValidateShiftAsync(new ShiftValidationRequest
                    {
                        EmployeeCode = request.EmployeeCode,
                        ShiftDate = shiftDate,
                        StartTime = dailyShift.StartTime,
                        EndTime = dailyShift.EndTime
                    });
                    
                    if (!validation.IsValid)
                    {
                        await transaction.RollbackAsync();
                        throw new InvalidOperationException($"Shift validation failed: {string.Join(", ", validation.ValidationErrors)}");
                    }

                    var shift = new WorkShift
                    {
                        EmployeeCode = request.EmployeeCode,
                        WorkLocationId = dailyShift.WorkLocationId,
                        ShiftDate = shiftDate,
                        StartTime = dailyShift.StartTime,
                        EndTime = dailyShift.EndTime,
                        TotalHours = validation.TotalDailyHours,
                        AssignedByEmployeeCode = currentEmployeeCode,
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true
                    };

                    _context.WorkShifts.Add(shift);
                    
                    var log = new WorkShiftLog
                    {
                        WorkShift = shift,
                        Action = "CREATE",
                        PerformedByEmployeeCode = currentEmployeeCode,
                        PerformedAt = DateTime.UtcNow,
                        NewValues = JsonSerializer.Serialize(new
                        {
                            shift.EmployeeCode,
                            shift.WorkLocationId,
                            shift.ShiftDate,
                            shift.StartTime,
                            shift.EndTime,
                            shift.TotalHours
                        }),
                        Comments = "Weekly shifts created"
                    };
                    
                    _context.WorkShiftLogs.Add(log);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var weeklyResult = await GetWeeklyShiftsAsync(currentEmployeeCode, userRoles, request.EmployeeCode, monday);
                return weeklyResult.Data;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }, "Create weekly shifts");
    }

    public async Task<ApiResponse<WorkShiftDto?>> UpdateWorkShiftAsync(string currentEmployeeCode, string[] userRoles, int id, UpdateWorkShiftRequest request)
    {
        return await HandleOperationAsync(async () =>
        {
            var currentEmployee = await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeCode == currentEmployeeCode);

            if (currentEmployee == null)
            {
                throw new InvalidOperationException("Current employee not found");
            }

            var shift = await _context.WorkShifts
                .Include(w => w.Employee)
                .FirstOrDefaultAsync(w => w.Id == id);

            if (shift == null)
            {
                throw new InvalidOperationException("Work shift not found");
            }

            // Authorization check
            if (userRoles.Contains("TeamLeader"))
            {
                if (currentEmployee.DepartmentId != shift.Employee.DepartmentId)
                {
                    throw new UnauthorizedAccessException("You can only update shifts for employees in your department");
                }
            }

            var originalValues = new
            {
                shift.WorkLocationId,
                shift.StartTime,
                shift.EndTime,
                shift.TotalHours
            };

            var validation = await ValidateShiftAsync(new ShiftValidationRequest
            {
                EmployeeCode = shift.EmployeeCode,
                ShiftDate = shift.ShiftDate,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                ExcludeShiftId = id
            });

            if (!validation.IsValid)
            {
                throw new InvalidOperationException($"Shift validation failed: {string.Join(", ", validation.ValidationErrors)}");
            }

            shift.WorkLocationId = request.WorkLocationId;
            shift.StartTime = request.StartTime;
            shift.EndTime = request.EndTime;
            shift.TotalHours = validation.TotalDailyHours;
            shift.UpdatedAt = DateTime.UtcNow;

            if (currentEmployeeCode != shift.AssignedByEmployeeCode)
            {
                shift.IsModified = true;
                shift.ModifiedByEmployeeCode = currentEmployeeCode;
                shift.ModifiedAt = DateTime.UtcNow;
                shift.ModificationReason = request.ModificationReason;
            }

            var log = new WorkShiftLog
            {
                WorkShiftId = shift.Id,
                Action = "UPDATE",
                PerformedByEmployeeCode = currentEmployeeCode,
                PerformedAt = DateTime.UtcNow,
                OriginalValues = JsonSerializer.Serialize(originalValues),
                NewValues = JsonSerializer.Serialize(new { shift.WorkLocationId, shift.StartTime, shift.EndTime, shift.TotalHours }),
                Reason = request.ModificationReason,
                Comments = currentEmployeeCode != shift.AssignedByEmployeeCode ? $"Modified by {currentEmployeeCode}" : "Updated by original assigner"
            };

            _context.WorkShiftLogs.Add(log);
            await _context.SaveChangesAsync();

            var updatedShift = await _context.WorkShifts
                .Include(w => w.Employee)
                .ThenInclude(e => e.Department)
                .Include(w => w.WorkLocation)
                .Include(w => w.AssignedByEmployee)
                .Include(w => w.ModifiedByEmployee)
                .Where(w => w.Id == id)
                .Select(w => new WorkShiftDto
                {
                    Id = w.Id,
                    EmployeeCode = w.EmployeeCode,
                    EmployeeName = w.Employee.FullName,
                    EmployeeDepartment = w.Employee.Department != null ? w.Employee.Department.Name : null,
                    WorkLocationId = w.WorkLocationId,
                    WorkLocationName = w.WorkLocation.Name,
                    WorkLocationCode = w.WorkLocation.LocationCode,
                    WorkLocationAddress = w.WorkLocation.Address,
                    ShiftDate = w.ShiftDate,
                    StartTime = w.StartTime,
                    EndTime = w.EndTime,
                    TotalHours = w.TotalHours,
                    AssignedByEmployeeCode = w.AssignedByEmployeeCode,
                    AssignedByEmployeeName = w.AssignedByEmployee.FullName,
                    IsActive = w.IsActive,
                    CreatedAt = w.CreatedAt,
                    UpdatedAt = w.UpdatedAt,
                    IsModified = w.IsModified,
                    ModifiedByEmployeeCode = w.ModifiedByEmployeeCode,
                    ModifiedByEmployeeName = w.ModifiedByEmployee != null ? w.ModifiedByEmployee.FullName : null,
                    ModifiedAt = w.ModifiedAt,
                    ModificationReason = w.ModificationReason
                })
                .FirstAsync();

            return updatedShift;
        }, "Update work shift");
    }

    public async Task<ApiResponse<object>> DeleteWorkShiftAsync(string currentEmployeeCode, string[] userRoles, int id, string? reason)
    {
        return await HandleOperationAsync<object>(async () =>
        {
            var shift = await _context.WorkShifts
                .FirstOrDefaultAsync(w => w.Id == id);

            if (shift == null)
            {
                throw new InvalidOperationException("Work shift not found");
            }

            var originalValues = JsonSerializer.Serialize(new
            {
                shift.EmployeeCode,
                shift.WorkLocationId,
                shift.ShiftDate,
                shift.StartTime,
                shift.EndTime,
                shift.TotalHours,
                shift.IsActive
            });

            shift.IsActive = false;
            shift.UpdatedAt = DateTime.UtcNow;
            shift.IsModified = true;
            shift.ModifiedByEmployeeCode = currentEmployeeCode;
            shift.ModifiedAt = DateTime.UtcNow;
            shift.ModificationReason = reason ?? "Shift deleted";

            var log = new WorkShiftLog
            {
                WorkShiftId = shift.Id,
                Action = "DELETE",
                PerformedByEmployeeCode = currentEmployeeCode,
                PerformedAt = DateTime.UtcNow,
                OriginalValues = originalValues,
                Reason = reason,
                Comments = $"Shift deleted by {currentEmployeeCode}"
            };

            _context.WorkShiftLogs.Add(log);
            await _context.SaveChangesAsync();

            return (object)new { Message = "Work shift deleted successfully" };
        }, "Delete work shift");
    }

    public async Task<PagedResponse<WorkShiftLogDto>> GetWorkShiftLogsAsync(string currentEmployeeCode, string[] userRoles, int id, int pageNumber, int pageSize)
    {
        return await HandlePagedOperationAsync(async () =>
        {
            var shift = await _context.WorkShifts
                .Include(w => w.Employee)
                .FirstOrDefaultAsync(w => w.Id == id);

            if (shift == null)
            {
                throw new InvalidOperationException("Work shift not found");
            }

            // Authorization check for TeamLeader
            if (userRoles.Contains("TeamLeader"))
            {
                var currentEmployee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.EmployeeCode == currentEmployeeCode);

                if (currentEmployee?.DepartmentId != shift.Employee.DepartmentId)
                {
                    throw new UnauthorizedAccessException("You can only view logs for shifts in your department");
                }
            }

            var query = _context.WorkShiftLogs
                .Include(wsl => wsl.PerformedByEmployee)
                .Where(wsl => wsl.WorkShiftId == id);

            var totalCount = await query.CountAsync();

            var logs = await query
                .OrderByDescending(wsl => wsl.PerformedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(wsl => new WorkShiftLogDto
                {
                    Id = wsl.Id,
                    WorkShiftId = wsl.WorkShiftId,
                    Action = wsl.Action,
                    PerformedByEmployeeCode = wsl.PerformedByEmployeeCode,
                    PerformedByEmployeeName = wsl.PerformedByEmployee.FullName,
                    PerformedAt = wsl.PerformedAt,
                    OriginalValues = wsl.OriginalValues,
                    NewValues = wsl.NewValues,
                    Reason = wsl.Reason,
                    Comments = wsl.Comments
                })
                .ToListAsync();

            return (logs, totalCount);
        }, pageNumber, pageSize, "Get work shift logs");
    }
}