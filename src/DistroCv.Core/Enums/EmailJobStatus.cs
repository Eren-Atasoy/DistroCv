namespace DistroCv.Core.Enums;

/// <summary>
/// Represents the status of an email job in the automation queue
/// </summary>
public enum EmailJobStatus
{
    /// <summary>Email is queued and waiting to be scheduled</summary>
    Pending = 0,

    /// <summary>Email has been scheduled via Hangfire and is waiting for its time slot</summary>
    Scheduled = 1,

    /// <summary>Email is currently being processed/sent</summary>
    Processing = 2,

    /// <summary>Email was sent successfully via Gmail API</summary>
    Sent = 3,

    /// <summary>Email sending failed after all retry attempts</summary>
    Failed = 4,

    /// <summary>Email was cancelled by the user or system before sending</summary>
    Cancelled = 5
}
