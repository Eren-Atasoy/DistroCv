namespace DistroCv.Core.Entities;

/// <summary>
/// Represents a log of throttled actions for anti-bot protection
/// </summary>
public class ThrottleLog
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string ActionType { get; set; } = string.Empty; // "LinkedInConnection", "LinkedInMessage", "Application"
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Platform { get; set; } = string.Empty; // "LinkedIn", "Email"

    // Navigation
    public User User { get; set; } = null!;
}
