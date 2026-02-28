using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using DistroCv.Core.DTOs;
using DistroCv.Core.Entities;
using DistroCv.Core.Interfaces;
using DistroCv.Infrastructure.Data;
using Google.Apis.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace DistroCv.Infrastructure.Services;

/// <summary>
/// Kendi JWT tabanlı auth servisi — AWS Cognito bağımlılığı yok
/// Email/şifre kaydı + Google OAuth destekler
/// </summary>
public class AuthService : IAuthService
{
    private readonly DistroCvDbContext _context;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthService> _logger;

    private string JwtSecret => _config["Jwt:Secret"]
        ?? throw new InvalidOperationException("Jwt:Secret is not configured");
    private string JwtIssuer => _config["Jwt:Issuer"] ?? "distrocv-api";
    private string JwtAudience => _config["Jwt:Audience"] ?? "distrocv-client";
    private int AccessTokenMinutes => int.TryParse(_config["Jwt:AccessTokenMinutes"], out var m) ? m : 60;
    private int RefreshTokenDays => int.TryParse(_config["Jwt:RefreshTokenDays"], out var d) ? d : 30;
    private string? GoogleClientId => _config["Google:ClientId"];

    public AuthService(
        DistroCvDbContext context,
        IConfiguration config,
        ILogger<AuthService> logger)
    {
        _context = context;
        _config = config;
        _logger = logger;
    }

    // =========================================================
    // KAYIT (email + şifre)
    // =========================================================
    public async Task<AuthResultDto> RegisterAsync(RegisterRequestDto request)
    {
        _logger.LogInformation("Register attempt: {Email}", request.Email);

        var existing = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (existing != null)
            throw new InvalidOperationException("Bu e-posta ile zaten bir hesap var.");

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 12);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email.ToLowerInvariant(),
            FullName = request.FullName,
            PasswordHash = passwordHash,
            AuthProvider = "local",
            PreferredLanguage = request.PreferredLanguage,
            EmailVerified = false,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation("User registered: {UserId}", user.Id);

        var tokens = GenerateTokenPair(user);
        return new AuthResultDto(tokens.AccessToken, tokens.RefreshToken, tokens.ExpiresIn, ToDto(user));
    }

    // =========================================================
    // GİRİŞ (email + şifre)
    // =========================================================
    public async Task<AuthResultDto> LoginAsync(LoginRequestDto request)
    {
        _logger.LogInformation("Login attempt: {Email}", request.Email);

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email.ToLowerInvariant());

        if (user == null || !user.IsActive)
            throw new InvalidOperationException("E-posta veya şifre hatalı.");

        if (user.AuthProvider == "google" && user.PasswordHash == null)
            throw new InvalidOperationException("Bu hesap Google ile oluşturulmuş. Google ile giriş yapın.");

        if (string.IsNullOrEmpty(user.PasswordHash) ||
            !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new InvalidOperationException("E-posta veya şifre hatalı.");

        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Login successful: {UserId}", user.Id);

        var tokens = GenerateTokenPair(user);
        return new AuthResultDto(tokens.AccessToken, tokens.RefreshToken, tokens.ExpiresIn, ToDto(user));
    }

    // =========================================================
    // GOOGLE OAUTH
    // =========================================================
    public async Task<AuthResultDto> GoogleLoginAsync(string idToken, string? preferredLanguage = null)
    {
        _logger.LogInformation("Google OAuth login attempt");

        // Google ID token'ı doğrula
        GoogleJsonWebSignature.Payload payload;
        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings();
            if (!string.IsNullOrEmpty(GoogleClientId))
                settings.Audience = new[] { GoogleClientId };

            payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
        }
        catch (InvalidJwtException ex)
        {
            _logger.LogWarning("Google token validation failed: {Message}", ex.Message);
            throw new InvalidOperationException("Geçersiz Google token.", ex);
        }

        var email = payload.Email?.ToLowerInvariant()
            ?? throw new InvalidOperationException("Google token'da e-posta bulunamadı.");
        var name = payload.Name ?? payload.GivenName ?? email;
        var googleId = payload.Subject;

        // Kullanıcı var mı?
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email || u.GoogleId == googleId);

        if (user == null)
        {
            // Yeni kullanıcı oluştur
            user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                FullName = name,
                GoogleId = googleId,
                AuthProvider = "google",
                PreferredLanguage = preferredLanguage ?? "tr",
                EmailVerified = payload.EmailVerified,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };
            _context.Users.Add(user);
            _logger.LogInformation("New user created via Google: {Email}", email);
        }
        else
        {
            // Mevcut kullanıcıyı Google ile ilişkilendir (daha önce email/şifre ile kayıt olmuş olabilir)
            if (string.IsNullOrEmpty(user.GoogleId))
            {
                user.GoogleId = googleId;
                _logger.LogInformation("Linked Google to existing user: {Email}", email);
            }
            user.EmailVerified = payload.EmailVerified;
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var tokens = GenerateTokenPair(user);
        return new AuthResultDto(tokens.AccessToken, tokens.RefreshToken, tokens.ExpiresIn, ToDto(user));
    }

    // =========================================================
    // TOKEN YENİLE
    // =========================================================
    public async Task<TokenPairDto> RefreshTokenAsync(string refreshToken)
    {
        var principal = ValidateRefreshToken(refreshToken);
        if (principal == null)
            throw new InvalidOperationException("Geçersiz veya süresi dolmuş refresh token.");

        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
            throw new InvalidOperationException("Token geçersiz.");

        var user = await _context.Users.FindAsync(userId);
        if (user == null || !user.IsActive)
            throw new InvalidOperationException("Kullanıcı bulunamadı veya hesap devre dışı.");

        return GenerateTokenPair(user);
    }

    // =========================================================
    // ŞİFRE DEĞİŞTİR
    // =========================================================
    public async Task ChangePasswordAsync(Guid userId, string oldPassword, string newPassword)
    {
        var user = await _context.Users.FindAsync(userId)
            ?? throw new InvalidOperationException("Kullanıcı bulunamadı.");

        if (user.AuthProvider == "google" && string.IsNullOrEmpty(user.PasswordHash))
            throw new InvalidOperationException("Google hesaplarında şifre değiştirilemez.");

        if (string.IsNullOrEmpty(user.PasswordHash) ||
            !BCrypt.Net.BCrypt.Verify(oldPassword, user.PasswordHash))
            throw new InvalidOperationException("Mevcut şifre hatalı.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword, workFactor: 12);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Password changed for user {UserId}", userId);
    }

    // =========================================================
    // ŞİFRE SIFIRLAMA (sadece log — email servisi ayrıca)
    // =========================================================
    public async Task ForgotPasswordAsync(string email)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant());

        if (user == null)
        {
            // Güvenlik: kullanıcı yoksa da başarılı gibi göster
            _logger.LogInformation("Forgot password: email not found (silently ignored): {Email}", email);
            return;
        }

        if (user.AuthProvider == "google")
        {
            _logger.LogInformation("Forgot password skipped: Google OAuth user: {Email}", email);
            return;
        }

        // TODO: Email servisi ile token gönder
        // Şimdilik log basıyoruz — Gmail servisi entegre edilebilir
        var resetToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        _logger.LogInformation("Password reset requested for {Email}. Token (not sent): {Token}", email, resetToken);
    }

    // =========================================================
    // YARDIMCI: JWT üret
    // =========================================================
    private TokenPairDto GenerateTokenPair(User user)
    {
        var accessToken = GenerateAccessToken(user);
        var refreshToken = GenerateRefreshToken(user);
        var expiresIn = AccessTokenMinutes * 60;
        return new TokenPairDto(accessToken, refreshToken, expiresIn);
    }

    private string GenerateAccessToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("authProvider", user.AuthProvider),
        };

        var token = new JwtSecurityToken(
            issuer: JwtIssuer,
            audience: JwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(AccessTokenMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string GenerateRefreshToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecret + "_refresh"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim("type", "refresh")
        };

        var token = new JwtSecurityToken(
            issuer: JwtIssuer,
            audience: JwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(RefreshTokenDays),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private ClaimsPrincipal? ValidateRefreshToken(string token)
    {
        try
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecret + "_refresh"));
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = JwtIssuer,
                ValidateAudience = true,
                ValidAudience = JwtAudience,
                ValidateLifetime = true,
                IssuerSigningKey = key,
                ClockSkew = TimeSpan.Zero
            }, out _);
            return principal;
        }
        catch
        {
            return null;
        }
    }

    private static UserDto ToDto(User user) => new(
        user.Id,
        user.Email,
        user.FullName,
        user.PreferredLanguage,
        user.AuthProvider,
        user.CreatedAt,
        user.LastLoginAt,
        user.IsActive,
        user.EmailVerified
    );
}
