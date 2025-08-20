using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SSS.BE.Models.Employee;
using SSS.BE.Models.Image;
using SSS.BE.Services.ImageService;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace SSS.BE.Controllers;

/// <summary>
/// Controller for comprehensive image management - Employee Photos, Attendance Photos, Attachments
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class ImageController : ControllerBase
{
    private readonly ILogger<ImageController> _logger;
    private readonly IImageService _imageService;

    public ImageController(ILogger<ImageController> logger, IImageService imageService)
    {
        _logger = logger;
        _imageService = imageService;
    }

    #region Basic Image Operations

    /// <summary>
    /// Upload basic image
    /// </summary>
    [HttpPost("upload")]
    public async Task<ActionResult<ApiResponse<ImageFileDto>>> UploadImage(IFormFile file, string fileType = "IMAGE", string? description = null)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new ApiResponse<ImageFileDto>
                {
                    Success = false,
                    Message = "No file uploaded",
                    Errors = new List<string> { "File is required" }
                });
            }

            var uploadedBy = GetCurrentEmployeeCode();
            
            // Use the image service to upload
            var result = await _imageService.UploadImageAsync(file, fileType, uploadedBy);
            
            if (!result.Success)
            {
                return BadRequest(new ApiResponse<ImageFileDto>
                {
                    Success = false,
                    Message = result.Message,
                    Errors = result.Errors
                });
            }

            // Convert to DTO
            var imageFileDto = new ImageFileDto
            {
                Id = result.Data!.Id,
                FileName = result.Data.FileName,
                OriginalFileName = result.Data.OriginalFileName,
                FilePath = result.Data.FilePath,
                ContentType = result.Data.ContentType,
                FileSizeBytes = result.Data.FileSizeBytes,
                FileHash = result.Data.FileHash,
                Width = result.Data.Width,
                Height = result.Data.Height,
                FileType = result.Data.FileType,
                UploadedBy = result.Data.UploadedBy,
                UploadedByName = "User", // TODO: Get actual user name
                UploadedAt = result.Data.UploadedAt,
                IsActive = result.Data.IsActive,
                PublicUrl = $"/api/image/view/{result.Data.Id}"
            };

            _logger.LogInformation("Image uploaded successfully by {UploadedBy}: {FileName}", 
                uploadedBy, file.FileName);

            return Ok(new ApiResponse<ImageFileDto>
            {
                Success = true,
                Message = "Image uploaded successfully",
                Data = imageFileDto
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading image");
            return StatusCode(500, new ApiResponse<ImageFileDto>
            {
                Success = false,
                Message = "Error uploading image",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// View image by ID
    /// </summary>
    /// <param name="imageId">ID of the image to view</param>
    [HttpGet("view/{imageId}")]
    [AllowAnonymous] // Allow viewing images without authentication
    [ProducesResponseType(typeof(FileResult), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult> ViewImage(int imageId)
    {
        try
        {
            var result = await _imageService.GetImageAsync(imageId);
            
            if (!result.Success || result.Data == null)
            {
                return NotFound(new { message = "Image not found" });
            }

            // Return the actual image bytes with proper content type
            return File(result.Data, "image/jpeg"); // Default to JPEG, could be improved to detect actual type
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error viewing image {ImageId}", imageId);
            return StatusCode(500, new { message = "Error viewing image" });
        }
    }

    /// <summary>
    /// Delete image
    /// </summary>
    /// <param name="imageId">ID of the image to delete</param>
    /// <param name="reason">Reason for deletion</param>
    [HttpDelete("{imageId}")]
    [Authorize(Roles = "Administrator,Director,TeamLeader")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<ApiResponse<object>>> DeleteImage(int imageId, [FromQuery] string reason = "")
    {
        try
        {
            var deletedBy = GetCurrentEmployeeCode();
            
            var result = await _imageService.DeleteImageAsync(imageId, deletedBy, reason);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            _logger.LogInformation("Image {ImageId} deleted by {DeletedBy} with reason: {Reason}", 
                imageId, deletedBy, reason);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting image {ImageId}", imageId);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "Error deleting image",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    #endregion

    #region Employee Photo Management

    /// <summary>
    /// Set employee avatar photo
    /// </summary>
    [HttpPost("employee-photo/{employeeCode}")]
    [Authorize(Roles = "Administrator,Director,TeamLeader")]
    public async Task<ActionResult<ApiResponse<EmployeePhotoDto>>> SetEmployeePhoto(string employeeCode, IFormFile photoFile)
    {
        try
        {
            if (photoFile == null || photoFile.Length == 0)
            {
                return BadRequest(new ApiResponse<EmployeePhotoDto>
                {
                    Success = false,
                    Message = "No photo file uploaded",
                    Errors = new List<string> { "Photo file is required" }
                });
            }

            var setBy = GetCurrentEmployeeCode();
            
            var result = await _imageService.SetEmployeePhotoAsync(employeeCode, photoFile, setBy);
            
            if (!result.Success)
            {
                return BadRequest(new ApiResponse<EmployeePhotoDto>
                {
                    Success = false,
                    Message = result.Message,
                    Errors = result.Errors
                });
            }

            // Convert to DTO (simplified for now)
            var employeePhotoDto = new EmployeePhotoDto
            {
                Id = result.Data!.Id,
                EmployeeCode = result.Data.EmployeeCode,
                EmployeeName = "Employee", // TODO: Get actual employee name
                ImageFile = new ImageFileDto
                {
                    Id = result.Data.ImageFileId,
                    FileName = $"emp_{employeeCode}_{DateTime.Now:yyyyMMdd}.jpg",
                    OriginalFileName = photoFile.FileName,
                    FilePath = $"/uploads/employees/{employeeCode}.jpg",
                    ContentType = photoFile.ContentType,
                    FileSizeBytes = photoFile.Length,
                    FileType = "EMPLOYEE_PHOTO",
                    UploadedBy = setBy,
                    UploadedAt = DateTime.UtcNow,
                    PublicUrl = $"/api/image/employee-photo/{employeeCode}"
                },
                SetBy = setBy,
                SetByName = "Manager", // TODO: Get actual user name
                SetAt = result.Data.SetAt,
                IsActive = result.Data.IsActive
            };

            _logger.LogInformation("Employee photo set for {EmployeeCode} by {SetBy}", employeeCode, setBy);

            return Ok(new ApiResponse<EmployeePhotoDto>
            {
                Success = true,
                Message = "Employee avatar photo set successfully",
                Data = employeePhotoDto
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting employee photo for {EmployeeCode}", employeeCode);
            return StatusCode(500, new ApiResponse<EmployeePhotoDto>
            {
                Success = false,
                Message = "Error setting employee avatar photo",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Get employee avatar photo
    /// </summary>
    /// <param name="employeeCode">Code of the employee</param>
    [HttpGet("employee-photo/{employeeCode}")]
    [ProducesResponseType(typeof(FileResult), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult> GetEmployeePhoto(string employeeCode)
    {
        try
        {
            var result = await _imageService.GetEmployeePhotoAsync(employeeCode);
            
            if (!result.Success || result.Data == null)
            {
                return NotFound(new { message = "Employee photo not found" });
            }

            // Get the actual image bytes
            var imageResult = await _imageService.GetImageAsync(result.Data.ImageFileId);
            if (!imageResult.Success || imageResult.Data == null)
            {
                return NotFound(new { message = "Image file not found" });
            }

            return File(imageResult.Data, result.Data.ImageFile?.ContentType ?? "image/jpeg");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting employee photo for {EmployeeCode}", employeeCode);
            return StatusCode(500, new { message = "Error retrieving employee avatar photo" });
        }
    }

    /// <summary>
    /// Employee uploads their own avatar photo
    /// </summary>
    [HttpPost("my-photo")]
    public async Task<ActionResult<ApiResponse<EmployeePhotoDto>>> SetMyPhoto(IFormFile photoFile)
    {
        try
        {
            if (photoFile == null || photoFile.Length == 0)
            {
                return BadRequest(new ApiResponse<EmployeePhotoDto>
                {
                    Success = false,
                    Message = "No photo file uploaded",
                    Errors = new List<string> { "Photo file is required" }
                });
            }

            var employeeCode = GetCurrentEmployeeCode();
            
            var result = await _imageService.SetEmployeePhotoAsync(employeeCode, photoFile, employeeCode);
            
            if (!result.Success)
            {
                return BadRequest(new ApiResponse<EmployeePhotoDto>
                {
                    Success = false,
                    Message = result.Message,
                    Errors = result.Errors
                });
            }

            // Convert to DTO (simplified)
            var employeePhotoDto = new EmployeePhotoDto
            {
                Id = result.Data!.Id,
                EmployeeCode = employeeCode,
                EmployeeName = "Current User", // TODO: Get actual name
                ImageFile = new ImageFileDto
                {
                    Id = result.Data.ImageFileId,
                    FileName = $"emp_{employeeCode}_{DateTime.Now:yyyyMMdd}.jpg",
                    OriginalFileName = photoFile.FileName,
                    ContentType = photoFile.ContentType,
                    FileSizeBytes = photoFile.Length,
                    FileType = "EMPLOYEE_PHOTO",
                    UploadedBy = employeeCode,
                    UploadedAt = DateTime.UtcNow
                },
                SetBy = employeeCode,
                SetAt = result.Data.SetAt,
                IsActive = result.Data.IsActive
            };

            _logger.LogInformation("Employee {EmployeeCode} updated their photo", employeeCode);

            return Ok(new ApiResponse<EmployeePhotoDto>
            {
                Success = true,
                Message = "Avatar photo updated successfully",
                Data = employeePhotoDto
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting own photo");
            return StatusCode(500, new ApiResponse<EmployeePhotoDto>
            {
                Success = false,
                Message = "Error updating avatar photo",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Get own avatar photo
    /// </summary>
    [HttpGet("my-photo")]
    [ProducesResponseType(typeof(FileResult), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult> GetMyPhoto()
    {
        try
        {
            var employeeCode = GetCurrentEmployeeCode();
            return await GetEmployeePhoto(employeeCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting own photo");
            return StatusCode(500, new { message = "Error retrieving avatar photo" });
        }
    }

    #endregion

    #region Attendance Photo Management

    /// <summary>
    /// Take photo during attendance (Check-In/Check-Out selfie)
    /// </summary>
    [HttpPost("attendance-photo")]
    public async Task<ActionResult<ApiResponse<AttendancePhotoDto>>> SaveAttendancePhoto(IFormFile photoFile, string photoType = "CHECK_IN")
    {
        try
        {
            if (photoFile == null || photoFile.Length == 0)
            {
                return BadRequest(new ApiResponse<AttendancePhotoDto>
                {
                    Success = false,
                    Message = "No photo file uploaded",
                    Errors = new List<string> { "Photo file is required" }
                });
            }

            // Placeholder implementation - return success for now
            var employeeCode = GetCurrentEmployeeCode();
            
            var result = new ApiResponse<AttendancePhotoDto>
            {
                Success = true,
                Message = "Attendance photo saved successfully (placeholder)",
                Data = new AttendancePhotoDto
                {
                    Id = 1,
                    EmployeeCode = employeeCode,
                    EmployeeName = "Current User",
                    ImageFile = new ImageFileDto
                    {
                        Id = 1,
                        FileName = $"att_{employeeCode}_{DateTime.Now:yyyyMMddHHmmss}.jpg",
                        OriginalFileName = photoFile.FileName,
                        ContentType = photoFile.ContentType,
                        FileSizeBytes = photoFile.Length,
                        FileType = "ATTENDANCE_PHOTO",
                        UploadedBy = employeeCode,
                        UploadedAt = DateTime.UtcNow
                    },
                    PhotoType = photoType,
                    TakenAt = DateTime.UtcNow,
                    IsVerified = false
                }
            };

            _logger.LogInformation("Attendance photo saved (placeholder) for {EmployeeCode} - Type: {PhotoType}", 
                employeeCode, photoType);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving attendance photo");
            return StatusCode(500, new ApiResponse<AttendancePhotoDto>
            {
                Success = false,
                Message = "Error saving attendance photo",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Get my attendance photos
    /// </summary>
    /// <param name="date">Optional date filter</param>
    /// <param name="photoType">Optional photo type filter</param>
    [HttpGet("attendance-photos")]
    [ProducesResponseType(typeof(ApiResponse<List<AttendancePhotoDto>>), 200)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<ApiResponse<List<AttendancePhotoDto>>>> GetMyAttendancePhotos(
        [FromQuery] DateTime? date = null,
        [FromQuery] string? photoType = null)
    {
        try
        {
            var employeeCode = GetCurrentEmployeeCode();
            
            var result = new ApiResponse<List<AttendancePhotoDto>>
            {
                Success = true,
                Message = "Attendance photos retrieved successfully",
                Data = new List<AttendancePhotoDto>()
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting attendance photos");
            return StatusCode(500, new ApiResponse<List<AttendancePhotoDto>>
            {
                Success = false,
                Message = "Error retrieving attendance photos",
                Data = new List<AttendancePhotoDto>(),
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Get image statistics overview
    /// </summary>
    [HttpGet("statistics")]
    [Authorize(Roles = "Administrator,Director")]
    [ProducesResponseType(typeof(ApiResponse<ImageStatistics>), 200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<ApiResponse<ImageStatistics>>> GetImageStatistics()
    {
        try
        {
            // Placeholder implementation - return mock statistics for now
            var result = new ApiResponse<ImageStatistics>
            {
                Success = true,
                Message = "Image statistics retrieved successfully",
                Data = new ImageStatistics
                {
                    TotalImages = 150,
                    TotalSizeBytes = 52428800, // 50 MB
                    TotalSizeFormatted = "50.0 MB",
                    ActiveImages = 145,
                    DeletedImages = 5,
                    ImagesByType = new Dictionary<string, int>
                    {
                        ["EMPLOYEE_PHOTO"] = 75,
                        ["ATTENDANCE_PHOTO"] = 60,
                        ["LEAVE_ATTACHMENT"] = 10,
                        ["OTHER"] = 5
                    },
                    ImagesByContentType = new Dictionary<string, int>
                    {
                        ["image/jpeg"] = 120,
                        ["image/png"] = 25,
                        ["image/webp"] = 5
                    },
                    EmployeesWithPhotos = 70,
                    TotalEmployees = 100,
                    PhotoCompletionPercentage = 70.0m,
                    AttendancePhotosToday = 12,
                    AttendancePhotosThisWeek = 85,
                    AttendancePhotosThisMonth = 320,
                    LeaveAttachmentsThisMonth = 8,
                    LastImageUpload = DateTime.UtcNow.AddMinutes(-30),
                    LastUploadedBy = GetCurrentEmployeeCode(),
                    TopUploaders = new List<TopUploaderDto>
                    {
                        new() { EmployeeCode = "EMP001", EmployeeName = "John Doe", UploadCount = 25, TotalSizeBytes = 10485760, TotalSizeFormatted = "10.0 MB" },
                        new() { EmployeeCode = "EMP002", EmployeeName = "Jane Smith", UploadCount = 18, TotalSizeBytes = 7340032, TotalSizeFormatted = "7.0 MB" }
                    }
                }
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting image statistics");
            return StatusCode(500, new ApiResponse<ImageStatistics>
            {
                Success = false,
                Message = "Error retrieving image statistics",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    #endregion

    #region Helper Methods

    private string GetCurrentEmployeeCode()
    {
        return User.FindFirst("EmployeeCode")?.Value ?? 
               User.FindFirst(ClaimTypes.Name)?.Value ?? 
               User.Identity?.Name ?? "UNKNOWN";
    }

    #endregion
}