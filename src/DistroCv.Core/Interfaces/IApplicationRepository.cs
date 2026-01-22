using DistroCv.Core.Entities;

namespace DistroCv.Core.Interfaces;

/// <summary>
/// Repository interface for Application entity operations
/// </summary>
public interface IApplicationRepository
{
    Task<Application?> GetByIdAsync(Guid id);
    Task<IEnumerable<Application>> GetByUserIdAsync(Guid userId, int skip = 0, int take = 50);
    Task<IEnumerable<Application>> GetByStatusAsync(Guid userId, string status, int skip = 0, int take = 50);
    Task<IEnumerable<Application>> GetPendingApplicationsAsync(int skip = 0, int take = 50);
    Task<Application> CreateAsync(Application application);
    Task<Application> UpdateAsync(Application application);
    Task<bool> DeleteAsync(Guid id);
    Task<int> GetCountByUserAsync(Guid userId);
    Task<int> GetCountByStatusAsync(Guid userId, string status);
}
