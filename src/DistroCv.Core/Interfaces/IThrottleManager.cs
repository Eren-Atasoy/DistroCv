namespace DistroCv.Core.Interfaces;

/// <summary>
/// Service for managing rate limiting and throttling for LinkedIn operations
/// </summary>
public interface IThrottleManager
{
    /// <summary>
    /// Checks if user can send a LinkedIn connection request (Validates: Requirement 6.1)
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if within daily limit (20 connections)</returns>
    Task<bool> CanSendConnectionRequestAsync(Guid userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if user can send a LinkedIn message (Validates: Requirement 6.2)
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if within daily limit (50-80 messages)</returns>
    Task<bool> CanSendMessageAsync(Guid userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Records a LinkedIn connection request
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RecordConnectionRequestAsync(Guid userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Records a LinkedIn message sent
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RecordMessageSentAsync(Guid userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets random delay between operations (2-8 minutes) (Validates: Requirement 6.3)
    /// </summary>
    /// <returns>Delay in milliseconds</returns>
    TimeSpan GetRandomDelay();
    
    /// <summary>
    /// Gets daily quota status for user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Quota status</returns>
    Task<ThrottleQuotaStatus> GetQuotaStatusAsync(Guid userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if operation should be queued due to quota exceeded (Validates: Requirement 6.4)
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="operationType">Type of operation (Connection, Message)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if should be queued</returns>
    Task<bool> ShouldQueueOperationAsync(
        Guid userId, 
        string operationType, 
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Throttle quota status for a user
/// </summary>
public class ThrottleQuotaStatus
{
    public int ConnectionRequestsToday { get; set; }
    public int MaxConnectionRequests { get; set; } = 20;
    public int MessagesSentToday { get; set; }
    public int MaxMessages { get; set; } = 80;
    public bool CanSendConnection => ConnectionRequestsToday < MaxConnectionRequests;
    public bool CanSendMessage => MessagesSentToday < MaxMessages;
    public DateTime QuotaResetTime { get; set; }
}
