using DistroCv.Core.Entities;

namespace DistroCv.Core.Interfaces;

/// <summary>
/// Service interface for interview coaching and preparation
/// </summary>
public interface IInterviewCoachService
{
    /// <summary>
    /// Generates interview questions for a specific job
    /// </summary>
    Task<List<string>> GenerateQuestionsAsync(JobPosting job, VerifiedCompany? company = null);
    
    /// <summary>
    /// Analyzes a user's answer using STAR technique
    /// </summary>
    Task<string> AnalyzeAnswerAsync(string question, string answer);
    
    /// <summary>
    /// Generates improvement suggestions based on answers
    /// </summary>
    Task<List<string>> GenerateImprovementSuggestionsAsync(List<string> answers);
    
    /// <summary>
    /// Gets interview preparation for an application
    /// </summary>
    Task<InterviewPreparation?> GetPreparationAsync(Guid applicationId);
    
    /// <summary>
    /// Creates interview preparation for an application
    /// </summary>
    Task<InterviewPreparation> CreatePreparationAsync(Guid applicationId);
}
