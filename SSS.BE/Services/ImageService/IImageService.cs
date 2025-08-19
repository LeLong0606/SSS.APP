using SSS.BE.Domain.Entities;
using SSS.BE.Models.Employee;
using SSS.BE.Models.Image;
using Microsoft.AspNetCore.Http;

namespace SSS.BE.Services.ImageService;

/// <summary>
/// Service interface for comprehensive image management
/// </summary>
public interface IImageService
{
    // ===== BASIC IMAGE OPERATIONS =====
    Task<ApiResponse<ImageFile>> UploadImageAsync(IFormFile file, string fileType, string uploadedBy);
    Task<ApiResponse<byte[]>> GetImageAsync(int imageId);
    Task<ApiResponse<byte[]>> GetImageAsync(string fileName);
    Task<ApiResponse<object>> DeleteImageAsync(int imageId, string deletedBy, string reason);

    // ===== EMPLOYEE PHOTO MANAGEMENT =====
    Task<ApiResponse<EmployeePhoto>> SetEmployeePhotoAsync(string employeeCode, IFormFile photoFile, string setBy);
    Task<ApiResponse<EmployeePhoto?>> GetEmployeePhotoAsync(string employeeCode);
    Task<ApiResponse<object>> RemoveEmployeePhotoAsync(string employeeCode, string removedBy);

    // ===== ATTENDANCE PHOTO MANAGEMENT =====
    Task<ApiResponse<AttendancePhoto>> SaveAttendancePhotoAsync(string employeeCode, IFormFile photoFile, 
        string photoType, int? attendanceEventId = null, decimal? latitude = null, decimal? longitude = null);
    Task<ApiResponse<List<AttendancePhoto>>> GetAttendancePhotosAsync(string employeeCode, DateTime? date = null);
    Task<ApiResponse<object>> VerifyAttendancePhotoAsync(int photoId, string verifiedBy, bool isVerified);

    // ===== LEAVE REQUEST ATTACHMENTS =====
    Task<ApiResponse<LeaveRequestAttachment>> AddLeaveAttachmentAsync(int leaveRequestId, IFormFile file, 
        string attachmentType, string? description = null);
    Task<ApiResponse<List<LeaveRequestAttachment>>> GetLeaveAttachmentsAsync(int leaveRequestId);
    Task<ApiResponse<object>> ApproveLeaveAttachmentAsync(int attachmentId, string approvedBy, string? notes = null);

    // ===== IMAGE VALIDATION & PROCESSING =====
    Task<ApiResponse<object>> ValidateImageAsync(IFormFile file);
    Task<ApiResponse<string>> ResizeImageAsync(int imageId, int maxWidth, int maxHeight);
    Task<ApiResponse<object>> CompressImageAsync(int imageId, int quality = 80);

    // ===== AI FACE RECOGNITION (Future) =====
    Task<ApiResponse<object>> AnalyzeFaceAsync(int attendancePhotoId);
    Task<ApiResponse<bool>> CompareFacesAsync(int employeePhotoId, int attendancePhotoId);

    // ===== BULK OPERATIONS =====
    Task<ApiResponse<object>> BulkDeleteImagesAsync(List<int> imageIds, string deletedBy, string reason);
    Task<ApiResponse<object>> CleanupOldImagesAsync(DateTime olderThan);

    // ===== STATISTICS & REPORTS =====
    Task<ApiResponse<object>> GetImageStatisticsAsync();
    Task<ApiResponse<object>> GetEmployeePhotoCompletionAsync();
}