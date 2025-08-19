using SSS.BE.Models.Attendance;
using SSS.BE.Models.Employee;

namespace SSS.BE.Services.PayrollService;

/// <summary>
/// Service interface for payroll management (qu?n lý k? công và xu?t Excel)
/// </summary>
public interface IPayrollService
{
    // ===== PAYROLL PERIODS =====
    Task<PagedResponse<PayrollPeriodDto>> GetPayrollPeriodsAsync(int pageNumber, int pageSize, string? status, int? year);
    Task<ApiResponse<PayrollPeriodDto?>> GetPayrollPeriodByIdAsync(int id);
    Task<ApiResponse<PayrollPeriodDto>> CreatePayrollPeriodAsync(CreatePayrollPeriodRequest request, string createdBy);
    Task<ApiResponse<object>> LockPayrollPeriodAsync(int id, string lockedBy);
    Task<ApiResponse<object>> UnlockPayrollPeriodAsync(int id);
    Task<ApiResponse<object>> FinalizePayrollPeriodAsync(int id, string finalizedBy);

    // ===== PAYROLL SUMMARY =====
    Task<PagedResponse<PayrollSummaryDto>> GetPayrollSummaryAsync(
        int periodId, string? employeeCode, int? departmentId, int pageNumber, int pageSize);
    Task<ApiResponse<object>> RecalculatePayrollSummaryAsync(int periodId, string calculatedBy);

    // ===== EXCEL EXPORT cho TCHC =====
    Task<ApiResponse<ExcelExportResult>> ExportPayrollToExcelAsync(ExcelExportRequest request, string exportedBy);
    Task<ApiResponse<object>> GetExportedFileAsync(string fileName);
    Task<PagedResponse<object>> GetExportHistoryAsync(int? periodId, int pageNumber, int pageSize);

    // ===== LEAVE REQUESTS =====
    Task<PagedResponse<LeaveRequestDto>> GetLeaveRequestsAsync(
        string? employeeCode, string? status, DateTime? startDate, DateTime? endDate, 
        int pageNumber, int pageSize);
    Task<ApiResponse<LeaveRequestDto>> CreateLeaveRequestAsync(CreateLeaveRequestRequest request, string employeeCode);
    Task<ApiResponse<object>> ApproveLeaveRequestAsync(ApproveLeaveRequestRequest request, string approvedBy);

    // ===== OVERTIME REQUESTS =====
    Task<PagedResponse<OvertimeRequestDto>> GetOvertimeRequestsAsync(
        string? employeeCode, string? status, DateTime? startDate, DateTime? endDate,
        int pageNumber, int pageSize);
    Task<ApiResponse<OvertimeRequestDto>> CreateOvertimeRequestAsync(CreateOvertimeRequestRequest request, string employeeCode);
    Task<ApiResponse<object>> ApproveOvertimeRequestAsync(int requestId, bool isApproved, string? notes, string approvedBy);
}