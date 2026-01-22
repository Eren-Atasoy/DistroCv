using DistroCv.Core.Entities;
using DistroCv.Core.Interfaces;
using DistroCv.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DistroCv.Infrastructure.Services;

public class ConsentService : IConsentService
{
    private readonly DistroCvDbContext _context;
    private readonly IAuditLogService _auditLogService;

    public ConsentService(DistroCvDbContext context, IAuditLogService auditLogService)
    {
        _context = context;
        _auditLogService = auditLogService;
    }

    public async Task GiveConsentAsync(Guid userId, string consentType, string? ipAddress = null, string? userAgent = null)
    {
        var existingConsent = await _context.UserConsents
            .FirstOrDefaultAsync(c => c.UserId == userId && c.ConsentType == consentType);

        if (existingConsent != null)
        {
            if (!existingConsent.IsGiven)
            {
                existingConsent.IsGiven = true;
                existingConsent.Timestamp = DateTime.UtcNow;
                existingConsent.IpAddress = ipAddress;
                existingConsent.UserAgent = userAgent;
                await _context.SaveChangesAsync();
                
                await _auditLogService.LogAsync(userId, "Consent_Given", consentType, ipAddress: ipAddress, userAgent: userAgent);
            }
        }
        else
        {
            var consent = new UserConsent
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ConsentType = consentType,
                IsGiven = true,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Timestamp = DateTime.UtcNow
            };
            _context.UserConsents.Add(consent);
            await _context.SaveChangesAsync();
            
            await _auditLogService.LogAsync(userId, "Consent_Given", consentType, ipAddress: ipAddress, userAgent: userAgent);
        }
    }

    public async Task RevokeConsentAsync(Guid userId, string consentType, string? ipAddress = null, string? userAgent = null)
    {
        var consent = await _context.UserConsents
            .FirstOrDefaultAsync(c => c.UserId == userId && c.ConsentType == consentType);

        if (consent != null && consent.IsGiven)
        {
            consent.IsGiven = false;
            consent.Timestamp = DateTime.UtcNow;
            consent.IpAddress = ipAddress;
            consent.UserAgent = userAgent;
            await _context.SaveChangesAsync();
            
            await _auditLogService.LogAsync(userId, "Consent_Revoked", consentType, ipAddress: ipAddress, userAgent: userAgent);
        }
    }

    public async Task<IEnumerable<UserConsent>> GetUserConsentsAsync(Guid userId)
    {
        return await _context.UserConsents
            .Where(c => c.UserId == userId)
            .ToListAsync();
    }

    public async Task<bool> HasConsentAsync(Guid userId, string consentType)
    {
        return await _context.UserConsents
            .AnyAsync(c => c.UserId == userId && c.ConsentType == consentType && c.IsGiven);
    }
}
