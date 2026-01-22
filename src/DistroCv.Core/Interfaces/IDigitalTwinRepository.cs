using DistroCv.Core.Entities;

namespace DistroCv.Core.Interfaces;

/// <summary>
/// Repository interface for DigitalTwin entity operations
/// </summary>
public interface IDigitalTwinRepository
{
    Task<DigitalTwin?> GetByIdAsync(Guid id);
    Task<DigitalTwin?> GetByUserIdAsync(Guid userId);
    Task<DigitalTwin> CreateAsync(DigitalTwin digitalTwin);
    Task<DigitalTwin> UpdateAsync(DigitalTwin digitalTwin);
    Task<bool> DeleteAsync(Guid id);
}
