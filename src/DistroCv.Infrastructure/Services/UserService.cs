using DistroCv.Core.DTOs;
using DistroCv.Core.Entities;
using DistroCv.Core.Interfaces;
using DistroCv.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DistroCv.Infrastructure.Services;

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
        => await _context.Users
            .Include(u => u.DigitalTwin)
            .FirstOrDefaultAsync(u => u.Id == id);

    public async Task<User?> GetByEmailAsync(string email)
        => await _context.Users
            .Include(u => u.DigitalTwin)
            .FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant());

    public async Task<User?> GetByGoogleIdAsync(string googleId)
        => await _context.Users
            .FirstOrDefaultAsync(u => u.GoogleId == googleId);

    public async Task<User> CreateAsync(CreateUserDto dto)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = dto.Email.ToLowerInvariant(),
            FullName = dto.FullName,
            PasswordHash = dto.PasswordHash,
            GoogleId = dto.GoogleId,
            AuthProvider = dto.AuthProvider,
            PreferredLanguage = dto.PreferredLanguage,
            EmailVerified = dto.EmailVerified,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created user {UserId} ({Email})", user.Id, user.Email);
        return user;
    }

    public async Task<User> UpdateAsync(Guid id, UpdateUserDto dto)
    {
        var user = await GetByIdAsync(id)
            ?? throw new InvalidOperationException($"User {id} not found");

        if (!string.IsNullOrEmpty(dto.FullName))
            user.FullName = dto.FullName;

        if (!string.IsNullOrEmpty(dto.PreferredLanguage))
            user.PreferredLanguage = dto.PreferredLanguage;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Updated user {UserId}", id);
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
        if (user == null) return false;

        user.IsActive = false;
        await _context.SaveChangesAsync();
        _logger.LogInformation("Soft deleted user {UserId}", id);
        return true;
    }

    public UserDto ToDto(User user) => new(
        user.Id,
        user.Email,
        user.FullName,
        user.PreferredLanguage,
        user.AuthProvider,
        user.CreatedAt,
        user.LastLoginAt,
        user.IsActive,
        user.EmailVerified
    );
}
