using DistroCv.Core.DTOs;
using DistroCv.Core.Entities;
using DistroCv.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace DistroCv.Infrastructure.Services;

/// <summary>
/// Service for managing user sessions
/// </summary>
public class SessionService : ISessionService
{
    private readonly ISessionRepository _sessionRepository;
    private readonly ILogger<SessionService> _logger;
    private const int MaxConcurrentSessions = 5; // Maximum concurrent sessions per user

    public SessionService(ISessionRepository sessionRepository, ILogger<SessionService> logger)
    {
        _sessionRepository = sessionRepository;
        _logger = logger;
    }

    public async Task<UserSession> CreateSessionAsync(CreateSessionDto dto)
    {
        // Check if user has too many active sessions
        var activeSessionCount = await _sessionRepository.CountActiveSessionsAsync(dto.UserId);
        
        if (activeSessionCount >= MaxConcurrentSessions)
        {
            _logger.LogWarning("User {UserId} has reached maximum concurrent sessions ({Max}). Revoking oldest session.", 
                dto.UserId, MaxConcurrentSessions);
            
            // Get all active sessions and revoke the oldest one
            var activeSessions = await _sessionRepository.GetActiveSessionsByUserIdAsync(dto.UserId);
            var oldestSession = activeSessions.OrderBy(s => s.LastActivityAt ?? s.CreatedAt).FirstOrDefault();
            
            if (oldestSession != null)
            {
                await _sessionRepository.RevokeSessionAsync(oldestSession.Id, "Maximum concurrent sessions reached");
            }
        }

        var session = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = dto.UserId,
            AccessToken = dto.AccessToken,
            RefreshToken = dto.RefreshToken,
            DeviceInfo = dto.DeviceInfo,
            IpAddress = dto.IpAddress,
            UserAgent = dto.UserAgent,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddSeconds(dto.ExpiresIn),
            IsActive = true
        };

        return await _sessionRepository.CreateAsync(session);
    }

    public async Task<UserSession?> GetSessionByAccessTokenAsync(string accessToken)
    {
        return await _sessionRepository.GetByAccessTokenAsync(accessToken);
    }

    public async Task<List<SessionDto>> GetActiveSessionsAsync(Guid userId)
    {
        var sessions = await _sessionRepository.GetActiveSessionsByUserIdAsync(userId);
        return sessions.Select(ToDto).ToList();
    }

    public async Task UpdateSessionActivityAsync(string accessToken)
    {
        var session = await _sessionRepository.GetByAccessTokenAsync(accessToken);
        if (session != null)
        {
            await _sessionRepository.UpdateActivityAsync(session.Id);
        }
    }

    public async Task<bool> RevokeSessionAsync(Guid sessionId, Guid userId, string reason = "User logout")
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId);
        
        if (session == null)
        {
            _logger.LogWarning("Session {SessionId} not found", sessionId);
            return false;
        }

        if (session.UserId != userId)
        {
            _logger.LogWarning("User {UserId} attempted to revoke session {SessionId} belonging to user {OwnerId}", 
                userId, sessionId, session.UserId);
            return false;
        }

        await _sessionRepository.RevokeSessionAsync(sessionId, reason);
        return true;
    }

    public async Task RevokeAllSessionsAsync(Guid userId, string reason = "User logout from all devices")
    {
        await _sessionRepository.RevokeAllUserSessionsAsync(userId, reason);
    }

    public async Task<bool> ValidateSessionAsync(string accessToken)
    {
        var session = await _sessionRepository.GetByAccessTokenAsync(accessToken);
        
        if (session == null)
        {
            return false;
        }

        // Check if session is active and not expired
        if (!session.IsActive || session.ExpiresAt <= DateTime.UtcNow)
        {
            return false;
        }

        // Update last activity
        await UpdateSessionActivityAsync(accessToken);
        
        return true;
    }

    public async Task CleanupExpiredSessionsAsync()
    {
        _logger.LogInformation("Starting cleanup of expired sessions");
        await _sessionRepository.DeleteExpiredSessionsAsync();
    }

    public SessionDto ToDto(UserSession session)
    {
        return new SessionDto(
            session.Id,
            session.UserId,
            session.DeviceInfo,
            session.IpAddress,
            session.CreatedAt,
            session.ExpiresAt,
            session.LastActivityAt,
            session.IsActive
        );
    }
}
