using System.ComponentModel.DataAnnotations;

namespace DistroCv.Core.DTOs;

/// <summary>
/// Request DTO for user sign up
/// </summary>
public record SignUpRequestDto(
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [StringLength(255, ErrorMessage = "Email cannot exceed 255 characters")]
    string Email,
    
    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 100 characters")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$", 
        ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one number and one special character")]
    string Password,
    
    [Required(ErrorMessage = "Full name is required")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 200 characters")]
    string FullName,
    
    [Required(ErrorMessage = "Preferred language is required")]
    [RegularExpression(@"^(tr|en)$", ErrorMessage = "Preferred language must be 'tr' or 'en'")]
    string PreferredLanguage = "tr"
);

/// <summary>
/// Request DTO for confirming sign up with verification code
/// </summary>
public record ConfirmSignUpRequestDto(
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    string Email,
    
    [Required(ErrorMessage = "Confirmation code is required")]
    [StringLength(10, MinimumLength = 6, ErrorMessage = "Confirmation code must be between 6 and 10 characters")]
    string ConfirmationCode
);

/// <summary>
/// Request DTO for user sign in
/// </summary>
public record SignInRequestDto(
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    string Email,
    
    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 100 characters")]
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
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    string Email
);

/// <summary>
/// Request DTO for confirming forgot password with new password
/// </summary>
public record ConfirmForgotPasswordRequestDto(
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    string Email,
    
    [Required(ErrorMessage = "Confirmation code is required")]
    [StringLength(10, MinimumLength = 6, ErrorMessage = "Confirmation code must be between 6 and 10 characters")]
    string ConfirmationCode,
    
    [Required(ErrorMessage = "New password is required")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 100 characters")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$", 
        ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one number and one special character")]
    string NewPassword
);

/// <summary>
/// Request DTO for changing password
/// </summary>
public record ChangePasswordRequestDto(
    [Required(ErrorMessage = "Old password is required")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 100 characters")]
    string OldPassword,
    
    [Required(ErrorMessage = "New password is required")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 100 characters")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d])(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$", 
        ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one number and one special character")]
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
    [Required(ErrorMessage = "ID token is required")]
    [StringLength(2048, ErrorMessage = "ID token cannot exceed 2048 characters")]
    string IdToken,
    
    [RegularExpression(@"^(tr|en)$", ErrorMessage = "Preferred language must be 'tr' or 'en'")]
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
    [Required(ErrorMessage = "Refresh token is required")]
    [StringLength(2048, ErrorMessage = "Refresh token cannot exceed 2048 characters")]
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
