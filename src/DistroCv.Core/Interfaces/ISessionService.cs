using DistroCv.Core.DTOs;
using DistroCv.Core.Entities;

namespace DistroCv.Core.Interfaces;

/// <summary>
/// Service interface for session management
/// </summary>
public interface ISessionService
{
    /// <summary>
    /// Create a new session
    /// </summary>
    Task<UserSession> CreateSessionAsync(CreateSessionDto dto);

    /// <summary>
    /// Get session by access token
    /// </summary>
    Task<UserSession?> GetSessionByAccessTokenAsync(string accessToken);

    /// <summary>
    /// Get all active sessions for a user
    /// </summary>
    Task<List<SessionDto>> GetActiveSessionsAsync(Guid userId);

    /// <summary>
    /// Update session activity
    /// </summary>
    Task UpdateSessionActivityAsync(string accessToken);

    /// <summary>
    /// Revoke a specific session
    /// </summary>
    Task<bool> RevokeSessionAsync(Guid sessionId, Guid userId, string reason = "User logout");

    /// <summary>
    /// Revoke all sessions for a user (logout from all devices)
    /// </summary>
    Task RevokeAllSessionsAsync(Guid userId, string reason = "User logout from all devices");

    /// <summary>
    /// Validate session and check if it's active
    /// </summary>
    Task<bool> ValidateSessionAsync(string accessToken);

    /// <summary>
    /// Clean up expired sessions
    /// </summary>
    Task CleanupExpiredSessionsAsync();

    /// <summary>
    /// Convert UserSession entity to SessionDto
    /// </summary>
    SessionDto ToDto(UserSession session);
}
