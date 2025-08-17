using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SSS.BE.Domain.Entities;

public class Employee
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string EmployeeCode { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(200)]
    public string FullName { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string? Position { get; set; }
    
    [MaxLength(20)]
    public string? PhoneNumber { get; set; }
    
    [MaxLength(200)]
    public string? Address { get; set; }
    
    public DateTime? HireDate { get; set; }
    
    public decimal? Salary { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    // Department relationship
    public int? DepartmentId { get; set; }
    public virtual Department? Department { get; set; }
    
    // Self-referencing relationship for team leadership
    public bool IsTeamLeader { get; set; } = false;
    
    // One-to-one relationship with AspNetUsers via EmployeeCode
    // This will be configured in the DbContext
}