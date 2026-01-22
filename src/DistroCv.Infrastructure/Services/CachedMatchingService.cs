using DistroCv.Core.Entities;
using DistroCv.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace DistroCv.Infrastructure.Services;

/// <summary>
/// Cached wrapper for MatchingService (Task 29.1)
/// Implements caching strategy for match results
/// </summary>
public class CachedMatchingService : IMatchingService
{
    private readonly IMatchingService _innerService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<CachedMatchingService> _logger;

    // Cache expiration times
    private static readonly TimeSpan MatchCacheExpiration = TimeSpan.FromHours(24);
    private static readonly TimeSpan UserMatchesCacheExpiration = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan QueuedMatchesCacheExpiration = TimeSpan.FromMinutes(5);

    public CachedMatchingService(
        IMatchingService innerService,
        ICacheService cacheService,
        ILogger<CachedMatchingService> logger)
    {
        _innerService = innerService;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<JobMatch> CalculateMatchAsync(Guid userId, Guid jobPostingId, CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheKeys.GetMatchKey(userId, jobPostingId);

        // Try to get from cache first
        var cachedMatch = await _cacheService.GetAsync<JobMatch>(cacheKey, cancellationToken);
        if (cachedMatch != null)
        {
            _logger.LogDebug("Match result retrieved from cache for user {UserId} and job {JobId}", userId, jobPostingId);
            return cachedMatch;
        }

        // Calculate match and cache it
        var match = await _innerService.CalculateMatchAsync(userId, jobPostingId, cancellationToken);
        
        await _cacheService.SetAsync(cacheKey, match, MatchCacheExpiration, cancellationToken);
        _logger.LogDebug("Match result cached for user {UserId} and job {JobId}", userId, jobPostingId);

        // Invalidate user matches cache when new match is calculated
        await _cacheService.RemoveAsync(CacheKeys.GetUserMatchesKey(userId), cancellationToken);

        return match;
    }

    public async Task<List<JobMatch>> FindMatchesForUserAsync(Guid userId, decimal minScore = 80, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{CacheKeys.GetUserMatchesKey(userId)}:{minScore}";

        // Try to get from cache first
        var cachedMatches = await _cacheService.GetAsync<List<JobMatch>>(cacheKey, cancellationToken);
        if (cachedMatches != null)
        {
            _logger.LogDebug("User matches retrieved from cache for user {UserId}", userId);
            return cachedMatches;
        }

        // Find matches and cache them
        var matches = await _innerService.FindMatchesForUserAsync(userId, minScore, cancellationToken);
        
        if (matches.Any())
        {
            await _cacheService.SetAsync(cacheKey, matches, UserMatchesCacheExpiration, cancellationToken);
            _logger.LogDebug("User matches cached for user {UserId}, count: {Count}", userId, matches.Count);
        }

        return matches;
    }

    public async Task<List<JobMatch>> GetQueuedMatchesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"queued_matches:{userId}";

        // Try to get from cache first
        var cachedMatches = await _cacheService.GetAsync<List<JobMatch>>(cacheKey, cancellationToken);
        if (cachedMatches != null)
        {
            _logger.LogDebug("Queued matches retrieved from cache for user {UserId}", userId);
            return cachedMatches;
        }

        // Get queued matches and cache them
        var matches = await _innerService.GetQueuedMatchesAsync(userId, cancellationToken);
        
        if (matches.Any())
        {
            await _cacheService.SetAsync(cacheKey, matches, QueuedMatchesCacheExpiration, cancellationToken);
            _logger.LogDebug("Queued matches cached for user {UserId}, count: {Count}", userId, matches.Count);
        }

        return matches;
    }

    public async Task<JobMatch> ApproveMatchAsync(Guid matchId, Guid userId, CancellationToken cancellationToken = default)
    {
        // Approve match through inner service
        var match = await _innerService.ApproveMatchAsync(matchId, userId, cancellationToken);
        
        // Invalidate related caches
        await InvalidateMatchCachesAsync(userId, match.JobPostingId, cancellationToken);
        
        _logger.LogDebug("Match {MatchId} approved and cache invalidated for user {UserId}", matchId, userId);
        
        return match;
    }

    public async Task<JobMatch> RejectMatchAsync(Guid matchId, Guid userId, CancellationToken cancellationToken = default)
    {
        // Reject match through inner service
        var match = await _innerService.RejectMatchAsync(matchId, userId, cancellationToken);
        
        // Invalidate related caches
        await InvalidateMatchCachesAsync(userId, match.JobPostingId, cancellationToken);
        
        _logger.LogDebug("Match {MatchId} rejected and cache invalidated for user {UserId}", matchId, userId);
        
        return match;
    }

    /// <summary>
    /// Invalidates all match-related caches for a user
    /// </summary>
    private async Task InvalidateMatchCachesAsync(Guid userId, Guid jobPostingId, CancellationToken cancellationToken = default)
    {
        await _cacheService.RemoveAsync(CacheKeys.GetMatchKey(userId, jobPostingId), cancellationToken);
        await _cacheService.RemoveAsync(CacheKeys.GetUserMatchesKey(userId), cancellationToken);
        await _cacheService.RemoveAsync($"queued_matches:{userId}", cancellationToken);
    }

    /// <summary>
    /// Invalidates all cached matches for a user
    /// Call this when user's digital twin is updated
    /// </summary>
    public async Task InvalidateUserCacheAsync(Guid userId)
    {
        _logger.LogInformation("Invalidating cache for user {UserId}", userId);
        await _cacheService.RemoveByPatternAsync($"match:{userId}:*");
        await _cacheService.RemoveAsync(CacheKeys.GetUserMatchesKey(userId));
        await _cacheService.RemoveAsync(CacheKeys.GetDigitalTwinKey(userId));
        await _cacheService.RemoveAsync($"queued_matches:{userId}");
    }
}

