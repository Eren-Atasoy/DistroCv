namespace DistroCv.Core.Entities;

/// <summary>
/// Represents a candidate user in the system
/// </summary>
public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? CognitoUserId { get; set; }
    public string PreferredLanguage { get; set; } = "tr"; // "tr" or "en"
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public DigitalTwin? DigitalTwin { get; set; }
    public ICollection<Application> Applications { get; set; } = new List<Application>();
    public ICollection<JobMatch> JobMatches { get; set; } = new List<JobMatch>();
    public ICollection<UserFeedback> Feedbacks { get; set; } = new List<UserFeedback>();
    public ICollection<ThrottleLog> ThrottleLogs { get; set; } = new List<ThrottleLog>();
    public ICollection<UserSession> Sessions { get; set; } = new List<UserSession>();
}
