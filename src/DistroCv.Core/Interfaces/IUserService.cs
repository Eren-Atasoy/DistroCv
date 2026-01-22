using DistroCv.Core.DTOs;
using DistroCv.Core.Entities;

namespace DistroCv.Core.Interfaces;

/// <summary>
/// Service interface for user management
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Get user by ID
    /// </summary>
    Task<User?> GetByIdAsync(Guid id);

    /// <summary>
    /// Get user by email
    /// </summary>
    Task<User?> GetByEmailAsync(string email);

    /// <summary>
    /// Get user by Cognito user ID
    /// </summary>
    Task<User?> GetByCognitoUserIdAsync(string cognitoUserId);

    /// <summary>
    /// Create a new user
    /// </summary>
    Task<User> CreateAsync(CreateUserDto dto);

    /// <summary>
    /// Update user information
    /// </summary>
    Task<User> UpdateAsync(Guid id, UpdateUserDto dto);

    /// <summary>
    /// Update user's last login timestamp
    /// </summary>
    Task UpdateLastLoginAsync(Guid id);

    /// <summary>
    /// Delete user (soft delete)
    /// </summary>
    Task<bool> DeleteAsync(Guid id);

    /// <summary>
    /// Convert User entity to UserDto
    /// </summary>
    UserDto ToDto(User user);
}
