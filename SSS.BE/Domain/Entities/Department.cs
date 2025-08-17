using System.ComponentModel.DataAnnotations;

namespace SSS.BE.Domain.Entities;

public class Department
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(20)]
    public string? DepartmentCode { get; set; }
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();
    
    // Team leader relationship - one department has exactly one team leader
    public string? TeamLeaderId { get; set; }
    public virtual Employee? TeamLeader { get; set; }
}