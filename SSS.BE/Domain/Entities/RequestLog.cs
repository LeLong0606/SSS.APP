using System.ComponentModel.DataAnnotations;

namespace SSS.BE.Domain.Entities;

/// <summary>
/// Logs all requests for spam detection and rate limiting analysis
/// </summary>
public class RequestLog
{
    [Key]
    public long Id { get; set; }
    
    [Required]
    [MaxLength(45)] // IPv6 support
    public string IpAddress { get; set; } = string.Empty;
    
    [MaxLength(450)]
    public string? UserId { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Endpoint { get; set; } = string.Empty;
    
    [Required]
    public string HttpMethod { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(64)] // SHA-256 hash length
    public string RequestHash { get; set; } = string.Empty;
    
    [Required]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    [MaxLength(500)]
    public string? UserAgent { get; set; }
    
    public int ResponseStatusCode { get; set; }
    
    public long ResponseTimeMs { get; set; }
    
    // Spam detection flags
    public bool IsSpamDetected { get; set; } = false;
    public string? SpamReason { get; set; }
    
    // Request pattern analysis
    public int RequestsInLastMinute { get; set; }
    public int RequestsInLastHour { get; set; }
    public int DuplicateRequestCount { get; set; }
}