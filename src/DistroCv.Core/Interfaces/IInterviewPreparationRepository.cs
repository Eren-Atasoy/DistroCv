using DistroCv.Core.Entities;

namespace DistroCv.Core.Interfaces;

/// <summary>
/// Repository interface for InterviewPreparation entity operations
/// </summary>
public interface IInterviewPreparationRepository
{
    Task<InterviewPreparation?> GetByIdAsync(Guid id);
    Task<InterviewPreparation?> GetByApplicationIdAsync(Guid applicationId);
    Task<InterviewPreparation> CreateAsync(InterviewPreparation preparation);
    Task<InterviewPreparation> UpdateAsync(InterviewPreparation preparation);
    Task<bool> DeleteAsync(Guid id);
}
