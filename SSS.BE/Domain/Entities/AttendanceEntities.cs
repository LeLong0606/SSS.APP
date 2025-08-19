using System.ComponentModel.DataAnnotations;

namespace SSS.BE.Domain.Entities;

/// <summary>
/// Danh m?c lo?i ca làm vi?c (Ca sáng, ca chi?u, ca t?i, v.v.)
/// </summary>
public class ShiftTemplate
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty; // "Ca sáng", "Ca chi?u", "Ca t?i"
    
    [MaxLength(20)]
    public string Code { get; set; } = string.Empty; // "MORNING", "AFTERNOON", "NIGHT"
    
    [Required]
    public TimeOnly StartTime { get; set; } // 08:00
    
    [Required]
    public TimeOnly EndTime { get; set; } // 17:00
    
    public TimeOnly? BreakStartTime { get; set; } // 12:00 (ngh? tr?a)
    
    public TimeOnly? BreakEndTime { get; set; } // 13:00
    
    public int AllowedLateMinutes { get; set; } = 15; // Cho phép tr? 15 phút
    
    public int AllowedEarlyLeaveMinutes { get; set; } = 15; // Cho phép v? s?m 15 phút
    
    public decimal StandardHours { get; set; } = 8.0m; // 8 ti?ng chu?n
    
    public bool IsOvertimeEligible { get; set; } = true; // Có ???c tính OT không
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    // Navigation properties
    public virtual ICollection<ShiftAssignment> ShiftAssignments { get; set; } = new List<ShiftAssignment>();
}

/// <summary>
/// Phân ca cho nhân viên (gán ca theo ngày/tu?n/tháng)
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
    public DateTime StartDate { get; set; } // Ngày b?t ??u áp d?ng ca này
    
    public DateTime? EndDate { get; set; } // Ngày k?t thúc (null = vô h?n)
    
    public string RecurrencePattern { get; set; } = "DAILY"; // DAILY, WEEKLY, MONTHLY
    
    public string? WeekDays { get; set; } = "1,2,3,4,5"; // Th? 2-6 (1=Monday, 7=Sunday)
    
    [MaxLength(50)]
    public string AssignedBy { get; set; } = string.Empty; // Ng??i phân ca
    
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
/// L?ch ca chi ti?t theo t?ng ngày (sinh ra t? ShiftAssignment)
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
    public DateTime ShiftDate { get; set; } // Ngày c? th?
    
    [Required]
    public TimeOnly StartTime { get; set; }
    
    [Required]
    public TimeOnly EndTime { get; set; }
    
    public TimeOnly? BreakStartTime { get; set; }
    
    public TimeOnly? BreakEndTime { get; set; }
    
    public decimal StandardHours { get; set; } = 8.0m;
    
    public bool IsSpecialDay { get; set; } = false; // Ngày l?, ngày ??c bi?t
    
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
/// S? ki?n ch?m công (Check-In/Check-Out do nhân viên t? nh?p)
/// </summary>
public class AttendanceEvent
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string EmployeeCode { get; set; } = string.Empty;
    
    public int? ShiftCalendarId { get; set; } // Liên k?t v?i ca làm vi?c
    
    [Required]
    public DateTime EventDateTime { get; set; } // Th?i gian ch?m công
    
    [Required]
    [MaxLength(20)]
    public string EventType { get; set; } = string.Empty; // CHECK_IN, CHECK_OUT, BREAK_START, BREAK_END
    
    public int? WorkLocationId { get; set; } // V? trí ch?m công
    
    [MaxLength(200)]
    public string? DeviceInfo { get; set; } // Thông tin thi?t b? (browser, IP)
    
    [MaxLength(50)]
    public string? IPAddress { get; set; }
    
    public decimal? Latitude { get; set; } // T?a ?? GPS (optional)
    
    public decimal? Longitude { get; set; }
    
    [MaxLength(500)]
    public string? Notes { get; set; } // Ghi chú c?a nhân viên
    
    public bool IsManualEntry { get; set; } = true; // Nh?p tay = true, t? máy ch?m công = false
    
    [MaxLength(50)]
    public string? ApprovedBy { get; set; } // Ng??i duy?t (n?u có)
    
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
/// K?t qu? ch?m công theo ngày (?ã x? lý và tính toán)
/// </summary>
public class AttendanceDaily
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string EmployeeCode { get; set; } = string.Empty;
    
    [Required]
    public int ShiftCalendarId { get; set; } // Liên k?t v?i ca làm vi?c
    
    [Required]
    public DateTime AttendanceDate { get; set; }
    
    public DateTime? CheckInTime { get; set; } // Gi? vào th?c t?
    
    public DateTime? CheckOutTime { get; set; } // Gi? ra th?c t?
    
    public DateTime? BreakStartTime { get; set; } // B?t ??u ngh? tr?a
    
    public DateTime? BreakEndTime { get; set; } // K?t thúc ngh? tr?a
    
    // Th?i gian chu?n t? ShiftTemplate
    public DateTime ScheduledCheckIn { get; set; }
    
    public DateTime ScheduledCheckOut { get; set; }
    
    // Tính toán ch?m công
    public int LateMinutes { get; set; } = 0; // S? phút ?i mu?n
    
    public int EarlyLeaveMinutes { get; set; } = 0; // S? phút v? s?m
    
    public decimal WorkedMinutes { get; set; } = 0; // T?ng s? phút làm vi?c
    
    public decimal WorkedHours { get; set; } = 0; // T?ng s? gi? làm vi?c
    
    public decimal StandardHours { get; set; } = 8.0m; // S? gi? chu?n
    
    public decimal OvertimeMinutes { get; set; } = 0; // S? phút làm thêm
    
    public decimal OvertimeHours { get; set; } = 0; // S? gi? làm thêm
    
    public decimal DeductedHours { get; set; } = 0; // S? gi? b? tr?
    
    public decimal ActualWorkDays { get; set; } = 1.0m; // S? công th?c t? (0.5, 1.0)
    
    // Tr?ng thái
    public string AttendanceStatus { get; set; } = "NORMAL"; // NORMAL, LATE, EARLY_LEAVE, ABSENT, LEAVE, HOLIDAY
    
    public bool IsComplete { get; set; } = false; // Có ?? CheckIn/CheckOut không
    
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
    public string? SystemCalculationLog { get; set; } // Log tính toán c?a h? th?ng
    
    // Navigation properties
    public virtual Employee Employee { get; set; } = null!;
    public virtual ShiftCalendar ShiftCalendar { get; set; } = null!;
    public virtual Employee? ApprovedByEmployee { get; set; }
}

/// <summary>
/// ??n ngh? phép
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
    
    public decimal TotalDays { get; set; } // S? ngày ngh?
    
    [Required]
    [MaxLength(50)]
    public string LeaveType { get; set; } = string.Empty; // ANNUAL_LEAVE, SICK_LEAVE, MATERNITY, UNPAID
    
    [Required]
    [MaxLength(1000)]
    public string Reason { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? AttachmentPath { get; set; } // ???ng d?n file ?ính kèm
    
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
/// ??n ??ng ký làm thêm gi?
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
    
    // Th?c t? OT (sau khi ch?m công)
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
/// Danh sách ngày ngh? l?
/// </summary>
public class Holiday
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty; // "T?t Nguyên ?án", "Qu?c khánh"
    
    [Required]
    public DateTime HolidayDate { get; set; }
    
    public DateTime? EndDate { get; set; } // Cho l? kéo dài nhi?u ngày
    
    public bool IsRecurring { get; set; } = false; // L?p l?i hàng n?m
    
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
/// K? công (tháng/chu k? tính l??ng)
/// </summary>
public class PayrollPeriod
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string PeriodName { get; set; } = string.Empty; // "Tháng 12/2024", "Quý 4/2024"
    
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
    public string? ExcelFilePath { get; set; } // ???ng d?n file Excel ?ã xu?t
    
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
/// T?ng h?p công theo nhân viên trong k?
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
    
    public int TotalWorkingDays { get; set; } // T?ng s? ngày ph?i làm trong k?
    
    public decimal ActualWorkDays { get; set; } // T?ng s? công th?c t?
    
    public decimal TotalWorkHours { get; set; } // T?ng gi? làm vi?c
    
    public decimal TotalOvertimeHours { get; set; } // T?ng gi? OT
    
    public int TotalLateCount { get; set; } // S? l?n ?i mu?n
    
    public int TotalLateMiutes { get; set; } // T?ng phút mu?n
    
    public int TotalEarlyLeaveCount { get; set; } // S? l?n v? s?m
    
    public int TotalEarlyLeaveMinutes { get; set; } // T?ng phút v? s?m
    
    public decimal AnnualLeaveDays { get; set; } // Ngh? phép n?m
    
    public decimal SickLeaveDays { get; set; } // Ngh? ?m
    
    public decimal UnpaidLeaveDays { get; set; } // Ngh? không l??ng
    
    public decimal AbsentDays { get; set; } // V?ng m?t không phép
    
    public decimal HolidayWorkDays { get; set; } // Làm ngày l?
    
    public decimal DeductedHours { get; set; } // T?ng gi? b? tr?
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    [MaxLength(1000)]
    public string? CalculationNotes { get; set; } // Ghi chú tính toán
    
    // Navigation properties
    public virtual PayrollPeriod PayrollPeriod { get; set; } = null!;
    public virtual Employee Employee { get; set; } = null!;
}