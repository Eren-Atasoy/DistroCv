namespace DistroCv.Core.DTOs;

/// <summary>
/// DTO for creating a new session
/// </summary>
public record CreateSessionDto(
    Guid UserId,
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    string DeviceInfo,
    string IpAddress,
    string UserAgent
);

/// <summary>
/// DTO for session information
/// </summary>
public record SessionDto(
    Guid Id,
    Guid UserId,
    string DeviceInfo,
    string IpAddress,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    DateTime? LastActivityAt,
    bool IsActive
);

/// <summary>
/// DTO for revoking a session
/// </summary>
public record RevokeSessionRequestDto(
    Guid SessionId
);

/// <summary>
/// Response DTO for active sessions list
/// </summary>
public record ActiveSessionsResponseDto(
    List<SessionDto> Sessions,
    int TotalCount
);
