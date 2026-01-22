using System.ComponentModel.DataAnnotations;

namespace DistroCv.Core.Entities;

/// <summary>
/// Represents an audit log entry for sensitive actions (Task 21.4)
/// </summary>
public class AuditLog
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    
    [Required]
    public string Action { get; set; } = string.Empty;
    
    public string? Resource { get; set; }
    public string? Details { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    // Navigation
    public User? User { get; set; }
}
