using SSS.BE.Models.Attendance;
using SSS.BE.Models.Employee;
using System.ComponentModel.DataAnnotations;

namespace SSS.BE.Services.AttendanceService;

/// <summary>
/// Service interface for attendance management (t? ch?m công)
/// </summary>
public interface IAttendanceService
{
    // ===== SELF CHECK-IN/OUT =====
    Task<ApiResponse<AttendanceEventDto>> CheckInAsync(string employeeCode, CheckInRequest request);
    Task<ApiResponse<AttendanceEventDto>> CheckOutAsync(string employeeCode, CheckOutRequest request);
    Task<ApiResponse<object>> GetCurrentAttendanceStatusAsync(string employeeCode);

    // ===== ATTENDANCE HISTORY =====
    Task<PagedResponse<AttendanceDailyDto>> GetAttendanceHistoryAsync(AttendanceReportFilter filter);
    Task<PagedResponse<AttendanceEventDto>> GetAttendanceEventsAsync(
        string? employeeCode, DateTime? startDate, DateTime? endDate, 
        string? eventType, int pageNumber, int pageSize);

    // ===== MANUAL ADJUSTMENTS =====
    Task<ApiResponse<AttendanceDailyDto>> AdjustAttendanceAsync(ManualAttendanceAdjustmentRequest request, string adjustedBy);
    Task<ApiResponse<object>> ApproveAttendanceAsync(int attendanceId, string approvedBy, string? notes);

    // ===== DASHBOARD =====
    Task<ApiResponse<AttendanceDashboardDto>> GetAttendanceDashboardAsync(DateTime date);

    // ===== SYSTEM OPERATIONS =====
    Task<ApiResponse<object>> ProcessDailyAttendanceAsync(DateTime date);
    Task<ApiResponse<object>> RecalculateAttendanceAsync(string employeeCode, DateTime startDate, DateTime endDate);
}