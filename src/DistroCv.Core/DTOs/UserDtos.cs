namespace DistroCv.Core.DTOs;

public record UserDto(
    Guid Id,
    string Email,
    string FullName,
    string PreferredLanguage,
    DateTime CreatedAt,
    DateTime? LastLoginAt,
    bool IsActive
);

public record CreateUserDto(
    string Email,
    string FullName,
    string? CognitoUserId,
    string PreferredLanguage = "tr"
);

public record UpdateUserDto(
    string? FullName,
    string? PreferredLanguage
);
