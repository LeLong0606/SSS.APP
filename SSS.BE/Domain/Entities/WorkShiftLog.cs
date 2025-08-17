using System.ComponentModel.DataAnnotations;

namespace SSS.BE.Domain.Entities;

public class WorkShiftLog
{
    [Key]
    public int Id { get; set; }
    
    // Reference to the work shift
    [Required]
    public int WorkShiftId { get; set; }
    public virtual WorkShift WorkShift { get; set; } = null!;
    
    // Action details
    [Required]
    [MaxLength(50)]
    public string Action { get; set; } = string.Empty; // CREATE, UPDATE, DELETE
    
    [Required]
    [MaxLength(50)]
    public string PerformedByEmployeeCode { get; set; } = string.Empty;
    public virtual Employee PerformedByEmployee { get; set; } = null!;
    
    public DateTime PerformedAt { get; set; } = DateTime.UtcNow;
    
    // Original and new values for updates
    public string? OriginalValues { get; set; } // JSON
    public string? NewValues { get; set; } // JSON
    
    [MaxLength(500)]
    public string? Reason { get; set; }
    
    [MaxLength(1000)]
    public string? Comments { get; set; }
}