using DistroCv.Core.Entities;

namespace DistroCv.Core.Interfaces;

/// <summary>
/// Repository interface for User entity operations
/// </summary>
public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByGoogleIdAsync(string googleId);
    Task<User> CreateAsync(User user);
    Task<User> UpdateAsync(User user);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
}
