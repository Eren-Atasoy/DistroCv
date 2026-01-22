using DistroCv.Core.Entities;
using DistroCv.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DistroCv.Infrastructure.Data;

/// <summary>
/// Repository implementation for User entity operations
/// Task 2.12: Implements user CRUD operations (Validates: Requirement 1.1, 1.4)
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly DistroCvDbContext _context;

    public UserRepository(DistroCvDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Gets a user by their unique identifier
    /// </summary>
    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await _context.Users
            .Include(u => u.DigitalTwin)
            .Include(u => u.Applications)
            .Include(u => u.JobMatches)
            .Include(u => u.Sessions)
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    /// <summary>
    /// Gets a user by their email address
    /// </summary>
    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users
            .Include(u => u.DigitalTwin)
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    /// <summary>
    /// Gets a user by their AWS Cognito user ID
    /// </summary>
    public async Task<User?> GetByCognitoIdAsync(string cognitoUserId)
    {
        return await _context.Users
            .Include(u => u.DigitalTwin)
            .FirstOrDefaultAsync(u => u.CognitoUserId == cognitoUserId);
    }

    /// <summary>
    /// Creates a new user in the database
    /// Validates: Requirement 1.1 (User profile creation)
    /// </summary>
    public async Task<User> CreateAsync(User user)
    {
        user.CreatedAt = DateTime.UtcNow;
        user.IsActive = true;

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        
        return user;
    }

    /// <summary>
    /// Updates an existing user
    /// Validates: Requirement 1.4 (Profile updates with real-time logging)
    /// </summary>
    public async Task<User> UpdateAsync(User user)
    {
        var existingUser = await _context.Users.FindAsync(user.Id);
        if (existingUser == null)
        {
            throw new InvalidOperationException($"User with ID {user.Id} not found");
        }

        // Update fields
        existingUser.Email = user.Email;
        existingUser.FullName = user.FullName;
        existingUser.PreferredLanguage = user.PreferredLanguage;
        existingUser.LastLoginAt = user.LastLoginAt;
        existingUser.IsActive = user.IsActive;
        existingUser.Role = user.Role;
        existingUser.EncryptedApiKey = user.EncryptedApiKey;

        _context.Users.Update(existingUser);
        await _context.SaveChangesAsync();
        
        return existingUser;
    }

    /// <summary>
    /// Deletes a user from the database
    /// Note: This is a hard delete. For GDPR compliance, use soft delete or anonymization
    /// </summary>
    public async Task<bool> DeleteAsync(Guid id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return false;

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        
        return true;
    }

    /// <summary>
    /// Checks if a user exists by ID
    /// </summary>
    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.Users.AnyAsync(u => u.Id == id);
    }
}
