namespace DistroCv.Core.Entities;

/// <summary>
/// Represents a job application submitted by a user
/// </summary>
public class Application
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid JobPostingId { get; set; }
    public Guid JobMatchId { get; set; }
    public string? TailoredResumeUrl { get; set; } // S3 URL
    public string? CoverLetter { get; set; }
    public string? CustomMessage { get; set; }
    public string DistributionMethod { get; set; } = "Email"; // "Email", "LinkedIn"
    public string Status { get; set; } = "Queued"; // "Queued", "Sent", "Viewed", "Responded", "Rejected"
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SentAt { get; set; }
    public DateTime? ViewedAt { get; set; }
    public DateTime? RespondedAt { get; set; }

    // Navigation
    public User User { get; set; } = null!;
    public JobPosting JobPosting { get; set; } = null!;
    public JobMatch JobMatch { get; set; } = null!;
    public ICollection<ApplicationLog> Logs { get; set; } = new List<ApplicationLog>();
    public InterviewPreparation? InterviewPreparation { get; set; }
}
