using System.ComponentModel.DataAnnotations;

namespace DistroCv.Core.Entities;

/// <summary>
/// Represents user consent for data processing (Task 21.6)
/// </summary>
public class UserConsent
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    
    [Required]
    public string ConsentType { get; set; } = string.Empty; // e.g., "Marketing", "Analytics", "DataProcessing"
    
    public bool IsGiven { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    
    // Navigation
    public User User { get; set; } = null!;
}
