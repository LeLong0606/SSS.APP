using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SSS.BE.Models.Employee;
using SSS.BE.Models.WorkShift;
using SSS.BE.Services.WorkLocationService;

namespace SSS.BE.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class WorkLocationController : ControllerBase
{
    private readonly IWorkLocationService _workLocationService;
    private readonly ILogger<WorkLocationController> _logger;

    public WorkLocationController(IWorkLocationService workLocationService, ILogger<WorkLocationController> logger)
    {
        _workLocationService = workLocationService;
        _logger = logger;
    }

    /// <summary>
    /// Get all work locations (All authenticated users can view)
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResponse<WorkLocationDto>>> GetWorkLocations(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null)
    {
        var result = await _workLocationService.GetWorkLocationsAsync(pageNumber, pageSize, search);
        return Ok(result);
    }

    /// <summary>
    /// Get work location by ID (All authenticated users can view)
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<WorkLocationDto>>> GetWorkLocation(int id)
    {
        var result = await _workLocationService.GetWorkLocationByIdAsync(id);
        
        if (!result.Success || result.Data == null)
        {
            return NotFound(new ApiResponse<WorkLocationDto>
            {
                Success = false,
                Message = "Work location not found"
            });
        }

        return Ok(result);
    }

    /// <summary>
    /// Create a new work location (Administrator and Director can create)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Administrator,Director")]
    public async Task<ActionResult<ApiResponse<WorkLocationDto>>> CreateWorkLocation([FromBody] CreateWorkLocationRequest request)
    {
        try
        {
            var result = await _workLocationService.CreateWorkLocationAsync(request);
            
            if (!result.Success)
            {
                return BadRequest(result);
            }

            _logger.LogInformation("Work location {LocationName} created successfully", request.Name);

            return CreatedAtAction(nameof(GetWorkLocation), new { id = result.Data!.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponse<WorkLocationDto>
            {
                Success = false,
                Message = ex.Message,
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Update a work location (Administrator and Director can update)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Administrator,Director")]
    public async Task<ActionResult<ApiResponse<WorkLocationDto>>> UpdateWorkLocation(int id, [FromBody] UpdateWorkLocationRequest request)
    {
        try
        {
            var result = await _workLocationService.UpdateWorkLocationAsync(id, request);
            
            if (!result.Success)
            {
                return BadRequest(result);
            }

            _logger.LogInformation("Work location {LocationId} updated successfully", id);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message.Contains("not found"))
            {
                return NotFound(new ApiResponse<WorkLocationDto>
                {
                    Success = false,
                    Message = ex.Message
                });
            }

            return BadRequest(new ApiResponse<WorkLocationDto>
            {
                Success = false,
                Message = ex.Message,
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Delete a work location - soft delete (Administrator only can delete)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteWorkLocation(int id)
    {
        try
        {
            var result = await _workLocationService.DeleteWorkLocationAsync(id);
            
            if (!result.Success)
            {
                return BadRequest(result);
            }

            _logger.LogInformation("Work location {LocationId} deleted successfully", id);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message.Contains("not found"))
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = ex.Message
                });
            }

            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message,
                Errors = new List<string> { ex.Message }
            });
        }
    }
}