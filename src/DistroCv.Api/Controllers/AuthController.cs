using DistroCv.Core.DTOs;
using DistroCv.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DistroCv.Api.Controllers;

/// <summary>
/// Authentication — email/şifre + Google OAuth
/// </summary>
public class AuthController : BaseApiController
{
    private readonly ILogger<AuthController> _logger;
    private readonly IAuthService _authService;
    private readonly IUserService _userService;
    private readonly ISessionService _sessionService;

    public AuthController(
        ILogger<AuthController> logger,
        IAuthService authService,
        IUserService userService,
        ISessionService sessionService)
    {
        _logger = logger;
        _authService = authService;
        _userService = userService;
        _sessionService = sessionService;
    }

    /// <summary>Yeni kullanıcı kaydı (email + şifre)</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] SignUpRequestDto request)
    {
        try
        {
            var result = await _authService.RegisterAsync(new RegisterRequestDto(
                request.Email,
                request.FullName,
                request.Password,
                request.PreferredLanguage
            ));
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Register error");
            return StatusCode(500, new { message = "Kayıt sırasında bir hata oluştu." });
        }
    }

    /// <summary>Giriş (email + şifre)</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] SignInRequestDto request)
    {
        try
        {
            var result = await _authService.LoginAsync(new LoginRequestDto(
                request.Email,
                request.Password
            ));
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login error");
            return StatusCode(500, new { message = "Giriş sırasında bir hata oluştu." });
        }
    }

    /// <summary>Google OAuth ile giriş / kayıt</summary>
    [HttpPost("google")]
    [ProducesResponseType(typeof(AuthResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleOAuthRequestDto request)
    {
        try
        {
            var result = await _authService.GoogleLoginAsync(
                request.IdToken,
                request.PreferredLanguage
            );
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Google OAuth error");
            return StatusCode(500, new { message = "Google ile giriş sırasında bir hata oluştu." });
        }
    }

    /// <summary>Access token yenile</summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(TokenPairDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto request)
    {
        try
        {
            var tokens = await _authService.RefreshTokenAsync(request.RefreshToken);
            return Ok(tokens);
        }
        catch (InvalidOperationException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Refresh token error");
            return StatusCode(500, new { message = "Token yenilemede hata oluştu." });
        }
    }

    /// <summary>Şifre değiştir</summary>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(typeof(SuccessResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return Unauthorized(new { message = "Kimlik doğrulaması gerekli." });

            await _authService.ChangePasswordAsync(userId, request.OldPassword, request.NewPassword);
            return Ok(new SuccessResponseDto(true, "Şifre başarıyla değiştirildi."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Change password error");
            return StatusCode(500, new { message = "Şifre değiştirmede hata oluştu." });
        }
    }

    /// <summary>Şifre sıfırlama isteği</summary>
    [HttpPost("forgot-password")]
    [ProducesResponseType(typeof(SuccessResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto request)
    {
        try
        {
            await _authService.ForgotPasswordAsync(request.Email);
            return Ok(new SuccessResponseDto(true, "Şifre sıfırlama talimatları e-posta adresinize gönderildi."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Forgot password error");
            return StatusCode(500, new { message = "Bir hata oluştu." });
        }
    }

    /// <summary>Mevcut kullanıcıyı getir</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrentUser()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return Unauthorized(new { message = "Kimlik doğrulaması gerekli." });

            var user = await _userService.GetByIdAsync(userId);
            if (user == null)
                return NotFound(new { message = "Kullanıcı bulunamadı." });

            return Ok(_userService.ToDto(user));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get current user error");
            return StatusCode(500, new { message = "Bir hata oluştu." });
        }
    }

    /// <summary>Oturum listesi</summary>
    [HttpGet("sessions")]
    [Authorize]
    [ProducesResponseType(typeof(ActiveSessionsResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActiveSessions()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var sessions = await _sessionService.GetActiveSessionsAsync(userId);
            return Ok(new ActiveSessionsResponseDto(sessions, sessions.Count));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get sessions error");
            return StatusCode(500, new { message = "Bir hata oluştu." });
        }
    }

    /// <summary>Oturumu iptal et</summary>
    [HttpPost("sessions/revoke")]
    [Authorize]
    [ProducesResponseType(typeof(SuccessResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> RevokeSession([FromBody] RevokeSessionRequestDto request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var success = await _sessionService.RevokeSessionAsync(
                request.SessionId, userId, "Kullanıcı oturumu iptal etti");

            if (!success) return BadRequest(new { message = "Oturum iptal edilemedi." });
            return Ok(new SuccessResponseDto(true, "Oturum başarıyla iptal edildi."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Revoke session error");
            return StatusCode(500, new { message = "Bir hata oluştu." });
        }
    }

    /// <summary>Tüm cihazlardan çıkış</summary>
    [HttpPost("logout-all")]
    [Authorize]
    [ProducesResponseType(typeof(SuccessResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> LogoutAll()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty) return Unauthorized();

            await _sessionService.RevokeAllSessionsAsync(userId, "Tüm cihazlardan çıkış");
            return Ok(new SuccessResponseDto(true, "Tüm cihazlardan çıkış yapıldı."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Logout all error");
            return StatusCode(500, new { message = "Bir hata oluştu." });
        }
    }
}
