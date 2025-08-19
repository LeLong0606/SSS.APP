using System.ComponentModel.DataAnnotations;

namespace SSS.BE.Models.Attendance;

// ===== SHIFT TEMPLATE DTOs =====

public class ShiftTemplateDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public TimeOnly? BreakStartTime { get; set; }
    public TimeOnly? BreakEndTime { get; set; }
    public int AllowedLateMinutes { get; set; }
    public int AllowedEarlyLeaveMinutes { get; set; }
    public decimal StandardHours { get; set; }
    public bool IsOvertimeEligible { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? Description { get; set; }
}

public class CreateShiftTemplateRequest
{
    [Required(ErrorMessage = "Tên ca làm vi?c là b?t bu?c")]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mã ca làm vi?c là b?t bu?c")]
    [MaxLength(20)]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Gi? b?t ??u là b?t bu?c")]
    public TimeOnly StartTime { get; set; }

    [Required(ErrorMessage = "Gi? k?t thúc là b?t bu?c")]
    public TimeOnly EndTime { get; set; }

    public TimeOnly? BreakStartTime { get; set; }
    public TimeOnly? BreakEndTime { get; set; }

    [Range(0, 60, ErrorMessage = "Th?i gian cho phép tr? ph?i t? 0-60 phút")]
    public int AllowedLateMinutes { get; set; } = 15;

    [Range(0, 60, ErrorMessage = "Th?i gian cho phép v? s?m ph?i t? 0-60 phút")]
    public int AllowedEarlyLeaveMinutes { get; set; } = 15;

    [Range(1, 12, ErrorMessage = "S? gi? chu?n ph?i t? 1-12 gi?")]
    public decimal StandardHours { get; set; } = 8.0m;

    public bool IsOvertimeEligible { get; set; } = true;

    [MaxLength(500)]
    public string? Description { get; set; }
}

public class UpdateShiftTemplateRequest : CreateShiftTemplateRequest
{
    public bool IsActive { get; set; } = true;
}

// ===== SHIFT ASSIGNMENT DTOs =====

public class ShiftAssignmentDto
{
    public int Id { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public int ShiftTemplateId { get; set; }
    public string ShiftTemplateName { get; set; } = string.Empty;
    public int? WorkLocationId { get; set; }
    public string? WorkLocationName { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string RecurrencePattern { get; set; } = string.Empty;
    public string? WeekDays { get; set; }
    public string AssignedBy { get; set; } = string.Empty;
    public string AssignedByName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? Notes { get; set; }
}

public class CreateShiftAssignmentRequest
{
    [Required(ErrorMessage = "Mã nhân viên là b?t bu?c")]
    public string EmployeeCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "M?u ca làm vi?c là b?t bu?c")]
    public int ShiftTemplateId { get; set; }

    public int? WorkLocationId { get; set; }

    [Required(ErrorMessage = "Ngày b?t ??u là b?t bu?c")]
    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public string RecurrencePattern { get; set; } = "DAILY";

    public string? WeekDays { get; set; } = "1,2,3,4,5"; // Mon-Fri

    [MaxLength(500)]
    public string? Notes { get; set; }
}

public class BulkShiftAssignmentRequest
{
    [Required]
    [MinLength(1, ErrorMessage = "Ph?i có ít nh?t m?t nhân viên")]
    public List<string> EmployeeCodes { get; set; } = new();

    [Required]
    public int ShiftTemplateId { get; set; }

    public int? WorkLocationId { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public string RecurrencePattern { get; set; } = "DAILY";

    public string? WeekDays { get; set; } = "1,2,3,4,5";

    [MaxLength(500)]
    public string? Notes { get; set; }
}

// ===== ATTENDANCE EVENT DTOs =====

public class AttendanceEventDto
{
    public int Id { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public int? ShiftCalendarId { get; set; }
    public DateTime EventDateTime { get; set; }
    public string EventType { get; set; } = string.Empty;
    public int? WorkLocationId { get; set; }
    public string? WorkLocationName { get; set; }
    public string? DeviceInfo { get; set; }
    public string? IPAddress { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? Notes { get; set; }
    public bool IsManualEntry { get; set; }
    public string? ApprovedBy { get; set; }
    public string? ApprovedByName { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string ApprovalStatus { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CheckInRequest
{
    [Required(ErrorMessage = "Th?i gian ch?m công là b?t bu?c")]
    public DateTime CheckInTime { get; set; }

    public int? WorkLocationId { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
}

public class CheckOutRequest
{
    [Required(ErrorMessage = "Th?i gian ch?m công là b?t bu?c")]
    public DateTime CheckOutTime { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
}

// ===== ATTENDANCE DAILY DTOs =====

public class AttendanceDailyDto
{
    public int Id { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public int ShiftCalendarId { get; set; }
    public DateTime AttendanceDate { get; set; }
    
    // Th?i gian th?c t?
    public DateTime? CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public DateTime? BreakStartTime { get; set; }
    public DateTime? BreakEndTime { get; set; }
    
    // Th?i gian chu?n
    public DateTime ScheduledCheckIn { get; set; }
    public DateTime ScheduledCheckOut { get; set; }
    
    // Tính toán
    public int LateMinutes { get; set; }
    public int EarlyLeaveMinutes { get; set; }
    public decimal WorkedMinutes { get; set; }
    public decimal WorkedHours { get; set; }
    public decimal StandardHours { get; set; }
    public decimal OvertimeMinutes { get; set; }
    public decimal OvertimeHours { get; set; }
    public decimal DeductedHours { get; set; }
    public decimal ActualWorkDays { get; set; }
    
    // Tr?ng thái
    public string AttendanceStatus { get; set; } = string.Empty;
    public bool IsComplete { get; set; }
    public bool HasTimeViolation { get; set; }
    public bool RequiresApproval { get; set; }
    public string? ApprovedBy { get; set; }
    public string? ApprovedByName { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string ApprovalStatus { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? Notes { get; set; }
    public string? SystemCalculationLog { get; set; }
    
    // Shift info
    public ShiftTemplateDto? ShiftTemplate { get; set; }
    public WorkLocationDto? WorkLocation { get; set; }
}

public class ManualAttendanceAdjustmentRequest
{
    [Required]
    public int AttendanceDailyId { get; set; }

    public DateTime? CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }

    [Required(ErrorMessage = "Lý do ?i?u ch?nh là b?t bu?c")]
    [MaxLength(1000)]
    public string Reason { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Notes { get; set; }
}

// ===== LEAVE REQUEST DTOs =====

public class LeaveRequestDto
{
    public int Id { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal TotalDays { get; set; }
    public string LeaveType { get; set; } = string.Empty;
    public string LeaveTypeName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string? AttachmentPath { get; set; }
    public string? ApprovedBy { get; set; }
    public string? ApprovedByName { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string ApprovalStatus { get; set; } = string.Empty;
    public string? ApprovalNotes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateLeaveRequestRequest
{
    [Required(ErrorMessage = "Ngày b?t ??u ngh? là b?t bu?c")]
    public DateTime StartDate { get; set; }

    [Required(ErrorMessage = "Ngày k?t thúc ngh? là b?t bu?c")]
    public DateTime EndDate { get; set; }

    [Required(ErrorMessage = "Lo?i ngh? phép là b?t bu?c")]
    public string LeaveType { get; set; } = string.Empty; // ANNUAL_LEAVE, SICK_LEAVE, MATERNITY, UNPAID

    [Required(ErrorMessage = "Lý do ngh? là b?t bu?c")]
    [MaxLength(1000)]
    public string Reason { get; set; } = string.Empty;

    public IFormFile? AttachmentFile { get; set; }
}

public class ApproveLeaveRequestRequest
{
    [Required]
    public int LeaveRequestId { get; set; }

    [Required]
    public bool IsApproved { get; set; }

    [MaxLength(500)]
    public string? ApprovalNotes { get; set; }
}

// ===== OVERTIME REQUEST DTOs =====

public class OvertimeRequestDto
{
    public int Id { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public DateTime OvertimeDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public decimal PlannedHours { get; set; }
    public string Reason { get; set; } = string.Empty;
    public int? WorkLocationId { get; set; }
    public string? WorkLocationName { get; set; }
    public string? ApprovedBy { get; set; }
    public string? ApprovedByName { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string ApprovalStatus { get; set; } = string.Empty;
    public string? ApprovalNotes { get; set; }
    public decimal? ActualHours { get; set; }
    public DateTime? ActualStartTime { get; set; }
    public DateTime? ActualEndTime { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateOvertimeRequestRequest
{
    [Required(ErrorMessage = "Ngày làm thêm gi? là b?t bu?c")]
    public DateTime OvertimeDate { get; set; }

    [Required(ErrorMessage = "Gi? b?t ??u là b?t bu?c")]
    public TimeOnly StartTime { get; set; }

    [Required(ErrorMessage = "Gi? k?t thúc là b?t bu?c")]
    public TimeOnly EndTime { get; set; }

    [Required(ErrorMessage = "Lý do làm thêm gi? là b?t bu?c")]
    [MaxLength(1000)]
    public string Reason { get; set; } = string.Empty;

    public int? WorkLocationId { get; set; }
}

// ===== PAYROLL PERIOD DTOs =====

public class PayrollPeriodDto
{
    public int Id { get; set; }
    public string PeriodName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string PeriodType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? LockedAt { get; set; }
    public string? LockedBy { get; set; }
    public string? LockedByName { get; set; }
    public DateTime? FinalizedAt { get; set; }
    public string? FinalizedBy { get; set; }
    public string? FinalizedByName { get; set; }
    public string? ExcelFilePath { get; set; }
    public DateTime? ExportedAt { get; set; }
    public string? ExportedBy { get; set; }
    public string? ExportedByName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? Notes { get; set; }
    public int TotalEmployees { get; set; }
    public decimal TotalWorkDays { get; set; }
    public decimal TotalOvertimeHours { get; set; }
}

public class CreatePayrollPeriodRequest
{
    [Required(ErrorMessage = "Tên k? công là b?t bu?c")]
    [MaxLength(100)]
    public string PeriodName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ngày b?t ??u là b?t bu?c")]
    public DateTime StartDate { get; set; }

    [Required(ErrorMessage = "Ngày k?t thúc là b?t bu?c")]
    public DateTime EndDate { get; set; }

    public string PeriodType { get; set; } = "MONTHLY";

    [MaxLength(1000)]
    public string? Notes { get; set; }
}

// ===== PAYROLL SUMMARY DTOs =====

public class PayrollSummaryDto
{
    public int Id { get; set; }
    public int PayrollPeriodId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string? DepartmentName { get; set; }
    public int TotalWorkingDays { get; set; }
    public decimal ActualWorkDays { get; set; }
    public decimal TotalWorkHours { get; set; }
    public decimal TotalOvertimeHours { get; set; }
    public int TotalLateCount { get; set; }
    public int TotalLateMiutes { get; set; }
    public int TotalEarlyLeaveCount { get; set; }
    public int TotalEarlyLeaveMinutes { get; set; }
    public decimal AnnualLeaveDays { get; set; }
    public decimal SickLeaveDays { get; set; }
    public decimal UnpaidLeaveDays { get; set; }
    public decimal AbsentDays { get; set; }
    public decimal HolidayWorkDays { get; set; }
    public decimal DeductedHours { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? CalculationNotes { get; set; }
}

// ===== COMMON DTOs =====

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

public class AttendanceDashboardDto
{
    public DateTime Date { get; set; }
    public int TotalEmployees { get; set; }
    public int PresentEmployees { get; set; }
    public int AbsentEmployees { get; set; }
    public int LateEmployees { get; set; }
    public int EarlyLeaveEmployees { get; set; }
    public decimal AverageWorkHours { get; set; }
    public decimal TotalOvertimeHours { get; set; }
    public List<AttendanceDailyDto> RecentAttendance { get; set; } = new();
}

public class AttendanceReportFilter
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? EmployeeCode { get; set; }
    public int? DepartmentId { get; set; }
    public string? AttendanceStatus { get; set; }
    public bool? HasViolations { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class ExcelExportRequest
{
    [Required]
    public int PayrollPeriodId { get; set; }
    
    public List<string>? EmployeeCodes { get; set; }
    public List<int>? DepartmentIds { get; set; }
    public string ExportFormat { get; set; } = "DETAILED"; // DETAILED, SUMMARY
    public bool IncludeAttendanceLog { get; set; } = true;
    public bool IncludeViolations { get; set; } = true;
}

public class ExcelExportResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? FilePath { get; set; }
    public string? FileName { get; set; }
    public long? FileSize { get; set; }
    public DateTime ExportedAt { get; set; }
    public string ExportedBy { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();
}