using DistroCv.Core.Entities;

namespace DistroCv.Core.Interfaces;

/// <summary>
/// Service interface for AI-powered job matching
/// </summary>
public interface IMatchingService
{
    /// <summary>
    /// Calculates match score between a user and job posting
    /// </summary>
    Task<JobMatch> CalculateMatchAsync(Guid userId, Guid jobPostingId);
    
    /// <summary>
    /// Generates reasoning for a match score
    /// </summary>
    Task<string> GenerateMatchReasoningAsync(DigitalTwin twin, JobPosting job);
    
    /// <summary>
    /// Analyzes skill gaps between a user and job requirements
    /// </summary>
    Task<List<string>> AnalyzeSkillGapsAsync(DigitalTwin twin, JobPosting job);
    
    /// <summary>
    /// Batch calculates matches for new job postings
    /// </summary>
    Task<IEnumerable<JobMatch>> BatchCalculateMatchesAsync(Guid userId, IEnumerable<Guid> jobPostingIds);
    
    /// <summary>
    /// Gets jobs matching criteria for a user (score >= threshold)
    /// </summary>
    Task<IEnumerable<JobMatch>> GetMatchingJobsAsync(Guid userId, decimal minScore = 80);
}
