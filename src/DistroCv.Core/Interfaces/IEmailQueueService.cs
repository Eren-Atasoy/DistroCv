using DistroCv.Core.Entities;
using DistroCv.Core.Enums;

namespace DistroCv.Core.Interfaces;

/// <summary>
/// Manages the email job queue: creates EmailJob records,
/// schedules them via Hangfire with jitter, and enforces daily limits.
/// Layer: Application (Core)
/// </summary>
public interface IEmailQueueService
{
    /// <summary>
    /// Enqueues a new email job: saves to DB as Pending, then schedules
    /// via Hangfire with random jitter (5-15 min between emails).
    /// Enforces daily limit of 40 emails per user and business-hours constraint.
    /// </summary>
    /// <param name="request">Email queue request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created EmailJob entity</returns>
    Task<EmailJob> EnqueueEmailAsync(
        EnqueueEmailRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the number of emails sent by a user today
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Count of emails sent or scheduled today</returns>
    Task<int> GetDailySendCountAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user can send more emails today (under daily limit of 40)
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if under the daily limit</returns>
    Task<bool> CanSendMoreTodayAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all pending/scheduled email jobs for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of email jobs</returns>
    Task<List<EmailJob>> GetPendingJobsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a pending/scheduled email job
    /// </summary>
    /// <param name="emailJobId">Email job ID</param>
    /// <param name="userId">User ID (for authorization)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if cancelled successfully</returns>
    Task<bool> CancelEmailJobAsync(Guid emailJobId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the email queue status/statistics for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Queue statistics</returns>
    Task<EmailQueueStats> GetQueueStatsAsync(Guid userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Request to enqueue a new email
/// </summary>
public class EnqueueEmailRequest
{
    public Guid UserId { get; set; }
    public Guid JobPostingId { get; set; }
    public Guid? ApplicationId { get; set; }
    public string RecipientEmail { get; set; } = string.Empty;
    public string RecipientName { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? CvPresignedUrl { get; set; }
}

/// <summary>
/// Queue statistics for a user
/// </summary>
public class EmailQueueStats
{
    public int TotalToday { get; set; }
    public int SentToday { get; set; }
    public int PendingCount { get; set; }
    public int ScheduledCount { get; set; }
    public int FailedToday { get; set; }
    public int DailyLimit { get; set; } = 40;
    public int RemainingToday => Math.Max(0, DailyLimit - TotalToday);
    public DateTime? NextScheduledSendUtc { get; set; }
}
