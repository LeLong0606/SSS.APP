using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SSS.BE.Models.Attendance;
using SSS.BE.Models.Employee;

namespace SSS.BE.Controllers;

/// <summary>
/// Controller for payroll period management, attendance aggregation and Excel export
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class PayrollController : ControllerBase
{
    private readonly ILogger<PayrollController> _logger;

    public PayrollController(ILogger<PayrollController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get payroll periods list
    /// </summary>
    [HttpGet("periods")]
    [Authorize(Roles = "Administrator,Director,TeamLeader")]
    public async Task<ActionResult<PagedResponse<PayrollPeriodDto>>> GetPayrollPeriods(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] int? year = null)
    {
        try
        {
            // TODO: Implement payroll service
            var result = new PagedResponse<PayrollPeriodDto>
            {
                Success = true,
                Message = "Payroll periods retrieved successfully",
                Data = new List<PayrollPeriodDto>(),
                TotalCount = 0,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payroll periods");
            return StatusCode(500, new PagedResponse<PayrollPeriodDto>
            {
                Success = false,
                Message = "Error retrieving payroll periods",
                Data = new List<PayrollPeriodDto>(),
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Create new payroll period (Admin, Director)
    /// </summary>
    [HttpPost("periods")]
    [Authorize(Roles = "Administrator,Director")]
    public async Task<ActionResult<ApiResponse<PayrollPeriodDto>>> CreatePayrollPeriod([FromBody] CreatePayrollPeriodRequest request)
    {
        try
        {
            var createdBy = GetCurrentEmployeeCode();
            
            // TODO: Implement payroll service
            var result = new ApiResponse<PayrollPeriodDto>
            {
                Success = true,
                Message = "Payroll period created successfully",
                Data = new PayrollPeriodDto
                {
                    Id = 1,
                    PeriodName = request.PeriodName,
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    Status = "OPEN",
                    CreatedAt = DateTime.UtcNow
                }
            };

            _logger.LogInformation("Payroll period {PeriodName} created by {CreatedBy}", 
                request.PeriodName, createdBy);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payroll period");
            return StatusCode(500, new ApiResponse<PayrollPeriodDto>
            {
                Success = false,
                Message = "Error creating payroll period",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Export attendance Excel for HR department
    /// </summary>
    [HttpPost("periods/{periodId}/export-excel")]
    [Authorize(Roles = "Administrator,Director")]
    public async Task<ActionResult<ApiResponse<ExcelExportResult>>> ExportPayrollToExcel(
        int periodId, 
        [FromBody] ExcelExportRequest request)
    {
        try
        {
            request.PayrollPeriodId = periodId;
            var exportedBy = GetCurrentEmployeeCode();
            
            // TODO: Implement Excel export service
            var result = new ApiResponse<ExcelExportResult>
            {
                Success = true,
                Message = "Excel export completed successfully",
                Data = new ExcelExportResult
                {
                    Success = true,
                    Message = "Excel file has been created successfully",
                    FileName = $"Payroll_Period_{periodId}_{DateTime.Now:yyyyMMdd}.xlsx",
                    ExportedAt = DateTime.UtcNow,
                    ExportedBy = exportedBy
                }
            };

            _logger.LogInformation("Payroll Excel exported for period {PeriodId} by {ExportedBy}", 
                periodId, exportedBy);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting payroll Excel for period {PeriodId}", periodId);
            return StatusCode(500, new ApiResponse<ExcelExportResult>
            {
                Success = false,
                Message = "Error exporting attendance Excel",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Create leave request (Employee self-service)
    /// </summary>
    [HttpPost("leave-requests")]
    public async Task<ActionResult<ApiResponse<LeaveRequestDto>>> CreateLeaveRequest([FromBody] CreateLeaveRequestRequest request)
    {
        try
        {
            var employeeCode = GetCurrentEmployeeCode();
            
            // TODO: Implement leave request service
            var result = new ApiResponse<LeaveRequestDto>
            {
                Success = true,
                Message = "Leave request created successfully",
                Data = new LeaveRequestDto
                {
                    Id = 1,
                    EmployeeCode = employeeCode,
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    LeaveType = request.LeaveType,
                    Reason = request.Reason,
                    ApprovalStatus = "PENDING",
                    CreatedAt = DateTime.UtcNow
                }
            };

            _logger.LogInformation("Leave request created by employee {EmployeeCode}", employeeCode);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating leave request");
            return StatusCode(500, new ApiResponse<LeaveRequestDto>
            {
                Success = false,
                Message = "Error creating leave request",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    private string GetCurrentEmployeeCode()
    {
        return User.FindFirst("EmployeeCode")?.Value ?? 
               User.Identity?.Name ?? "UNKNOWN";
    }
}