using DistroCv.Core.DTOs;
using DistroCv.Core.Entities;
using DistroCv.Core.Interfaces;
using DistroCv.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DistroCv.Infrastructure.Services;

/// <summary>
/// Service for managing users in the database
/// </summary>
public class UserService : IUserService
{
    private readonly DistroCvDbContext _context;
    private readonly ILogger<UserService> _logger;

    public UserService(DistroCvDbContext context, ILogger<UserService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await _context.Users
            .Include(u => u.DigitalTwin)
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users
            .Include(u => u.DigitalTwin)
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<User?> GetByCognitoUserIdAsync(string cognitoUserId)
    {
        return await _context.Users
            .Include(u => u.DigitalTwin)
            .FirstOrDefaultAsync(u => u.CognitoUserId == cognitoUserId);
    }

    public async Task<User> CreateAsync(CreateUserDto dto)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = dto.Email,
            FullName = dto.FullName,
            CognitoUserId = dto.CognitoUserId ?? string.Empty,
            PreferredLanguage = dto.PreferredLanguage,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created user {UserId} with email {Email}", user.Id, user.Email);

        return user;
    }

    public async Task<User> UpdateAsync(Guid id, UpdateUserDto dto)
    {
        var user = await GetByIdAsync(id);
        if (user == null)
        {
            throw new InvalidOperationException($"User with ID {id} not found");
        }

        if (!string.IsNullOrEmpty(dto.FullName))
        {
            user.FullName = dto.FullName;
        }

        if (!string.IsNullOrEmpty(dto.PreferredLanguage))
        {
            user.PreferredLanguage = dto.PreferredLanguage;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated user {UserId}", user.Id);

        return user;
    }

    public async Task UpdateLastLoginAsync(Guid id)
    {
        var user = await GetByIdAsync(id);
        if (user != null)
        {
            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var user = await GetByIdAsync(id);
        if (user == null)
        {
            return false;
        }

        user.IsActive = false;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Soft deleted user {UserId}", user.Id);

        return true;
    }

    public UserDto ToDto(User user)
    {
        return new UserDto(
            user.Id,
            user.Email,
            user.FullName,
            user.PreferredLanguage,
            user.CreatedAt,
            user.LastLoginAt,
            user.IsActive
        );
    }
}
