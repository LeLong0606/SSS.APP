using System.ComponentModel.DataAnnotations;

namespace SSS.BE.Domain.Entities;

/// <summary>
/// Danh m?c lo?i ca l�m vi?c (Ca s�ng, ca chi?u, ca t?i, v.v.)
/// </summary>
public class ShiftTemplate
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty; // "Ca s�ng", "Ca chi?u", "Ca t?i"
    
    [MaxLength(20)]
    public string Code { get; set; } = string.Empty; // "MORNING", "AFTERNOON", "NIGHT"
    
    [Required]
    public TimeOnly StartTime { get; set; } // 08:00
    
    [Required]
    public TimeOnly EndTime { get; set; } // 17:00
    
    public TimeOnly? BreakStartTime { get; set; } // 12:00 (ngh? tr?a)
    
    public TimeOnly? BreakEndTime { get; set; } // 13:00
    
    public int AllowedLateMinutes { get; set; } = 15; // Cho ph�p tr? 15 ph�t
    
    public int AllowedEarlyLeaveMinutes { get; set; } = 15; // Cho ph�p v? s?m 15 ph�t
    
    public decimal StandardHours { get; set; } = 8.0m; // 8 ti?ng chu?n
    
    public bool IsOvertimeEligible { get; set; } = true; // C� ???c t�nh OT kh�ng
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    // Navigation properties
    public virtual ICollection<ShiftAssignment> ShiftAssignments { get; set; } = new List<ShiftAssignment>();
}

/// <summary>
/// Ph�n ca cho nh�n vi�n (g�n ca theo ng�y/tu?n/th�ng)
/// </summary>
public class ShiftAssignment
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string EmployeeCode { get; set; } = string.Empty;
    
    [Required]
    public int ShiftTemplateId { get; set; }
    
    public int? WorkLocationId { get; set; }
    
    [Required]
    public DateTime StartDate { get; set; } // Ng�y b?t ??u �p d?ng ca n�y
    
    public DateTime? EndDate { get; set; } // Ng�y k?t th�c (null = v� h?n)
    
    public string RecurrencePattern { get; set; } = "DAILY"; // DAILY, WEEKLY, MONTHLY
    
    public string? WeekDays { get; set; } = "1,2,3,4,5"; // Th? 2-6 (1=Monday, 7=Sunday)
    
    [MaxLength(50)]
    public string AssignedBy { get; set; } = string.Empty; // Ng??i ph�n ca
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    [MaxLength(500)]
    public string? Notes { get; set; }
    
    // Navigation properties
    public virtual Employee Employee { get; set; } = null!;
    public virtual ShiftTemplate ShiftTemplate { get; set; } = null!;
    public virtual WorkLocation? WorkLocation { get; set; }
    public virtual Employee AssignedByEmployee { get; set; } = null!;
    
    // Generated shift calendar
    public virtual ICollection<ShiftCalendar> ShiftCalendars { get; set; } = new List<ShiftCalendar>();
}

/// <summary>
/// L?ch ca chi ti?t theo t?ng ng�y (sinh ra t? ShiftAssignment)
/// </summary>
public class ShiftCalendar
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string EmployeeCode { get; set; } = string.Empty;
    
    [Required]
    public int ShiftAssignmentId { get; set; }
    
    [Required]
    public int ShiftTemplateId { get; set; }
    
    public int? WorkLocationId { get; set; }
    
    [Required]
    public DateTime ShiftDate { get; set; } // Ng�y c? th?
    
    [Required]
    public TimeOnly StartTime { get; set; }
    
    [Required]
    public TimeOnly EndTime { get; set; }
    
    public TimeOnly? BreakStartTime { get; set; }
    
    public TimeOnly? BreakEndTime { get; set; }
    
    public decimal StandardHours { get; set; } = 8.0m;
    
    public bool IsSpecialDay { get; set; } = false; // Ng�y l?, ng�y ??c bi?t
    
    public string ShiftStatus { get; set; } = "SCHEDULED"; // SCHEDULED, CANCELLED, MODIFIED
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    [MaxLength(500)]
    public string? Notes { get; set; }
    
    // Navigation properties
    public virtual Employee Employee { get; set; } = null!;
    public virtual ShiftAssignment ShiftAssignment { get; set; } = null!;
    public virtual ShiftTemplate ShiftTemplate { get; set; } = null!;
    public virtual WorkLocation? WorkLocation { get; set; }
    
    // Attendance tracking
    public virtual ICollection<AttendanceEvent> AttendanceEvents { get; set; } = new List<AttendanceEvent>();
    public virtual AttendanceDaily? AttendanceDaily { get; set; }
}

/// <summary>
/// S? ki?n ch?m c�ng (Check-In/Check-Out do nh�n vi�n t? nh?p)
/// </summary>
public class AttendanceEvent
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string EmployeeCode { get; set; } = string.Empty;
    
    public int? ShiftCalendarId { get; set; } // Li�n k?t v?i ca l�m vi?c
    
    [Required]
    public DateTime EventDateTime { get; set; } // Th?i gian ch?m c�ng
    
    [Required]
    [MaxLength(20)]
    public string EventType { get; set; } = string.Empty; // CHECK_IN, CHECK_OUT, BREAK_START, BREAK_END
    
    public int? WorkLocationId { get; set; } // V? tr� ch?m c�ng
    
    [MaxLength(200)]
    public string? DeviceInfo { get; set; } // Th�ng tin thi?t b? (browser, IP)
    
    [MaxLength(50)]
    public string? IPAddress { get; set; }
    
    public decimal? Latitude { get; set; } // T?a ?? GPS (optional)
    
    public decimal? Longitude { get; set; }
    
    [MaxLength(500)]
    public string? Notes { get; set; } // Ghi ch� c?a nh�n vi�n
    
    public bool IsManualEntry { get; set; } = true; // Nh?p tay = true, t? m�y ch?m c�ng = false
    
    [MaxLength(50)]
    public string? ApprovedBy { get; set; } // Ng??i duy?t (n?u c�)
    
    public DateTime? ApprovedAt { get; set; }
    
    public string ApprovalStatus { get; set; } = "PENDING"; // PENDING, APPROVED, REJECTED
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public virtual Employee Employee { get; set; } = null!;
    public virtual ShiftCalendar? ShiftCalendar { get; set; }
    public virtual WorkLocation? WorkLocation { get; set; }
    public virtual Employee? ApprovedByEmployee { get; set; }
}

/// <summary>
/// K?t qu? ch?m c�ng theo ng�y (?� x? l� v� t�nh to�n)
/// </summary>
public class AttendanceDaily
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string EmployeeCode { get; set; } = string.Empty;
    
    [Required]
    public int ShiftCalendarId { get; set; } // Li�n k?t v?i ca l�m vi?c
    
    [Required]
    public DateTime AttendanceDate { get; set; }
    
    public DateTime? CheckInTime { get; set; } // Gi? v�o th?c t?
    
    public DateTime? CheckOutTime { get; set; } // Gi? ra th?c t?
    
    public DateTime? BreakStartTime { get; set; } // B?t ??u ngh? tr?a
    
    public DateTime? BreakEndTime { get; set; } // K?t th�c ngh? tr?a
    
    // Th?i gian chu?n t? ShiftTemplate
    public DateTime ScheduledCheckIn { get; set; }
    
    public DateTime ScheduledCheckOut { get; set; }
    
    // T�nh to�n ch?m c�ng
    public int LateMinutes { get; set; } = 0; // S? ph�t ?i mu?n
    
    public int EarlyLeaveMinutes { get; set; } = 0; // S? ph�t v? s?m
    
    public decimal WorkedMinutes { get; set; } = 0; // T?ng s? ph�t l�m vi?c
    
    public decimal WorkedHours { get; set; } = 0; // T?ng s? gi? l�m vi?c
    
    public decimal StandardHours { get; set; } = 8.0m; // S? gi? chu?n
    
    public decimal OvertimeMinutes { get; set; } = 0; // S? ph�t l�m th�m
    
    public decimal OvertimeHours { get; set; } = 0; // S? gi? l�m th�m
    
    public decimal DeductedHours { get; set; } = 0; // S? gi? b? tr?
    
    public decimal ActualWorkDays { get; set; } = 1.0m; // S? c�ng th?c t? (0.5, 1.0)
    
    // Tr?ng th�i
    public string AttendanceStatus { get; set; } = "NORMAL"; // NORMAL, LATE, EARLY_LEAVE, ABSENT, LEAVE, HOLIDAY
    
    public bool IsComplete { get; set; } = false; // C� ?? CheckIn/CheckOut kh�ng
    
    public bool HasTimeViolation { get; set; } = false; // Vi ph?m gi? gi?c
    
    public bool RequiresApproval { get; set; } = false; // C?n duy?t
    
    [MaxLength(50)]
    public string? ApprovedBy { get; set; }
    
    public DateTime? ApprovedAt { get; set; }
    
    public string ApprovalStatus { get; set; } = "AUTO_APPROVED"; // AUTO_APPROVED, PENDING, APPROVED, REJECTED
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    [MaxLength(1000)]
    public string? Notes { get; set; }
    
    [MaxLength(500)]
    public string? SystemCalculationLog { get; set; } // Log t�nh to�n c?a h? th?ng
    
    // Navigation properties
    public virtual Employee Employee { get; set; } = null!;
    public virtual ShiftCalendar ShiftCalendar { get; set; } = null!;
    public virtual Employee? ApprovedByEmployee { get; set; }
}

/// <summary>
/// ??n ngh? ph�p
/// </summary>
public class LeaveRequest
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string EmployeeCode { get; set; } = string.Empty;
    
    [Required]
    public DateTime StartDate { get; set; }
    
    [Required]
    public DateTime EndDate { get; set; }
    
    public decimal TotalDays { get; set; } // S? ng�y ngh?
    
    [Required]
    [MaxLength(50)]
    public string LeaveType { get; set; } = string.Empty; // ANNUAL_LEAVE, SICK_LEAVE, MATERNITY, UNPAID
    
    [Required]
    [MaxLength(1000)]
    public string Reason { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? AttachmentPath { get; set; } // ???ng d?n file ?�nh k�m
    
    [MaxLength(50)]
    public string? ApprovedBy { get; set; }
    
    public DateTime? ApprovedAt { get; set; }
    
    public string ApprovalStatus { get; set; } = "PENDING"; // PENDING, APPROVED, REJECTED
    
    [MaxLength(500)]
    public string? ApprovalNotes { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public virtual Employee Employee { get; set; } = null!;
    public virtual Employee? ApprovedByEmployee { get; set; }
}

/// <summary>
/// ??n ??ng k� l�m th�m gi?
/// </summary>
public class OvertimeRequest
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string EmployeeCode { get; set; } = string.Empty;
    
    [Required]
    public DateTime OvertimeDate { get; set; }
    
    [Required]
    public TimeOnly StartTime { get; set; }
    
    [Required]
    public TimeOnly EndTime { get; set; }
    
    public decimal PlannedHours { get; set; }
    
    [Required]
    [MaxLength(1000)]
    public string Reason { get; set; } = string.Empty;
    
    public int? WorkLocationId { get; set; }
    
    [MaxLength(50)]
    public string? ApprovedBy { get; set; }
    
    public DateTime? ApprovedAt { get; set; }
    
    public string ApprovalStatus { get; set; } = "PENDING"; // PENDING, APPROVED, REJECTED
    
    [MaxLength(500)]
    public string? ApprovalNotes { get; set; }
    
    // Th?c t? OT (sau khi ch?m c�ng)
    public decimal? ActualHours { get; set; }
    
    public DateTime? ActualStartTime { get; set; }
    
    public DateTime? ActualEndTime { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public virtual Employee Employee { get; set; } = null!;
    public virtual WorkLocation? WorkLocation { get; set; }
    public virtual Employee? ApprovedByEmployee { get; set; }
}

/// <summary>
/// Danh s�ch ng�y ngh? l?
/// </summary>
public class Holiday
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty; // "T?t Nguy�n ?�n", "Qu?c kh�nh"
    
    [Required]
    public DateTime HolidayDate { get; set; }
    
    public DateTime? EndDate { get; set; } // Cho l? k�o d�i nhi?u ng�y
    
    public bool IsRecurring { get; set; } = false; // L?p l?i h�ng n?m
    
    public decimal PayMultiplier { get; set; } = 2.0m; // H? s? l??ng (200%, 300%)
    
    [MaxLength(50)]
    public string HolidayType { get; set; } = "NATIONAL"; // NATIONAL, COMPANY, RELIGIOUS
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    [MaxLength(500)]
    public string? Description { get; set; }
}

/// <summary>
/// K? c�ng (th�ng/chu k? t�nh l??ng)
/// </summary>
public class PayrollPeriod
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string PeriodName { get; set; } = string.Empty; // "Th�ng 12/2024", "Qu� 4/2024"
    
    [Required]
    public DateTime StartDate { get; set; }
    
    [Required]
    public DateTime EndDate { get; set; }
    
    public string PeriodType { get; set; } = "MONTHLY"; // MONTHLY, QUARTERLY, CUSTOM
    
    public string Status { get; set; } = "OPEN"; // OPEN, LOCKED, FINALIZED
    
    public DateTime? LockedAt { get; set; }
    
    [MaxLength(50)]
    public string? LockedBy { get; set; }
    
    public DateTime? FinalizedAt { get; set; }
    
    [MaxLength(50)]
    public string? FinalizedBy { get; set; }
    
    [MaxLength(500)]
    public string? ExcelFilePath { get; set; } // ???ng d?n file Excel ?� xu?t
    
    public DateTime? ExportedAt { get; set; }
    
    [MaxLength(50)]
    public string? ExportedBy { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    [MaxLength(1000)]
    public string? Notes { get; set; }
    
    // Navigation properties
    public virtual Employee? LockedByEmployee { get; set; }
    public virtual Employee? FinalizedByEmployee { get; set; }
    public virtual Employee? ExportedByEmployee { get; set; }
    
    // Summary data
    public virtual ICollection<PayrollSummary> PayrollSummaries { get; set; } = new List<PayrollSummary>();
}

/// <summary>
/// T?ng h?p c�ng theo nh�n vi�n trong k?
/// </summary>
public class PayrollSummary
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int PayrollPeriodId { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string EmployeeCode { get; set; } = string.Empty;
    
    public int TotalWorkingDays { get; set; } // T?ng s? ng�y ph?i l�m trong k?
    
    public decimal ActualWorkDays { get; set; } // T?ng s? c�ng th?c t?
    
    public decimal TotalWorkHours { get; set; } // T?ng gi? l�m vi?c
    
    public decimal TotalOvertimeHours { get; set; } // T?ng gi? OT
    
    public int TotalLateCount { get; set; } // S? l?n ?i mu?n
    
    public int TotalLateMiutes { get; set; } // T?ng ph�t mu?n
    
    public int TotalEarlyLeaveCount { get; set; } // S? l?n v? s?m
    
    public int TotalEarlyLeaveMinutes { get; set; } // T?ng ph�t v? s?m
    
    public decimal AnnualLeaveDays { get; set; } // Ngh? ph�p n?m
    
    public decimal SickLeaveDays { get; set; } // Ngh? ?m
    
    public decimal UnpaidLeaveDays { get; set; } // Ngh? kh�ng l??ng
    
    public decimal AbsentDays { get; set; } // V?ng m?t kh�ng ph�p
    
    public decimal HolidayWorkDays { get; set; } // L�m ng�y l?
    
    public decimal DeductedHours { get; set; } // T?ng gi? b? tr?
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    [MaxLength(1000)]
    public string? CalculationNotes { get; set; } // Ghi ch� t�nh to�n
    
    // Navigation properties
    public virtual PayrollPeriod PayrollPeriod { get; set; } = null!;
    public virtual Employee Employee { get; set; } = null!;
}