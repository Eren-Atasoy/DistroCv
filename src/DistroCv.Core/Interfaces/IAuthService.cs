using DistroCv.Core.DTOs;

namespace DistroCv.Core.Interfaces;

public interface IAuthService
{
    /// <summary>Kendi auth: email + password ile kayıt</summary>
    Task<AuthResultDto> RegisterAsync(RegisterRequestDto request);

    /// <summary>Kendi auth: email + password ile giriş</summary>
    Task<AuthResultDto> LoginAsync(LoginRequestDto request);

    /// <summary>Google ID token ile giriş / kayıt</summary>
    Task<AuthResultDto> GoogleLoginAsync(string idToken, string? preferredLanguage = null);

    /// <summary>Access token yenile</summary>
    Task<TokenPairDto> RefreshTokenAsync(string refreshToken);

    /// <summary>Şifreyi değiştir</summary>
    Task ChangePasswordAsync(Guid userId, string oldPassword, string newPassword);

    /// <summary>Şifre sıfırlama e-postası gönder</summary>
    Task ForgotPasswordAsync(string email);
}
