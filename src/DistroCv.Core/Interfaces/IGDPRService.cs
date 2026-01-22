using DistroCv.Core.DTOs;

namespace DistroCv.Core.Interfaces;

public interface IGDPRService
{
    Task<string> ExportUserDataAsync(Guid userId);
    Task DeleteUserAccountAsync(Guid userId);
}
