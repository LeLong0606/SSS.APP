using System.ComponentModel.DataAnnotations;

namespace SSS.BE.Domain.Entities;

/// <summary>
/// Comprehensive audit logging for security and compliance
/// </summary>
public class AuditLog
{
    [Key]
    public long Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string TableName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string RecordId { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string Action { get; set; } = string.Empty; // CREATE, UPDATE, DELETE, READ
    
    [Required]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    [MaxLength(450)]
    public string? UserId { get; set; }
    
    [MaxLength(256)]
    public string? UserName { get; set; }
    
    [MaxLength(45)]
    public string? IpAddress { get; set; }
    
    // Data before change (for UPDATE and DELETE)
    public string? OldValues { get; set; }
    
    // Data after change (for CREATE and UPDATE)
    public string? NewValues { get; set; }
    
    // What fields were changed (for UPDATE)
    public string? ChangedFields { get; set; }
    
    // Additional context
    public string? Reason { get; set; }
    public string? ApplicationName { get; set; } = "SSS.BE";
    public string? UserAgent { get; set; }
    
    // Risk assessment
    public string? RiskLevel { get; set; } // LOW, MEDIUM, HIGH, CRITICAL
    public bool IsSuspiciousActivity { get; set; } = false;
    public string? SecurityNotes { get; set; }
}