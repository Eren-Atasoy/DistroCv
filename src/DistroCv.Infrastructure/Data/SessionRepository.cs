using DistroCv.Core.Entities;
using DistroCv.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DistroCv.Infrastructure.Data;

/// <summary>
/// Repository for managing user sessions in the database
/// </summary>
public class SessionRepository : ISessionRepository
{
    private readonly DistroCvDbContext _context;
    private readonly ILogger<SessionRepository> _logger;

    public SessionRepository(DistroCvDbContext context, ILogger<SessionRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<UserSession?> GetByIdAsync(Guid id)
    {
        return await _context.UserSessions
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<UserSession?> GetByAccessTokenAsync(string accessToken)
    {
        return await _context.UserSessions
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.AccessToken == accessToken && s.IsActive);
    }

    public async Task<UserSession?> GetByRefreshTokenAsync(string refreshToken)
    {
        return await _context.UserSessions
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.RefreshToken == refreshToken && s.IsActive);
    }

    public async Task<List<UserSession>> GetActiveSessionsByUserIdAsync(Guid userId)
    {
        return await _context.UserSessions
            .Where(s => s.UserId == userId && s.IsActive && s.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(s => s.LastActivityAt ?? s.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<UserSession>> GetAllSessionsByUserIdAsync(Guid userId)
    {
        return await _context.UserSessions
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task<UserSession> CreateAsync(UserSession session)
    {
        _context.UserSessions.Add(session);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Created session {SessionId} for user {UserId}", session.Id, session.UserId);
        
        return session;
    }

    public async Task UpdateActivityAsync(Guid sessionId)
    {
        var session = await _context.UserSessions.FindAsync(sessionId);
        if (session != null)
        {
            session.LastActivityAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task RevokeSessionAsync(Guid sessionId, string reason)
    {
        var session = await _context.UserSessions.FindAsync(sessionId);
        if (session != null)
        {
            session.IsActive = false;
            session.RevokedAt = DateTime.UtcNow;
            session.RevokedReason = reason;
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Revoked session {SessionId} for user {UserId}. Reason: {Reason}", 
                sessionId, session.UserId, reason);
        }
    }

    public async Task RevokeAllUserSessionsAsync(Guid userId, string reason)
    {
        var sessions = await _context.UserSessions
            .Where(s => s.UserId == userId && s.IsActive)
            .ToListAsync();

        foreach (var session in sessions)
        {
            session.IsActive = false;
            session.RevokedAt = DateTime.UtcNow;
            session.RevokedReason = reason;
        }

        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Revoked {Count} sessions for user {UserId}. Reason: {Reason}", 
            sessions.Count, userId, reason);
    }

    public async Task DeleteExpiredSessionsAsync()
    {
        var expiredSessions = await _context.UserSessions
            .Where(s => s.ExpiresAt < DateTime.UtcNow)
            .ToListAsync();

        _context.UserSessions.RemoveRange(expiredSessions);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Deleted {Count} expired sessions", expiredSessions.Count);
    }

    public async Task<int> CountActiveSessionsAsync(Guid userId)
    {
        return await _context.UserSessions
            .CountAsync(s => s.UserId == userId && s.IsActive && s.ExpiresAt > DateTime.UtcNow);
    }
}
