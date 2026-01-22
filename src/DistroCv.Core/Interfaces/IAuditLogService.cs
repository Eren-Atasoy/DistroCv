using DistroCv.Core.Entities;

namespace DistroCv.Core.Interfaces;

public interface IAuditLogService
{
    Task LogAsync(Guid? userId, string action, string? resource = null, string? details = null, string? ipAddress = null, string? userAgent = null);
    Task<IEnumerable<AuditLog>> GetLogsAsync(Guid userId, int skip = 0, int take = 50);
}
