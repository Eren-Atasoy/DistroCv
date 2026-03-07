namespace DistroCv.Core.Interfaces;

/// <summary>
/// Hangfire job contract for delivering a single email from the queue.
/// This is the entry point that Hangfire calls when a scheduled email job fires.
/// Layer: Application (Core)
/// </summary>
public interface IEmailDeliveryJob
{
    /// <summary>
    /// Executes a single email delivery from the queue.
    /// 1. Loads the EmailJob from DB
    /// 2. Validates business hours and daily limits
    /// 3. Sends via IGmailDeliveryService
    /// 4. Updates status to Sent or Failed
    /// </summary>
    /// <param name="emailJobId">The EmailJob ID to process</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ExecuteAsync(Guid emailJobId, CancellationToken cancellationToken);
}
