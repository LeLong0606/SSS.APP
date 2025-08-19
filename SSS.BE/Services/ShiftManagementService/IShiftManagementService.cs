using SSS.BE.Models.Attendance;
using SSS.BE.Models.Employee;

namespace SSS.BE.Services.ShiftManagementService;

/// <summary>
/// Service interface for shift management (qu?n lý ca làm vi?c)
/// </summary>
public interface IShiftManagementService
{
    // ===== SHIFT TEMPLATES =====
    Task<PagedResponse<ShiftTemplateDto>> GetShiftTemplatesAsync(int pageNumber, int pageSize, bool? isActive);
    Task<ApiResponse<ShiftTemplateDto?>> GetShiftTemplateByIdAsync(int id);
    Task<ApiResponse<ShiftTemplateDto>> CreateShiftTemplateAsync(CreateShiftTemplateRequest request);
    Task<ApiResponse<ShiftTemplateDto>> UpdateShiftTemplateAsync(int id, UpdateShiftTemplateRequest request);
    Task<ApiResponse<object>> DeleteShiftTemplateAsync(int id);

    // ===== SHIFT ASSIGNMENTS =====
    Task<PagedResponse<ShiftAssignmentDto>> GetShiftAssignmentsAsync(
        string? employeeCode, int? shiftTemplateId, DateTime? startDate, DateTime? endDate,
        bool? isActive, int pageNumber, int pageSize);
    Task<ApiResponse<ShiftAssignmentDto>> CreateShiftAssignmentAsync(CreateShiftAssignmentRequest request, string assignedBy);
    Task<ApiResponse<List<ShiftAssignmentDto>>> BulkCreateShiftAssignmentsAsync(BulkShiftAssignmentRequest request, string assignedBy);
    Task<ApiResponse<ShiftAssignmentDto>> UpdateShiftAssignmentAsync(int id, CreateShiftAssignmentRequest request);
    Task<ApiResponse<object>> DeleteShiftAssignmentAsync(int id);

    // ===== SHIFT CALENDAR =====
    Task<ApiResponse<object>> GetEmployeeShiftCalendarAsync(string employeeCode, int year, int month);
    Task<ApiResponse<object>> GenerateShiftCalendarAsync(DateTime startDate, DateTime endDate);
    Task<ApiResponse<object>> GetDepartmentShiftCalendarAsync(int departmentId, DateTime startDate, DateTime endDate);
}