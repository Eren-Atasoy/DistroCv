using DistroCv.Core.Entities;

namespace DistroCv.Core.Interfaces;

/// <summary>
/// Repository interface for JobPosting entity operations
/// </summary>
public interface IJobPostingRepository
{
    Task<JobPosting?> GetByIdAsync(Guid id);
    Task<JobPosting?> GetByExternalIdAsync(string externalId);
    Task<IEnumerable<JobPosting>> GetActiveJobsAsync(int skip = 0, int take = 50);
    Task<IEnumerable<JobPosting>> SearchBySectorAsync(string sector, int skip = 0, int take = 50);
    Task<IEnumerable<JobPosting>> SearchByLocationAsync(string location, int skip = 0, int take = 50);
    Task<JobPosting> CreateAsync(JobPosting jobPosting);
    Task<IEnumerable<JobPosting>> CreateBatchAsync(IEnumerable<JobPosting> jobPostings);
    Task<JobPosting> UpdateAsync(JobPosting jobPosting);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ExistsByExternalIdAsync(string externalId);
    Task<int> GetActiveCountAsync();
}
