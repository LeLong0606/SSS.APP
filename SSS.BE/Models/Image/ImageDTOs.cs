using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using System.ComponentModel;

namespace SSS.BE.Models.Image;

/// <summary>
/// DTOs for Image Service
/// </summary>
public class ImageUploadRequest
{
    [Required]
    [Description("Image file to upload")]
    public IFormFile File { get; set; } = null!;
    
    [Required]
    [MaxLength(50)]
    [Description("Type of file (e.g., EMPLOYEE_PHOTO, ATTENDANCE_PHOTO, LEAVE_ATTACHMENT)")]
    public string FileType { get; set; } = string.Empty;
    
    [MaxLength(500)]
    [Description("Optional description of the file")]
    public string? Description { get; set; }
    
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Request for uploading attendance photo
/// </summary>
public class AttendancePhotoRequest
{
    [Required]
    [Description("Photo file to upload")]
    public IFormFile PhotoFile { get; set; } = null!;
    
    [Required]
    [MaxLength(20)]
    [Description("Type of attendance photo (CHECK_IN, CHECK_OUT)")]
    public string PhotoType { get; set; } = string.Empty;
    
    [Description("Optional ID of related attendance event")]
    public int? AttendanceEventId { get; set; }
    
    [Description("GPS latitude coordinate")]
    public decimal? Latitude { get; set; }
    
    [Description("GPS longitude coordinate")]
    public decimal? Longitude { get; set; }
    
    [MaxLength(255)]
    [Description("Location description")]
    public string? Location { get; set; }
    
    [MaxLength(500)]
    [Description("Device information")]
    public string? DeviceInfo { get; set; }
    
    [MaxLength(1000)]
    [Description("Additional notes")]
    public string? Notes { get; set; }
}

public class ImageFileDto
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string FileHash { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
    public string FileType { get; set; } = string.Empty;
    public string UploadedBy { get; set; } = string.Empty;
    public string UploadedByName { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
    public bool IsActive { get; set; }
    public string? ThumbnailPath { get; set; }
    public string PublicUrl { get; set; } = string.Empty;
}

public class EmployeePhotoDto
{
    public int Id { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public ImageFileDto ImageFile { get; set; } = null!;
    public string SetBy { get; set; } = string.Empty;
    public string SetByName { get; set; } = string.Empty;
    public DateTime SetAt { get; set; }
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
}

public class AttendancePhotoDto
{
    public int Id { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public ImageFileDto ImageFile { get; set; } = null!;
    public int? AttendanceEventId { get; set; }
    public string PhotoType { get; set; } = string.Empty;
    public DateTime TakenAt { get; set; }
    public string? Location { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? DeviceInfo { get; set; }
    public bool IsVerified { get; set; }
    public string? VerifiedBy { get; set; }
    public string? VerifiedByName { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public string? Notes { get; set; }
    public bool? IsFaceMatched { get; set; }
    public decimal? FaceConfidenceScore { get; set; }
}

public class LeaveRequestAttachmentDto
{
    public int Id { get; set; }
    public int LeaveRequestId { get; set; }
    public ImageFileDto ImageFile { get; set; } = null!;
    public string AttachmentType { get; set; } = string.Empty;
    public string AttachmentTypeName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime AttachedAt { get; set; }
    public bool IsRequired { get; set; }
    public bool IsApproved { get; set; }
    public string? ApprovedBy { get; set; }
    public string? ApprovedByName { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ApprovalNotes { get; set; }
}

/// <summary>
/// Image statistics overview
/// </summary>
public class ImageStatistics
{
    public int TotalImages { get; set; }
    public long TotalSizeBytes { get; set; }
    public string TotalSizeFormatted { get; set; } = string.Empty;
    public int ActiveImages { get; set; }
    public int DeletedImages { get; set; }
    public Dictionary<string, int> ImagesByType { get; set; } = new();
    public Dictionary<string, int> ImagesByContentType { get; set; } = new();
    public int EmployeesWithPhotos { get; set; }
    public int TotalEmployees { get; set; }
    public decimal PhotoCompletionPercentage { get; set; }
    public int AttendancePhotosToday { get; set; }
    public int AttendancePhotosThisWeek { get; set; }
    public int AttendancePhotosThisMonth { get; set; }
    public int LeaveAttachmentsThisMonth { get; set; }
    public DateTime? LastImageUpload { get; set; }
    public string? LastUploadedBy { get; set; }
    public List<TopUploaderDto> TopUploaders { get; set; } = new();
}

/// <summary>
/// Top uploader statistics
/// </summary>
public class TopUploaderDto
{
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public int UploadCount { get; set; }
    public long TotalSizeBytes { get; set; }
    public string TotalSizeFormatted { get; set; } = string.Empty;
}

// Configuration for image handling
public class ImageConfiguration
{
    public string UploadPath { get; set; } = "uploads/images";
    public string ThumbnailPath { get; set; } = "uploads/thumbnails";
    public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024; // 10MB
    public int MaxImageWidth { get; set; } = 4096;
    public int MaxImageHeight { get; set; } = 4096;
    public int ThumbnailWidth { get; set; } = 300;
    public int ThumbnailHeight { get; set; } = 300;
    public int ImageQuality { get; set; } = 85;
    public List<string> AllowedExtensions { get; set; } = new() { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
    public List<string> AllowedContentTypes { get; set; } = new() 
    { 
        "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" 
    };
    public bool EnableAutoResize { get; set; } = true;
    public bool EnableCompression { get; set; } = true;
    public bool GenerateThumbnails { get; set; } = true;
    public int CleanupAfterDays { get; set; } = 365; // Clean deleted images after 1 year
}