using Microsoft.EntityFrameworkCore;
using SSS.BE.Domain.Entities;
using SSS.BE.Models.Employee;
using SSS.BE.Models.Image;
using SSS.BE.Persistence;
using Microsoft.AspNetCore.Http;
using System.Security.Cryptography;

namespace SSS.BE.Services.ImageService;

/// <summary>
/// Comprehensive image management service implementation
/// </summary>
public class ImageService : IImageService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ImageService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _uploadPath;

    public ImageService(
        ApplicationDbContext context,
        ILogger<ImageService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
        _uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
        
        // Ensure upload directory exists
        Directory.CreateDirectory(_uploadPath);
        Directory.CreateDirectory(Path.Combine(_uploadPath, "images"));
        Directory.CreateDirectory(Path.Combine(_uploadPath, "employees"));
        Directory.CreateDirectory(Path.Combine(_uploadPath, "attendance"));
        Directory.CreateDirectory(Path.Combine(_uploadPath, "leave-attachments"));
    }

    #region Basic Image Operations

    public async Task<ApiResponse<ImageFile>> UploadImageAsync(IFormFile file, string fileType, string uploadedBy)
    {
        try
        {
            // Validate file
            var validation = await ValidateImageAsync(file);
            if (!validation.Success)
            {
                return new ApiResponse<ImageFile>
                {
                    Success = false,
                    Message = "Invalid image file",
                    Errors = validation.Errors
                };
            }

            // Generate unique filename
            var fileName = $"{fileType.ToLower()}_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid().ToString("N")[..8]}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(_uploadPath, GetFolderForFileType(fileType), fileName);

            // Calculate file hash
            var fileHash = await CalculateFileHashAsync(file);

            // Check for duplicates
            var existingFile = await _context.ImageFiles
                .FirstOrDefaultAsync(x => x.FileHash == fileHash && x.IsActive);

            if (existingFile != null)
            {
                return new ApiResponse<ImageFile>
                {
                    Success = true,
                    Message = "File already exists",
                    Data = existingFile
                };
            }

            // Save file to disk
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Get image dimensions
            var (width, height) = await GetImageDimensionsAsync(filePath);

            // Create database record
            var imageFile = new ImageFile
            {
                FileName = fileName,
                OriginalFileName = file.FileName,
                FilePath = $"/uploads/{GetFolderForFileType(fileType)}/{fileName}",
                ContentType = file.ContentType,
                FileSizeBytes = file.Length,
                FileHash = fileHash,
                Width = width,
                Height = height,
                FileType = fileType,
                UploadedBy = uploadedBy,
                UploadedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.ImageFiles.Add(imageFile);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Image uploaded successfully: {FileName} by {UploadedBy}", fileName, uploadedBy);

            return new ApiResponse<ImageFile>
            {
                Success = true,
                Message = "Image uploaded successfully",
                Data = imageFile
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading image");
            return new ApiResponse<ImageFile>
            {
                Success = false,
                Message = "Error uploading image",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<ApiResponse<byte[]>> GetImageAsync(int imageId)
    {
        try
        {
            var imageFile = await _context.ImageFiles
                .FirstOrDefaultAsync(x => x.Id == imageId && x.IsActive);

            if (imageFile == null)
            {
                return new ApiResponse<byte[]>
                {
                    Success = false,
                    Message = "Image not found"
                };
            }

            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", imageFile.FilePath.TrimStart('/'));
            
            if (!File.Exists(fullPath))
            {
                return new ApiResponse<byte[]>
                {
                    Success = false,
                    Message = "Image file not found on disk"
                };
            }

            var fileBytes = await File.ReadAllBytesAsync(fullPath);

            return new ApiResponse<byte[]>
            {
                Success = true,
                Message = "Image retrieved successfully",
                Data = fileBytes
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving image {ImageId}", imageId);
            return new ApiResponse<byte[]>
            {
                Success = false,
                Message = "Error retrieving image",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<ApiResponse<byte[]>> GetImageAsync(string fileName)
    {
        try
        {
            var imageFile = await _context.ImageFiles
                .FirstOrDefaultAsync(x => x.FileName == fileName && x.IsActive);

            if (imageFile == null)
            {
                return new ApiResponse<byte[]>
                {
                    Success = false,
                    Message = "Image not found"
                };
            }

            return await GetImageAsync(imageFile.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving image by filename {FileName}", fileName);
            return new ApiResponse<byte[]>
            {
                Success = false,
                Message = "Error retrieving image",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<ApiResponse<object>> DeleteImageAsync(int imageId, string deletedBy, string reason)
    {
        try
        {
            var imageFile = await _context.ImageFiles.FindAsync(imageId);

            if (imageFile == null)
            {
                return new ApiResponse<object>
                {
                    Success = false,
                    Message = "Image not found"
                };
            }

            // Soft delete
            imageFile.IsActive = false;
            imageFile.DeletedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Image {ImageId} soft deleted by {DeletedBy} with reason: {Reason}", 
                imageId, deletedBy, reason);

            return new ApiResponse<object>
            {
                Success = true,
                Message = "Image deleted successfully",
                Data = new { ImageId = imageId, DeletedBy = deletedBy, Reason = reason }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting image {ImageId}", imageId);
            return new ApiResponse<object>
            {
                Success = false,
                Message = "Error deleting image",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    #endregion

    #region Employee Photo Management

    public async Task<ApiResponse<EmployeePhoto>> SetEmployeePhotoAsync(string employeeCode, IFormFile photoFile, string setBy)
    {
        try
        {
            // Upload the image file first
            var uploadResult = await UploadImageAsync(photoFile, "EMPLOYEE_PHOTO", setBy);
            if (!uploadResult.Success)
            {
                return new ApiResponse<EmployeePhoto>
                {
                    Success = false,
                    Message = uploadResult.Message,
                    Errors = uploadResult.Errors
                };
            }

            // Deactivate existing employee photo
            var existingPhoto = await _context.EmployeePhotos
                .FirstOrDefaultAsync(x => x.EmployeeCode == employeeCode && x.IsActive);

            if (existingPhoto != null)
            {
                existingPhoto.IsActive = false;
            }

            // Create new employee photo record
            var employeePhoto = new EmployeePhoto
            {
                EmployeeCode = employeeCode,
                ImageFileId = uploadResult.Data!.Id,
                SetBy = setBy,
                SetAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.EmployeePhotos.Add(employeePhoto);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Employee photo set for {EmployeeCode} by {SetBy}", employeeCode, setBy);

            return new ApiResponse<EmployeePhoto>
            {
                Success = true,
                Message = "Employee photo set successfully",
                Data = employeePhoto
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting employee photo for {EmployeeCode}", employeeCode);
            return new ApiResponse<EmployeePhoto>
            {
                Success = false,
                Message = "Error setting employee photo",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<ApiResponse<EmployeePhoto?>> GetEmployeePhotoAsync(string employeeCode)
    {
        try
        {
            var employeePhoto = await _context.EmployeePhotos
                .Include(x => x.ImageFile)
                .Include(x => x.Employee)
                .FirstOrDefaultAsync(x => x.EmployeeCode == employeeCode && x.IsActive);

            return new ApiResponse<EmployeePhoto?>
            {
                Success = true,
                Message = employeePhoto != null ? "Employee photo found" : "Employee photo not found",
                Data = employeePhoto
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting employee photo for {EmployeeCode}", employeeCode);
            return new ApiResponse<EmployeePhoto?>
            {
                Success = false,
                Message = "Error getting employee photo",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<ApiResponse<object>> RemoveEmployeePhotoAsync(string employeeCode, string removedBy)
    {
        try
        {
            var employeePhoto = await _context.EmployeePhotos
                .FirstOrDefaultAsync(x => x.EmployeeCode == employeeCode && x.IsActive);

            if (employeePhoto == null)
            {
                return new ApiResponse<object>
                {
                    Success = false,
                    Message = "Employee photo not found"
                };
            }

            employeePhoto.IsActive = false;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Employee photo removed for {EmployeeCode} by {RemovedBy}", employeeCode, removedBy);

            return new ApiResponse<object>
            {
                Success = true,
                Message = "Employee photo removed successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing employee photo for {EmployeeCode}", employeeCode);
            return new ApiResponse<object>
            {
                Success = false,
                Message = "Error removing employee photo",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    #endregion

    #region Not Implemented Methods (Placeholder)

    public Task<ApiResponse<AttendancePhoto>> SaveAttendancePhotoAsync(string employeeCode, IFormFile photoFile, string photoType, int? attendanceEventId = null, decimal? latitude = null, decimal? longitude = null)
    {
        throw new NotImplementedException("Attendance photo functionality will be implemented in future versions");
    }

    public Task<ApiResponse<List<AttendancePhoto>>> GetAttendancePhotosAsync(string employeeCode, DateTime? date = null)
    {
        throw new NotImplementedException("Attendance photo functionality will be implemented in future versions");
    }

    public Task<ApiResponse<object>> VerifyAttendancePhotoAsync(int photoId, string verifiedBy, bool isVerified)
    {
        throw new NotImplementedException("Attendance photo functionality will be implemented in future versions");
    }

    public Task<ApiResponse<LeaveRequestAttachment>> AddLeaveAttachmentAsync(int leaveRequestId, IFormFile file, string attachmentType, string? description = null)
    {
        throw new NotImplementedException("Leave attachment functionality will be implemented in future versions");
    }

    public Task<ApiResponse<List<LeaveRequestAttachment>>> GetLeaveAttachmentsAsync(int leaveRequestId)
    {
        throw new NotImplementedException("Leave attachment functionality will be implemented in future versions");
    }

    public Task<ApiResponse<object>> ApproveLeaveAttachmentAsync(int attachmentId, string approvedBy, string? notes = null)
    {
        throw new NotImplementedException("Leave attachment functionality will be implemented in future versions");
    }

    public Task<ApiResponse<object>> ValidateImageAsync(IFormFile file)
    {
        try
        {
            var errors = new List<string>();

            // Check file size (10MB max)
            if (file.Length > 10 * 1024 * 1024)
            {
                errors.Add("File size cannot exceed 10MB");
            }

            // Check file type
            var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
            if (!allowedTypes.Contains(file.ContentType.ToLower()))
            {
                errors.Add("Only image files (JPEG, PNG, GIF, WebP) are allowed");
            }

            // Check file extension
            var extension = Path.GetExtension(file.FileName).ToLower();
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            if (!allowedExtensions.Contains(extension))
            {
                errors.Add("Invalid file extension");
            }

            return Task.FromResult(new ApiResponse<object>
            {
                Success = errors.Count == 0,
                Message = errors.Count == 0 ? "File is valid" : "File validation failed",
                Errors = errors
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new ApiResponse<object>
            {
                Success = false,
                Message = "Error validating file",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    public Task<ApiResponse<string>> ResizeImageAsync(int imageId, int maxWidth, int maxHeight)
    {
        throw new NotImplementedException("Image resizing functionality will be implemented in future versions");
    }

    public Task<ApiResponse<object>> CompressImageAsync(int imageId, int quality = 80)
    {
        throw new NotImplementedException("Image compression functionality will be implemented in future versions");
    }

    public Task<ApiResponse<object>> AnalyzeFaceAsync(int attendancePhotoId)
    {
        throw new NotImplementedException("Face analysis functionality will be implemented in future versions");
    }

    public Task<ApiResponse<bool>> CompareFacesAsync(int employeePhotoId, int attendancePhotoId)
    {
        throw new NotImplementedException("Face comparison functionality will be implemented in future versions");
    }

    public Task<ApiResponse<object>> BulkDeleteImagesAsync(List<int> imageIds, string deletedBy, string reason)
    {
        throw new NotImplementedException("Bulk operations will be implemented in future versions");
    }

    public Task<ApiResponse<object>> CleanupOldImagesAsync(DateTime olderThan)
    {
        throw new NotImplementedException("Cleanup functionality will be implemented in future versions");
    }

    public Task<ApiResponse<object>> GetImageStatisticsAsync()
    {
        throw new NotImplementedException("Statistics functionality will be implemented in future versions");
    }

    public Task<ApiResponse<object>> GetEmployeePhotoCompletionAsync()
    {
        throw new NotImplementedException("Photo completion statistics will be implemented in future versions");
    }

    #endregion

    #region Helper Methods

    private string GetFolderForFileType(string fileType)
    {
        return fileType.ToUpper() switch
        {
            "EMPLOYEE_PHOTO" => "employees",
            "ATTENDANCE_PHOTO" => "attendance", 
            "LEAVE_ATTACHMENT" => "leave-attachments",
            _ => "images"
        };
    }

    private async Task<string> CalculateFileHashAsync(IFormFile file)
    {
        using var sha256 = SHA256.Create();
        using var stream = file.OpenReadStream();
        var hash = await Task.Run(() => sha256.ComputeHash(stream));
        return Convert.ToBase64String(hash);
    }

    private async Task<(int width, int height)> GetImageDimensionsAsync(string filePath)
    {
        try
        {
            // Simple implementation - in production, use a proper image library
            return await Task.FromResult((1920, 1080)); // Default dimensions for now
        }
        catch
        {
            return (0, 0);
        }
    }

    #endregion
}