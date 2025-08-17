using System.ComponentModel.DataAnnotations;

namespace SSS.BE.Domain.Entities;

/// <summary>
/// Tracks duplicate data attempts across all entities
/// </summary>
public class DuplicateDetectionLog
{
    [Key]
    public long Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string EntityType { get; set; } = string.Empty; // Employee, Department, WorkShift, etc.
    
    [Required]
    [MaxLength(50)]
    public string EntityId { get; set; } = string.Empty; // The ID or unique key being duplicated
    
    [Required]
    [MaxLength(64)] // SHA-256 hash of the data being inserted
    public string DataHash { get; set; } = string.Empty;
    
    [Required]
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
    
    [MaxLength(450)]
    public string? UserId { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string Action { get; set; } = string.Empty; // CREATE, UPDATE
    
    // The original data that was attempted to be duplicated
    public string? OriginalData { get; set; }
    
    // The duplicate data that was blocked
    public string? DuplicateData { get; set; }
    
    // IP address for tracking malicious attempts
    [MaxLength(45)]
    public string? IpAddress { get; set; }
    
    // How the duplicate was detected
    public string? DetectionMethod { get; set; } // INDEX_VIOLATION, HASH_MATCH, BUSINESS_LOGIC
    
    // Was this attempt blocked or allowed
    public bool WasBlocked { get; set; } = true;
    
    // Additional context
    public string? Notes { get; set; }
}