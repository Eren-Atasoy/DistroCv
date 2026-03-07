namespace DistroCv.Core.Entities;

/// <summary>
/// Represents a candidate user in the system
/// </summary>
public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;

    // Kendi auth sistemi için — null ise sadece Google ile giriş yapıyor
    public string? PasswordHash { get; set; }

    // Google OAuth — Google'ın user sub değeri
    public string? GoogleId { get; set; }

    // Auth provider: "local" | "google"
    public string AuthProvider { get; set; } = "local";

    public string PreferredLanguage { get; set; } = "tr"; // "tr" or "en"
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; } = true;
    public bool EmailVerified { get; set; } = false;
    public string Role { get; set; } = "User";
    public string? EncryptedApiKey { get; set; }

    // Gmail OAuth2 — kullanıcının kendi hesabından mail göndermek için
    /// <summary>Encrypted Gmail OAuth2 Refresh Token</summary>
    public string? GmailRefreshToken { get; set; }

    /// <summary>Gmail OAuth2 scopes granted by user (JSON array)</summary>
    public string? GmailScopes { get; set; }

    /// <summary>When the Gmail OAuth2 token was last refreshed</summary>
    public DateTime? GmailTokenGrantedAtUtc { get; set; }

    // Navigation
    public DigitalTwin? DigitalTwin { get; set; }
    public ICollection<Application> Applications { get; set; } = new List<Application>();
    public ICollection<JobMatch> JobMatches { get; set; } = new List<JobMatch>();
    public ICollection<UserFeedback> Feedbacks { get; set; } = new List<UserFeedback>();
    public ICollection<ThrottleLog> ThrottleLogs { get; set; } = new List<ThrottleLog>();
    public ICollection<UserSession> Sessions { get; set; } = new List<UserSession>();
    public ICollection<EmailJob> EmailJobs { get; set; } = new List<EmailJob>();
}
