using DistroCv.Core.Entities;

namespace DistroCv.Core.Interfaces;

/// <summary>
/// Repository for job match operations
/// </summary>
public interface IJobMatchRepository
{
    /// <summary>
    /// Gets a job match by ID
    /// </summary>
    Task<JobMatch?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all matches for a user
    /// </summary>
    Task<List<JobMatch>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets matches for a user with minimum score
    /// </summary>
    Task<List<JobMatch>> GetByUserIdWithMinScoreAsync(Guid userId, decimal minScore, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets queued matches for a user
    /// </summary>
    Task<List<JobMatch>> GetQueuedMatchesAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a match already exists
    /// </summary>
    Task<bool> ExistsAsync(Guid userId, Guid jobPostingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new job match
    /// </summary>
    Task<JobMatch> CreateAsync(JobMatch jobMatch, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing job match
    /// </summary>
    Task<JobMatch> UpdateAsync(JobMatch jobMatch, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a job match
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
