using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SSS.BE.Services.ImageService;
using SSS.BE.Models.Employee;
using SSS.BE.Models.Image;
using System.Security.Claims;

namespace SSS.BE.Controllers;

/// <summary>
/// Controller for comprehensive image management - Employee Photos, Attendance Photos, Attachments
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[ApiExplorerSettings(IgnoreApi = true)] // Temporarily exclude from Swagger
public class ImageController : ControllerBase
{
    private readonly ILogger<ImageController> _logger;

    public ImageController(ILogger<ImageController> logger)
    {
        _logger = logger;
    }

    #region Basic Image Operations

    /// <summary>
    /// Upload basic image
    /// </summary>
    [HttpPost("upload")]
    public async Task<ActionResult<ApiResponse<ImageFileDto>>> UploadImage([FromForm] ImageUploadRequest request)
    {
        try
        {
            var uploadedBy = GetCurrentEmployeeCode();
            
            // TODO: Implement image service
            var result = new ApiResponse<ImageFileDto>
            {
                Success = true,
                Message = "Image uploaded successfully",
                Data = new ImageFileDto
                {
                    Id = 1,
                    FileName = $"img_{DateTime.Now:yyyyMMddHHmmss}.jpg",
                    OriginalFileName = request.File.FileName,
                    FilePath = "/uploads/images/img_20241226120000.jpg",
                    ContentType = request.File.ContentType,
                    FileSizeBytes = request.File.Length,
                    FileHash = "abc123def456",
                    Width = 1920,
                    Height = 1080,
                    FileType = request.FileType,
                    UploadedBy = uploadedBy,
                    UploadedByName = "John Doe",
                    UploadedAt = DateTime.UtcNow,
                    IsActive = true,
                    PublicUrl = "/api/image/view/1"
                }
            };

            _logger.LogInformation("Image uploaded successfully by {UploadedBy}: {FileName}", 
                uploadedBy, request.File.FileName);

            return Ok(result);
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
    [HttpGet("view/{imageId}")]
    [AllowAnonymous] // Allow viewing images without authentication
    public async Task<ActionResult> ViewImage(int imageId)
    {
        try
        {
            // TODO: Implement image service
            // For now, return a placeholder response
            return NotFound(new { message = "Image not found or service not implemented yet" });
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
    [HttpDelete("{imageId}")]
    [Authorize(Roles = "Administrator,Director,TeamLeader")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteImage(int imageId, [FromQuery] string reason = "")
    {
        try
        {
            var deletedBy = GetCurrentEmployeeCode();
            
            // TODO: Implement image service
            var result = new ApiResponse<object>
            {
                Success = true,
                Message = "Image deleted successfully",
                Data = new { ImageId = imageId, DeletedBy = deletedBy, Reason = reason }
            };

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
    public async Task<ActionResult<ApiResponse<EmployeePhotoDto>>> SetEmployeePhoto(
        string employeeCode, 
        [FromForm] IFormFile photoFile)
    {
        try
        {
            var setBy = GetCurrentEmployeeCode();
            
            // TODO: Implement image service
            var result = new ApiResponse<EmployeePhotoDto>
            {
                Success = true,
                Message = "Employee avatar photo set successfully",
                Data = new EmployeePhotoDto
                {
                    Id = 1,
                    EmployeeCode = employeeCode,
                    EmployeeName = "John Doe",
                    ImageFile = new ImageFileDto
                    {
                        Id = 1,
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
                    SetByName = "HR Manager",
                    SetAt = DateTime.UtcNow,
                    IsActive = true
                }
            };

            _logger.LogInformation("Employee photo set for {EmployeeCode} by {SetBy}", employeeCode, setBy);

            return Ok(result);
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
    [HttpGet("employee-photo/{employeeCode}")]
    public async Task<ActionResult> GetEmployeePhoto(string employeeCode)
    {
        try
        {
            // TODO: Implement image service
            // For now, return placeholder
            return NotFound(new { message = "Employee photo not found or service not implemented yet" });
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
    public async Task<ActionResult<ApiResponse<EmployeePhotoDto>>> SetMyPhoto([FromForm] IFormFile photoFile)
    {
        try
        {
            var employeeCode = GetCurrentEmployeeCode();
            
            // TODO: Implement image service
            var result = new ApiResponse<EmployeePhotoDto>
            {
                Success = true,
                Message = "Avatar photo updated successfully",
                Data = new EmployeePhotoDto
                {
                    Id = 1,
                    EmployeeCode = employeeCode,
                    EmployeeName = "Current User",
                    ImageFile = new ImageFileDto
                    {
                        Id = 1,
                        FileName = $"emp_{employeeCode}_{DateTime.Now:yyyyMMdd}.jpg",
                        OriginalFileName = photoFile.FileName,
                        ContentType = photoFile.ContentType,
                        FileSizeBytes = photoFile.Length,
                        FileType = "EMPLOYEE_PHOTO",
                        UploadedBy = employeeCode,
                        UploadedAt = DateTime.UtcNow
                    },
                    SetBy = employeeCode,
                    SetAt = DateTime.UtcNow,
                    IsActive = true
                }
            };

            _logger.LogInformation("Employee {EmployeeCode} updated their photo", employeeCode);

            return Ok(result);
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
    public async Task<ActionResult> GetMyPhoto()
    {
        try
        {
            var employeeCode = GetCurrentEmployeeCode();
            
            // TODO: Implement image service
            return NotFound(new { message = "Photo not found or service not implemented yet" });
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
    public async Task<ActionResult<ApiResponse<AttendancePhotoDto>>> SaveAttendancePhoto(
        [FromForm] AttendancePhotoRequest request)
    {
        try
        {
            var employeeCode = GetCurrentEmployeeCode();
            
            // TODO: Implement image service
            var result = new ApiResponse<AttendancePhotoDto>
            {
                Success = true,
                Message = "Attendance photo saved successfully",
                Data = new AttendancePhotoDto
                {
                    Id = 1,
                    EmployeeCode = employeeCode,
                    EmployeeName = "Current User",
                    ImageFile = new ImageFileDto
                    {
                        Id = 1,
                        FileName = $"att_{employeeCode}_{DateTime.Now:yyyyMMddHHmmss}.jpg",
                        OriginalFileName = request.PhotoFile.FileName,
                        ContentType = request.PhotoFile.ContentType,
                        FileSizeBytes = request.PhotoFile.Length,
                        FileType = "ATTENDANCE_PHOTO",
                        UploadedBy = employeeCode,
                        UploadedAt = DateTime.UtcNow
                    },
                    AttendanceEventId = request.AttendanceEventId,
                    PhotoType = request.PhotoType,
                    TakenAt = DateTime.UtcNow,
                    Location = request.Location,
                    Latitude = request.Latitude,
                    Longitude = request.Longitude,
                    DeviceInfo = request.DeviceInfo,
                    IsVerified = false,
                    Notes = request.Notes
                }
            };

            _logger.LogInformation("Attendance photo saved for {EmployeeCode} - Type: {PhotoType}", 
                employeeCode, request.PhotoType);

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
    /// Get employee's attendance photo list
    /// </summary>
    [HttpGet("attendance-photos")]
    public async Task<ActionResult<ApiResponse<List<AttendancePhotoDto>>>> GetMyAttendancePhotos(
        [FromQuery] DateTime? date = null,
        [FromQuery] string? photoType = null)
    {
        try
        {
            var employeeCode = GetCurrentEmployeeCode();
            
            // TODO: Implement image service
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
    /// View employee attendance photos (Manager)
    /// </summary>
    [HttpGet("attendance-photos/{employeeCode}")]
    [Authorize(Roles = "Administrator,Director,TeamLeader")]
    public async Task<ActionResult<ApiResponse<List<AttendancePhotoDto>>>> GetEmployeeAttendancePhotos(
        string employeeCode,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? photoType = null)
    {
        try
        {
            // TODO: Implement image service
            var result = new ApiResponse<List<AttendancePhotoDto>>
            {
                Success = true,
                Message = "Employee attendance photos retrieved successfully",
                Data = new List<AttendancePhotoDto>()
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting employee attendance photos for {EmployeeCode}", employeeCode);
            return StatusCode(500, new ApiResponse<List<AttendancePhotoDto>>
            {
                Success = false,
                Message = "Error retrieving employee attendance photos",
                Data = new List<AttendancePhotoDto>(),
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Verify attendance photo (Manager)
    /// </summary>
    [HttpPost("attendance-photos/{photoId}/verify")]
    [Authorize(Roles = "Administrator,Director,TeamLeader")]
    public async Task<ActionResult<ApiResponse<object>>> VerifyAttendancePhoto(
        int photoId, 
        [FromBody] object verificationData)
    {
        try
        {
            var verifiedBy = GetCurrentEmployeeCode();
            
            // TODO: Implement image service
            var result = new ApiResponse<object>
            {
                Success = true,
                Message = "Attendance photo verified successfully",
                Data = new { PhotoId = photoId, VerifiedBy = verifiedBy, VerifiedAt = DateTime.UtcNow }
            };

            _logger.LogInformation("Attendance photo {PhotoId} verified by {VerifiedBy}", photoId, verifiedBy);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying attendance photo {PhotoId}", photoId);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "Error verifying attendance photo",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    #endregion

    #region Leave Request Attachments

    /// <summary>
    /// Attach file to leave request
    /// </summary>
    [HttpPost("leave-attachment/{leaveRequestId}")]
    public async Task<ActionResult<ApiResponse<LeaveRequestAttachmentDto>>> AddLeaveAttachment(
        int leaveRequestId,
        [FromForm] IFormFile file,
        [FromForm] string attachmentType,
        [FromForm] string? description = null)
    {
        try
        {
            // TODO: Implement image service
            var result = new ApiResponse<LeaveRequestAttachmentDto>
            {
                Success = true,
                Message = "File attached successfully",
                Data = new LeaveRequestAttachmentDto
                {
                    Id = 1,
                    LeaveRequestId = leaveRequestId,
                    ImageFile = new ImageFileDto
                    {
                        Id = 1,
                        FileName = $"leave_{leaveRequestId}_{DateTime.Now:yyyyMMdd}.jpg",
                        OriginalFileName = file.FileName,
                        ContentType = file.ContentType,
                        FileSizeBytes = file.Length,
                        FileType = "LEAVE_ATTACHMENT",
                        UploadedBy = GetCurrentEmployeeCode(),
                        UploadedAt = DateTime.UtcNow
                    },
                    AttachmentType = attachmentType,
                    AttachmentTypeName = GetAttachmentTypeName(attachmentType),
                    Description = description,
                    AttachedAt = DateTime.UtcNow,
                    IsRequired = false,
                    IsApproved = false
                }
            };

            _logger.LogInformation("File attached to leave request {LeaveRequestId}", leaveRequestId);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding leave attachment");
            return StatusCode(500, new ApiResponse<LeaveRequestAttachmentDto>
            {
                Success = false,
                Message = "Error attaching file",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Get leave request attachment list
    /// </summary>
    [HttpGet("leave-attachments/{leaveRequestId}")]
    public async Task<ActionResult<ApiResponse<List<LeaveRequestAttachmentDto>>>> GetLeaveAttachments(int leaveRequestId)
    {
        try
        {
            // TODO: Implement image service
            var result = new ApiResponse<List<LeaveRequestAttachmentDto>>
            {
                Success = true,
                Message = "Attachment list retrieved successfully",
                Data = new List<LeaveRequestAttachmentDto>()
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting leave attachments for {LeaveRequestId}", leaveRequestId);
            return StatusCode(500, new ApiResponse<List<LeaveRequestAttachmentDto>>
            {
                Success = false,
                Message = "Error retrieving attachment list",
                Data = new List<LeaveRequestAttachmentDto>(),
                Errors = new List<string> { ex.Message }
            });
        }
    }

    #endregion

    #region Image Statistics & Management

    /// <summary>
    /// Get image statistics overview (Admin)
    /// </summary>
    [HttpGet("statistics")]
    [Authorize(Roles = "Administrator,Director")]
    public async Task<ActionResult<ApiResponse<ImageStatistics>>> GetImageStatistics()
    {
        try
        {
            // TODO: Implement image service
            var result = new ApiResponse<ImageStatistics>
            {
                Success = true,
                Message = "Image statistics retrieved successfully",
                Data = new ImageStatistics
                {
                    TotalImages = 0,
                    TotalSizeBytes = 0,
                    TotalSizeFormatted = "0 MB",
                    ActiveImages = 0,
                    DeletedImages = 0,
                    ImagesByType = new Dictionary<string, int>(),
                    ImagesByContentType = new Dictionary<string, int>(),
                    EmployeesWithPhotos = 0,
                    TotalEmployees = 0,
                    PhotoCompletionPercentage = 0,
                    AttendancePhotosToday = 0,
                    AttendancePhotosThisWeek = 0,
                    AttendancePhotosThisMonth = 0,
                    LeaveAttachmentsThisMonth = 0,
                    LastImageUpload = DateTime.UtcNow,
                    TopUploaders = new List<TopUploaderDto>()
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

    /// <summary>
    /// Cleanup old images (Admin)
    /// </summary>
    [HttpPost("cleanup")]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<ApiResponse<object>>> CleanupOldImages([FromQuery] int olderThanDays = 365)
    {
        try
        {
            // TODO: Implement image service
            var result = new ApiResponse<object>
            {
                Success = true,
                Message = $"Old images cleanup completed successfully (older than {olderThanDays} days)",
                Data = new { CleanedImages = 0, FreedSpaceBytes = 0 }
            };

            _logger.LogInformation("Image cleanup completed for images older than {Days} days", olderThanDays);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during image cleanup");
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "Error during image cleanup",
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

    private string GetAttachmentTypeName(string attachmentType)
    {
        return attachmentType switch
        {
            "MEDICAL_CERTIFICATE" => "Medical Certificate",
            "PERSONAL_DOCUMENT" => "Personal Document",
            "FAMILY_CERTIFICATE" => "Family Certificate",
            "GOVERNMENT_DOCUMENT" => "Government Document", 
            "SUPPORTING_EVIDENCE" => "Supporting Evidence",
            _ => "Other"
        };
    }

    #endregion
}