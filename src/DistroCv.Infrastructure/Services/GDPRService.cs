using System.Text.Json;
using System.Text.Json.Serialization;
using DistroCv.Core.Interfaces;
using DistroCv.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DistroCv.Infrastructure.Services;

public class GDPRService : IGDPRService
{
    private readonly DistroCvDbContext _context;
    private readonly IAuditLogService _auditLogService;

    public GDPRService(DistroCvDbContext context, IAuditLogService auditLogService)
    {
        _context = context;
        _auditLogService = auditLogService;
    }

    public async Task<string> ExportUserDataAsync(Guid userId)
    {
        var user = await _context.Users
            .Include(u => u.DigitalTwin)
            .Include(u => u.Applications)
                .ThenInclude(a => a.Logs)
            .Include(u => u.JobMatches)
            .Include(u => u.Feedbacks)
            .Include(u => u.ThrottleLogs)
            .Include(u => u.Sessions)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null) throw new Exception("User not found");

        // Mask sensitive data (Session tokens)
        foreach (var session in user.Sessions) 
        {
            session.AccessToken = "***";
            session.RefreshToken = "***";
        }

        var options = new JsonSerializerOptions 
        { 
            WriteIndented = true,
            ReferenceHandler = ReferenceHandler.IgnoreCycles 
        };
        
        var json = JsonSerializer.Serialize(user, options);
        
        await _auditLogService.LogAsync(userId, "GDPR_Export", "UserData");

        return json;
    }

    public async Task DeleteUserAccountAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return; // Or throw

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        
        // Log is stored with UserId null (since user is deleted, or we keep ID but relation is SetNull)
        // Since we configured AuditLog.User relation as SetNull, the ID remains in DB but User is null.
        // We can explicitly log with userId.
        await _auditLogService.LogAsync(userId, "GDPR_Delete", "Account");
    }
}
