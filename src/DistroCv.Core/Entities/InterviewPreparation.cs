namespace DistroCv.Core.Entities;

/// <summary>
/// Represents interview preparation materials for an application
/// </summary>
public class InterviewPreparation
{
    public Guid Id { get; set; }
    public Guid ApplicationId { get; set; }
    public string? Questions { get; set; } // JSON array of 10 questions
    public string? UserAnswers { get; set; } // JSON array
    public string? Feedback { get; set; } // JSON array (STAR-based)
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Application Application { get; set; } = null!;
}
