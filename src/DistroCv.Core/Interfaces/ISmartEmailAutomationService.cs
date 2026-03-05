namespace DistroCv.Core.Interfaces;

/// <summary>
/// Main orchestrator for the Smart Email Automation Engine.
/// Coordinates CV analysis, email generation, and queue scheduling
/// for a batch of job applications.
/// Layer: Application (Core)
/// </summary>
public interface ISmartEmailAutomationService
{
    /// <summary>
    /// Orchestrates the full email automation pipeline for a single job posting:
    /// 1. Analyzes CV against job description (Gemini API)
    /// 2. Generates personalized plain-text email with spintax
    /// 3. Generates CV presigned URL from S3
    /// 4. Enqueues via IEmailQueueService with jitter and daily limits
    /// </summary>
    /// <param name="request">Automation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created email job</returns>
    Task<SmartEmailResult> ProcessAsync(
        SmartEmailRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes a batch of job postings for a user (up to daily limit)
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="jobPostingIds">List of target job posting IDs</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Results for each processed job posting</returns>
    Task<List<SmartEmailResult>> ProcessBatchAsync(
        Guid userId,
        List<Guid> jobPostingIds,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Request to create a smart automated email
/// </summary>
public class SmartEmailRequest
{
    public Guid UserId { get; set; }
    public Guid JobPostingId { get; set; }
    public Guid? ApplicationId { get; set; }
}

/// <summary>
/// Result of a smart email automation request
/// </summary>
public class SmartEmailResult
{
    public bool IsSuccess { get; set; }
    public Guid? EmailJobId { get; set; }
    public Guid JobPostingId { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? ScheduledAtUtc { get; set; }
}
