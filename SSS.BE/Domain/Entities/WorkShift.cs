using System.ComponentModel.DataAnnotations;

namespace SSS.BE.Domain.Entities;

public class WorkShift
{
    [Key]
    public int Id { get; set; }
    
    // Employee relationship
    [Required]
    public string EmployeeCode { get; set; } = string.Empty;
    public virtual Employee Employee { get; set; } = null!;
    
    // Work location relationship
    [Required]
    public int WorkLocationId { get; set; }
    public virtual WorkLocation WorkLocation { get; set; } = null!;
    
    // Shift date and times
    [Required]
    public DateTime ShiftDate { get; set; }
    
    [Required]
    public TimeOnly StartTime { get; set; }
    
    [Required]
    public TimeOnly EndTime { get; set; }
    
    // Calculated total hours (max 8 per day)
    public decimal TotalHours { get; set; }
    
    // Who assigned this shift
    [Required]
    [MaxLength(50)]
    public string AssignedByEmployeeCode { get; set; } = string.Empty;
    public virtual Employee AssignedByEmployee { get; set; } = null!;
    
    // Status and tracking
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    // Modification tracking
    public bool IsModified { get; set; } = false;
    public string? ModifiedByEmployeeCode { get; set; }
    public virtual Employee? ModifiedByEmployee { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public string? ModificationReason { get; set; }
}