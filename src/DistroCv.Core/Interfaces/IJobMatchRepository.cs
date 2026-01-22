using DistroCv.Core.Entities;

namespace DistroCv.Core.Interfaces;

/// <summary>
/// Repository interface for JobMatch entity operations
/// </summary>
public interface IJobMatchRepository
{
    Task<JobMatch?> GetByIdAsync(Guid id);
    Task<IEnumerable<JobMatch>> GetByUserIdAsync(Guid userId, int skip = 0, int take = 50);
    Task<IEnumerable<JobMatch>> GetQueuedMatchesAsync(Guid userId, int skip = 0, int take = 20);
    Task<IEnumerable<JobMatch>> GetHighScoreMatchesAsync(Guid userId, decimal minScore = 80, int skip = 0, int take = 50);
    Task<JobMatch> CreateAsync(JobMatch jobMatch);
    Task<JobMatch> UpdateAsync(JobMatch jobMatch);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid userId, Guid jobPostingId);
}
