using DistroCv.Core.Entities;
using DistroCv.Core.Interfaces;
using DistroCv.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DistroCv.Infrastructure.Services;

public class AuditLogService : IAuditLogService
{
    private readonly DistroCvDbContext _context;

    public AuditLogService(DistroCvDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(Guid? userId, string action, string? resource = null, string? details = null, string? ipAddress = null, string? userAgent = null)
    {
        var log = new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Action = action,
            Resource = resource,
            Details = details,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Timestamp = DateTime.UtcNow
        };

        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetLogsAsync(Guid userId, int skip = 0, int take = 50)
    {
        return await _context.AuditLogs
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.Timestamp)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }
}
