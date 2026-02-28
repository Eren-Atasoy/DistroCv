using DistroCv.Core.DTOs;
using DistroCv.Core.Entities;

namespace DistroCv.Core.Interfaces;

public interface IUserService
{
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByGoogleIdAsync(string googleId);
    Task<User> CreateAsync(CreateUserDto dto);
    Task<User> UpdateAsync(Guid id, UpdateUserDto dto);
    Task UpdateLastLoginAsync(Guid id);
    Task<bool> DeleteAsync(Guid id);
    UserDto ToDto(User user);
}
