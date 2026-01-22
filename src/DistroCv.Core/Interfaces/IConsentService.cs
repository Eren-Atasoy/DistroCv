using DistroCv.Core.Entities;

namespace DistroCv.Core.Interfaces;

public interface IConsentService
{
    Task GiveConsentAsync(Guid userId, string consentType, string? ipAddress = null, string? userAgent = null);
    Task RevokeConsentAsync(Guid userId, string consentType, string? ipAddress = null, string? userAgent = null);
    Task<IEnumerable<UserConsent>> GetUserConsentsAsync(Guid userId);
    Task<bool> HasConsentAsync(Guid userId, string consentType);
}
