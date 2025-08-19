using System.ComponentModel.DataAnnotations;

namespace SSS.BE.Domain.Entities;

/// <summary>
/// B?ng qu?n lý file hình ?nh t?ng quan
/// </summary>
public class ImageFile
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string FileName { get; set; } = string.Empty; // File name trên server
    
    [Required]
    [MaxLength(255)]
    public string OriginalFileName { get; set; } = string.Empty; // Tên file g?c khi upload
    
    [Required]
    [MaxLength(500)]
    public string FilePath { get; set; } = string.Empty; // ???ng d?n file trên server
    
    [Required]
    [MaxLength(100)]
    public string ContentType { get; set; } = string.Empty; // image/jpeg, image/png, etc.
    
    [Required]
    public long FileSizeBytes { get; set; } // Kích th??c file (bytes)
    
    [Required]
    [MaxLength(64)]
    public string FileHash { get; set; } = string.Empty; // SHA-256 hash ?? ki?m tra duplicate
    
    public int Width { get; set; } // Chi?u r?ng ?nh (pixels)
    
    public int Height { get; set; } // Chi?u cao ?nh (pixels)
    
    [Required]
    [MaxLength(50)]
    public string FileType { get; set; } = string.Empty; // EMPLOYEE_PHOTO, ATTENDANCE_PHOTO, LEAVE_ATTACHMENT, etc.
    
    [Required]
    [MaxLength(50)]
    public string UploadedBy { get; set; } = string.Empty; // Employee code c?a ng??i upload
    
    [Required]
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    
    public bool IsActive { get; set; } = true;
    
    public DateTime? DeletedAt { get; set; }
    
    [MaxLength(50)]
    public string? DeletedBy { get; set; }
    
    [MaxLength(500)]
    public string? DeletedReason { get; set; }
    
    // Navigation properties
    public virtual Employee UploadedByEmployee { get; set; } = null!;
    public virtual Employee? DeletedByEmployee { get; set; }
}

/// <summary>
/// ?nh ??i di?n c?a nhân viên
/// </summary>
public class EmployeePhoto
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string EmployeeCode { get; set; } = string.Empty;
    
    [Required]
    public int ImageFileId { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string SetBy { get; set; } = string.Empty; // Ng??i set ?nh (có th? là chính nhân viên ho?c HR)
    
    [Required]
    public DateTime SetAt { get; set; } = DateTime.UtcNow;
    
    public bool IsActive { get; set; } = true;
    
    [MaxLength(500)]
    public string? Notes { get; set; }
    
    // Navigation properties
    public virtual Employee Employee { get; set; } = null!;
    public virtual ImageFile ImageFile { get; set; } = null!;
    public virtual Employee SetByEmployee { get; set; } = null!;
}

/// <summary>
/// ?nh ch?p khi ch?m công (selfie check-in/out)
/// </summary>
public class AttendancePhoto
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string EmployeeCode { get; set; } = string.Empty;
    
    [Required]
    public int ImageFileId { get; set; }
    
    public int? AttendanceEventId { get; set; } // Liên k?t v?i s? ki?n ch?m công
    
    [Required]
    [MaxLength(20)]
    public string PhotoType { get; set; } = string.Empty; // CHECK_IN, CHECK_OUT, BREAK_START, BREAK_END
    
    [Required]
    public DateTime TakenAt { get; set; } = DateTime.UtcNow;
    
    [MaxLength(200)]
    public string? Location { get; set; } // Tên ??a ?i?m
    
    public decimal? Latitude { get; set; } // GPS latitude
    
    public decimal? Longitude { get; set; } // GPS longitude
    
    [MaxLength(200)]
    public string? DeviceInfo { get; set; } // Thông tin thi?t b? ch?p
    
    public bool IsVerified { get; set; } = false; // ?nh ?ã ???c xác th?c ch?a
    
    [MaxLength(50)]
    public string? VerifiedBy { get; set; }
    
    public DateTime? VerifiedAt { get; set; }
    
    [MaxLength(500)]
    public string? Notes { get; set; }
    
    // AI Face Recognition (future enhancement)
    public bool? IsFaceMatched { get; set; }
    
    public decimal? FaceConfidenceScore { get; set; } // 0.0 - 1.0
    
    [MaxLength(1000)]
    public string? FaceAnalysisData { get; set; } // JSON data t? AI analysis
    
    // Navigation properties
    public virtual Employee Employee { get; set; } = null!;
    public virtual ImageFile ImageFile { get; set; } = null!;
    public virtual AttendanceEvent? AttendanceEvent { get; set; }
    public virtual Employee? VerifiedByEmployee { get; set; }
}

/// <summary>
/// File ?ính kèm trong ??n ngh? phép
/// </summary>
public class LeaveRequestAttachment
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int LeaveRequestId { get; set; }
    
    [Required]
    public int ImageFileId { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string AttachmentType { get; set; } = string.Empty; // MEDICAL_CERTIFICATE, PERSONAL_DOCUMENT, OTHER
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    [Required]
    public DateTime AttachedAt { get; set; } = DateTime.UtcNow;
    
    public bool IsRequired { get; set; } = false; // Có ph?i là file b?t bu?c không
    
    public bool IsApproved { get; set; } = false;
    
    [MaxLength(50)]
    public string? ApprovedBy { get; set; }
    
    public DateTime? ApprovedAt { get; set; }
    
    [MaxLength(500)]
    public string? ApprovalNotes { get; set; }
    
    // Navigation properties
    public virtual LeaveRequest LeaveRequest { get; set; } = null!;
    public virtual ImageFile ImageFile { get; set; } = null!;
    public virtual Employee? ApprovedByEmployee { get; set; }
}

/// <summary>
/// Enum cho các lo?i file ?nh
/// </summary>
public static class ImageFileTypes
{
    public const string EMPLOYEE_PHOTO = "EMPLOYEE_PHOTO";
    public const string ATTENDANCE_PHOTO = "ATTENDANCE_PHOTO";
    public const string LEAVE_ATTACHMENT = "LEAVE_ATTACHMENT";
    public const string OVERTIME_ATTACHMENT = "OVERTIME_ATTACHMENT";
    public const string DOCUMENT_SCAN = "DOCUMENT_SCAN";
    public const string PROFILE_AVATAR = "PROFILE_AVATAR";
    public const string SIGNATURE = "SIGNATURE";
    public const string OTHER = "OTHER";
}

/// <summary>
/// Enum cho các lo?i ?nh ch?m công
/// </summary>
public static class AttendancePhotoTypes
{
    public const string CHECK_IN = "CHECK_IN";
    public const string CHECK_OUT = "CHECK_OUT";
    public const string BREAK_START = "BREAK_START";
    public const string BREAK_END = "BREAK_END";
    public const string OVERTIME_START = "OVERTIME_START";
    public const string OVERTIME_END = "OVERTIME_END";
    public const string MANUAL_VERIFICATION = "MANUAL_VERIFICATION";
}

/// <summary>
/// Enum cho các lo?i attachment trong leave request
/// </summary>
public static class LeaveAttachmentTypes
{
    public const string MEDICAL_CERTIFICATE = "MEDICAL_CERTIFICATE";
    public const string PERSONAL_DOCUMENT = "PERSONAL_DOCUMENT";
    public const string FAMILY_CERTIFICATE = "FAMILY_CERTIFICATE";
    public const string GOVERNMENT_DOCUMENT = "GOVERNMENT_DOCUMENT";
    public const string SUPPORTING_EVIDENCE = "SUPPORTING_EVIDENCE";
    public const string OTHER = "OTHER";
}