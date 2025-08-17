using System.ComponentModel.DataAnnotations;

namespace SSS.BE.Models.WorkShift;

// Work Location DTOs
public class CreateWorkLocationRequest
{
    [Required(ErrorMessage = "Location name is required")]
    [MaxLength(100, ErrorMessage = "Location name cannot exceed 100 characters")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(20, ErrorMessage = "Location code cannot exceed 20 characters")]
    public string? LocationCode { get; set; }

    [MaxLength(500, ErrorMessage = "Address cannot exceed 500 characters")]
    public string? Address { get; set; }

    [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }
}

public class UpdateWorkLocationRequest
{
    [Required(ErrorMessage = "Location name is required")]
    [MaxLength(100, ErrorMessage = "Location name cannot exceed 100 characters")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(20, ErrorMessage = "Location code cannot exceed 20 characters")]
    public string? LocationCode { get; set; }

    [MaxLength(500, ErrorMessage = "Address cannot exceed 500 characters")]
    public string? Address { get; set; }

    [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}

public class WorkLocationDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? LocationCode { get; set; }
    public string? Address { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

// Work Shift DTOs
public class CreateWorkShiftRequest
{
    [Required(ErrorMessage = "Employee code is required")]
    public string EmployeeCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Work location is required")]
    public int WorkLocationId { get; set; }

    [Required(ErrorMessage = "Shift date is required")]
    public DateTime ShiftDate { get; set; }

    [Required(ErrorMessage = "Start time is required")]
    public TimeOnly StartTime { get; set; }

    [Required(ErrorMessage = "End time is required")]
    public TimeOnly EndTime { get; set; }
}

public class UpdateWorkShiftRequest
{
    [Required(ErrorMessage = "Work location is required")]
    public int WorkLocationId { get; set; }

    [Required(ErrorMessage = "Start time is required")]
    public TimeOnly StartTime { get; set; }

    [Required(ErrorMessage = "End time is required")]
    public TimeOnly EndTime { get; set; }

    [MaxLength(500, ErrorMessage = "Modification reason cannot exceed 500 characters")]
    public string? ModificationReason { get; set; }
}

public class CreateWeeklyShiftsRequest
{
    [Required(ErrorMessage = "Employee code is required")]
    public string EmployeeCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Week start date is required (Monday)")]
    public DateTime WeekStartDate { get; set; } // Must be Monday

    [Required(ErrorMessage = "At least one shift is required")]
    public List<DailyShiftRequest> DailyShifts { get; set; } = new();
}

public class DailyShiftRequest
{
    [Range(1, 7, ErrorMessage = "Day of week must be between 1 (Monday) and 7 (Sunday)")]
    public int DayOfWeek { get; set; } // 1=Monday, 7=Sunday

    [Required(ErrorMessage = "Work location is required")]
    public int WorkLocationId { get; set; }

    [Required(ErrorMessage = "Start time is required")]
    public TimeOnly StartTime { get; set; }

    [Required(ErrorMessage = "End time is required")]
    public TimeOnly EndTime { get; set; }
}

public class WorkShiftDto
{
    public int Id { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string? EmployeeDepartment { get; set; }
    
    public int WorkLocationId { get; set; }
    public string WorkLocationName { get; set; } = string.Empty;
    public string? WorkLocationCode { get; set; }
    public string? WorkLocationAddress { get; set; }
    
    public DateTime ShiftDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public decimal TotalHours { get; set; }
    
    public string AssignedByEmployeeCode { get; set; } = string.Empty;
    public string AssignedByEmployeeName { get; set; } = string.Empty;
    
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Modification tracking
    public bool IsModified { get; set; }
    public string? ModifiedByEmployeeCode { get; set; }
    public string? ModifiedByEmployeeName { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public string? ModificationReason { get; set; }
}

public class WeeklyShiftsDto
{
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public DateTime WeekStartDate { get; set; }
    public DateTime WeekEndDate { get; set; }
    public decimal TotalWeeklyHours { get; set; }
    public List<WorkShiftDto> DailyShifts { get; set; } = new();
}

public class WorkShiftLogDto
{
    public int Id { get; set; }
    public int WorkShiftId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string PerformedByEmployeeCode { get; set; } = string.Empty;
    public string PerformedByEmployeeName { get; set; } = string.Empty;
    public DateTime PerformedAt { get; set; }
    public string? OriginalValues { get; set; }
    public string? NewValues { get; set; }
    public string? Reason { get; set; }
    public string? Comments { get; set; }
}

// Validation helper class
public class ShiftValidationRequest
{
    public string EmployeeCode { get; set; } = string.Empty;
    public DateTime ShiftDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int? ExcludeShiftId { get; set; } // For updates
}

public class ShiftValidationResponse
{
    public bool IsValid { get; set; }
    public List<string> ValidationErrors { get; set; } = new();
    public decimal TotalDailyHours { get; set; }
    public List<WorkShiftDto> ConflictingShifts { get; set; } = new();
}