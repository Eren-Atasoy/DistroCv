using DistroCv.Core.Entities;

namespace DistroCv.Core.Interfaces;

/// <summary>
/// Service for matching candidates with job postings using AI
/// </summary>
public interface IMatchingService
{
    /// <summary>
    /// Calculates match score between a user's digital twin and a job posting
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="jobPostingId">Job posting ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Job match with score and reasoning</returns>
    Task<JobMatch> CalculateMatchAsync(Guid userId, Guid jobPostingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds and calculates matches for all active job postings for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="minScore">Minimum match score threshold (default: 80)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of job matches above threshold</returns>
    Task<List<JobMatch>> FindMatchesForUserAsync(Guid userId, decimal minScore = 80, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets matches for a user that are in the application queue
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of matches in queue</returns>
    Task<List<JobMatch>> GetQueuedMatchesAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Approves a match and adds it to application queue
    /// </summary>
    /// <param name="matchId">Match ID</param>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated job match</returns>
    Task<JobMatch> ApproveMatchAsync(Guid matchId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rejects a match
    /// </summary>
    /// <param name="matchId">Match ID</param>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated job match</returns>
    Task<JobMatch> RejectMatchAsync(Guid matchId, Guid userId, CancellationToken cancellationToken = default);
}
