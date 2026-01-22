using DistroCv.Core.DTOs;
using DistroCv.Core.Entities;

namespace DistroCv.Core.Interfaces;

/// <summary>
/// Service interface for skill gap analysis operations
/// </summary>
public interface ISkillGapService
{
    /// <summary>
    /// Analyze skill gaps between a user's profile and a job posting
    /// Task 17.1: Skill gap detection
    /// </summary>
    Task<SkillGapAnalysisResultDto> AnalyzeSkillGapsAsync(
        Guid userId, 
        Guid jobMatchId, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Analyze skill gaps for user's career goals (without specific job)
    /// </summary>
    Task<SkillGapAnalysisResultDto> AnalyzeCareerGapsAsync(
        Guid userId, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get all skill gaps for a user with filtering
    /// </summary>
    Task<List<SkillGapDto>> GetUserSkillGapsAsync(
        Guid userId, 
        SkillGapFilterDto? filter = null, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get a specific skill gap by ID
    /// </summary>
    Task<SkillGapDto?> GetSkillGapByIdAsync(
        Guid skillGapId, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Generate course recommendations for a skill gap
    /// Task 17.3: Course recommendation
    /// </summary>
    Task<List<CourseRecommendationDto>> GetCourseRecommendationsAsync(
        string skillName, 
        string category,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Generate project suggestions for a skill gap
    /// Task 17.4: Project suggestions
    /// </summary>
    Task<List<ProjectSuggestionDto>> GetProjectSuggestionsAsync(
        string skillName, 
        string category,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Generate certification recommendations for a skill
    /// </summary>
    Task<List<CertificationRecommendationDto>> GetCertificationRecommendationsAsync(
        string skillName, 
        string category,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Update progress for a skill gap
    /// Task 17.5: Progress tracking
    /// </summary>
    Task<SkillGapDto> UpdateProgressAsync(
        Guid skillGapId, 
        Guid userId,
        UpdateSkillGapProgressDto dto, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Mark a skill gap as completed
    /// </summary>
    Task<SkillGapDto> MarkAsCompletedAsync(
        Guid skillGapId, 
        Guid userId,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get skill development progress summary for a user
    /// </summary>
    Task<SkillDevelopmentProgressDto> GetDevelopmentProgressAsync(
        Guid userId, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Delete a skill gap entry
    /// </summary>
    Task DeleteSkillGapAsync(
        Guid skillGapId, 
        Guid userId,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Recalculate match score after skill completion
    /// </summary>
    Task<decimal> RecalculateMatchScoreAsync(
        Guid userId, 
        Guid jobMatchId, 
        CancellationToken cancellationToken = default);
}

