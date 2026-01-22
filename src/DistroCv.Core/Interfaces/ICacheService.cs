namespace DistroCv.Core.Interfaces;

/// <summary>
/// Interface for caching service (Task 29.1)
/// Provides caching capabilities for match results and other frequently accessed data
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Gets a value from cache
    /// </summary>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;
    
    /// <summary>
    /// Sets a value in cache with optional expiration
    /// </summary>
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class;
    
    /// <summary>
    /// Removes a value from cache
    /// </summary>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Removes all values matching a pattern
    /// </summary>
    Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if a key exists in cache
    /// </summary>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets or sets a value using a factory function if not in cache
    /// </summary>
    Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class;
}

/// <summary>
/// Cache key constants for consistent key naming
/// </summary>
public static class CacheKeys
{
    public const string MatchResult = "match:{0}:{1}"; // userId:jobId
    public const string UserMatches = "user_matches:{0}"; // userId
    public const string JobPosting = "job:{0}"; // jobId
    public const string DigitalTwin = "twin:{0}"; // userId
    public const string UserProfile = "profile:{0}"; // userId
    public const string InterviewQuestions = "interview:{0}"; // applicationId
    
    public static string GetMatchKey(Guid userId, Guid jobId) => string.Format(MatchResult, userId, jobId);
    public static string GetUserMatchesKey(Guid userId) => string.Format(UserMatches, userId);
    public static string GetJobKey(Guid jobId) => string.Format(JobPosting, jobId);
    public static string GetDigitalTwinKey(Guid userId) => string.Format(DigitalTwin, userId);
    public static string GetUserProfileKey(Guid userId) => string.Format(UserProfile, userId);
    public static string GetInterviewQuestionsKey(Guid applicationId) => string.Format(InterviewQuestions, applicationId);
}

