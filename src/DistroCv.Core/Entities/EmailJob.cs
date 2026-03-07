using DistroCv.Core.Enums;

namespace DistroCv.Core.Entities;

/// <summary>
/// Represents an email job in the smart email automation queue.
/// Each record tracks a single outbound email from a user's own Gmail account
/// to an HR department, including generation metadata, scheduling info, and delivery status.
/// </summary>
public class EmailJob
{
    public Guid Id { get; set; }

    /// <summary>The user who owns this email job (sender)</summary>
    public Guid UserId { get; set; }

    /// <summary>The related application record, if any</summary>
    public Guid? ApplicationId { get; set; }

    /// <summary>The job posting this email targets</summary>
    public Guid JobPostingId { get; set; }

    // ── Recipient ──────────────────────────────────────────────

    /// <summary>HR contact email address (from VerifiedCompany or job posting)</summary>
    public string RecipientEmail { get; set; } = string.Empty;

    /// <summary>HR contact name or "Hiring Manager"</summary>
    public string RecipientName { get; set; } = string.Empty;

    // ── Email Content ──────────────────────────────────────────

    /// <summary>Generated email subject line</summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>Generated plain-text email body (no HTML, no spam words)</summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>AWS S3 presigned URL for the CV attachment link</summary>
    public string? CvPresignedUrl { get; set; }

    // ── Status & Scheduling ────────────────────────────────────

    /// <summary>Current status of this email job</summary>
    public EmailJobStatus Status { get; set; } = EmailJobStatus.Pending;

    /// <summary>The UTC time this email is scheduled to be sent</summary>
    public DateTime? ScheduledAtUtc { get; set; }

    /// <summary>The UTC time this email was actually sent</summary>
    public DateTime? SentAtUtc { get; set; }

    /// <summary>Hangfire job ID for tracking/cancellation</summary>
    public string? HangfireJobId { get; set; }

    // ── Retry & Error ──────────────────────────────────────────

    /// <summary>Number of delivery attempts so far</summary>
    public int RetryCount { get; set; } = 0;

    /// <summary>Maximum retry attempts allowed</summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>Last error message if delivery failed</summary>
    public string? LastError { get; set; }

    /// <summary>Gmail API message ID returned on successful send</summary>
    public string? GmailMessageId { get; set; }

    // ── Timestamps ─────────────────────────────────────────────

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    // ── Navigation ─────────────────────────────────────────────

    public User User { get; set; } = null!;
    public Application? Application { get; set; }
    public JobPosting JobPosting { get; set; } = null!;
}
