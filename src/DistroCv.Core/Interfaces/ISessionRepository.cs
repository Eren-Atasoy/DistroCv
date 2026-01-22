using DistroCv.Core.Entities;

namespace DistroCv.Core.Interfaces;

/// <summary>
/// Repository interface for user session management
/// </summary>
public interface ISessionRepository
{
    /// <summary>
    /// Get session by ID
    /// </summary>
    Task<UserSession?> GetByIdAsync(Guid id);

    /// <summary>
    /// Get session by access token
    /// </summary>
    Task<UserSession?> GetByAccessTokenAsync(string accessToken);

    /// <summary>
    /// Get session by refresh token
    /// </summary>
    Task<UserSession?> GetByRefreshTokenAsync(string refreshToken);

    /// <summary>
    /// Get all active sessions for a user
    /// </summary>
    Task<List<UserSession>> GetActiveSessionsByUserIdAsync(Guid userId);

    /// <summary>
    /// Get all sessions for a user (including inactive)
    /// </summary>
    Task<List<UserSession>> GetAllSessionsByUserIdAsync(Guid userId);

    /// <summary>
    /// Create a new session
    /// </summary>
    Task<UserSession> CreateAsync(UserSession session);

    /// <summary>
    /// Update session activity timestamp
    /// </summary>
    Task UpdateActivityAsync(Guid sessionId);

    /// <summary>
    /// Revoke a specific session
    /// </summary>
    Task RevokeSessionAsync(Guid sessionId, string reason);

    /// <summary>
    /// Revoke all sessions for a user
    /// </summary>
    Task RevokeAllUserSessionsAsync(Guid userId, string reason);

    /// <summary>
    /// Delete expired sessions
    /// </summary>
    Task DeleteExpiredSessionsAsync();

    /// <summary>
    /// Count active sessions for a user
    /// </summary>
    Task<int> CountActiveSessionsAsync(Guid userId);
}
