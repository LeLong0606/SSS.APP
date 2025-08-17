using System.ComponentModel.DataAnnotations;

namespace SSS.BE.Domain.Entities;

public class WorkLocation
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(20)]
    public string? LocationCode { get; set; }
    
    [MaxLength(500)]
    public string? Address { get; set; }
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public virtual ICollection<WorkShift> WorkShifts { get; set; } = new List<WorkShift>();
}