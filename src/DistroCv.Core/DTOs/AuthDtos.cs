namespace DistroCv.Core.DTOs;

/// <summary>
/// Request DTO for user sign up
/// </summary>
public record SignUpRequestDto(
    string Email,
    string Password,
    string FullName,
    string PreferredLanguage = "tr"
);

/// <summary>
/// Request DTO for confirming sign up with verification code
/// </summary>
public record ConfirmSignUpRequestDto(
    string Email,
    string ConfirmationCode
);

/// <summary>
/// Request DTO for user sign in
/// </summary>
public record SignInRequestDto(
    string Email,
    string Password
);

/// <summary>
/// Response DTO for successful authentication
/// </summary>
public record AuthResponseDto(
    string AccessToken,
    string RefreshToken,
    string IdToken,
    int ExpiresIn,
    UserDto User
);

/// <summary>
/// Request DTO for forgot password
/// </summary>
public record ForgotPasswordRequestDto(
    string Email
);

/// <summary>
/// Request DTO for confirming forgot password with new password
/// </summary>
public record ConfirmForgotPasswordRequestDto(
    string Email,
    string ConfirmationCode,
    string NewPassword
);

/// <summary>
/// Request DTO for changing password
/// </summary>
public record ChangePasswordRequestDto(
    string OldPassword,
    string NewPassword
);

/// <summary>
/// Response DTO for sign up
/// </summary>
public record SignUpResponseDto(
    string UserId,
    string Message
);

/// <summary>
/// Generic success response
/// </summary>
public record SuccessResponseDto(
    bool Success,
    string Message
);

/// <summary>
/// Request DTO for Google OAuth login
/// </summary>
public record GoogleOAuthRequestDto(
    string IdToken,
    string? PreferredLanguage = "tr"
);

/// <summary>
/// Response DTO for OAuth authorization URL
/// </summary>
public record OAuthUrlResponseDto(
    string AuthorizationUrl,
    string State
);

/// <summary>
/// Request DTO for refreshing access token
/// </summary>
public record RefreshTokenRequestDto(
    string RefreshToken
);

/// <summary>
/// Response DTO for token refresh
/// </summary>
public record RefreshTokenResponseDto(
    string AccessToken,
    string IdToken,
    int ExpiresIn,
    string TokenType = "Bearer"
);
