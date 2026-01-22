namespace DistroCv.Core.Interfaces;

/// <summary>
/// Service interface for throttle management and anti-bot protection
/// </summary>
public interface IThrottleManager
{
    /// <summary>
    /// Checks if user can perform the specified action
    /// </summary>
    Task<bool> CanPerformActionAsync(Guid userId, string actionType);
    
    /// <summary>
    /// Records an action for throttling purposes
    /// </summary>
    Task RecordActionAsync(Guid userId, string actionType, string platform);
    
    /// <summary>
    /// Gets remaining quota for an action type
    /// </summary>
    Task<int> GetRemainingQuotaAsync(Guid userId, string actionType);
    
    /// <summary>
    /// Gets a random delay between min and max minutes
    /// </summary>
    Task<TimeSpan> GetRandomDelayAsync(int minMinutes, int maxMinutes);
    
    /// <summary>
    /// Gets daily stats for user actions
    /// </summary>
    Task<Dictionary<string, int>> GetDailyStatsAsync(Guid userId);
}
