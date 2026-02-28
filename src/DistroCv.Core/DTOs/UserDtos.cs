namespace DistroCv.Core.DTOs;

public record UserDto(
    Guid Id,
    string Email,
    string FullName,
    string PreferredLanguage,
    string AuthProvider,
    DateTime CreatedAt,
    DateTime? LastLoginAt,
    bool IsActive,
    bool EmailVerified
);

public record CreateUserDto(
    string Email,
    string FullName,
    string PreferredLanguage = "tr",
    string? PasswordHash = null,
    string? GoogleId = null,
    string AuthProvider = "local",
    bool EmailVerified = false
);

public record UpdateUserDto(
    string? FullName,
    string? PreferredLanguage
);
