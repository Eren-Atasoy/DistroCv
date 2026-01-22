namespace DistroCv.Core.Entities;

/// <summary>
/// Represents a match between a user and a job posting with AI-calculated score
/// </summary>
public class JobMatch
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid JobPostingId { get; set; }
    public decimal MatchScore { get; set; } // 0-100
    public string? MatchReasoning { get; set; } // Gemini explanation
    public string? SkillGaps { get; set; } // JSON array
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
    public bool IsInQueue { get; set; }
    public string Status { get; set; } = "Pending"; // "Pending", "Approved", "Rejected"

    // Navigation
    public User User { get; set; } = null!;
    public JobPosting JobPosting { get; set; } = null!;
    public ICollection<UserFeedback> Feedbacks { get; set; } = new List<UserFeedback>();
}
