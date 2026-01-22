namespace DistroCv.Core.Entities;

/// <summary>
/// Represents user feedback on rejected job matches for learning system
/// </summary>
public class UserFeedback
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid JobMatchId { get; set; }
    public string FeedbackType { get; set; } = "Rejected"; // "Rejected"
    public string? Reason { get; set; } // "Low Salary", "Old Tech", etc.
    public string? AdditionalNotes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User User { get; set; } = null!;
    public JobMatch JobMatch { get; set; } = null!;
}
