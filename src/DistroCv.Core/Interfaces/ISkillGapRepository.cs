using DistroCv.Core.Entities;

namespace DistroCv.Core.Interfaces;

/// <summary>
/// Repository interface for skill gap data operations
/// </summary>
public interface ISkillGapRepository
{
    /// <summary>
    /// Get a skill gap by ID
    /// </summary>
    Task<SkillGapAnalysis?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get all skill gaps for a user
    /// </summary>
    Task<List<SkillGapAnalysis>> GetByUserIdAsync(
        Guid userId, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get skill gaps for a user with filtering
    /// </summary>
    Task<List<SkillGapAnalysis>> GetFilteredAsync(
        Guid userId,
        string? category = null,
        string? status = null,
        int? minImportance = null,
        Guid? jobMatchId = null,
        int skip = 0,
        int take = 20,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get skill gaps for a specific job match
    /// </summary>
    Task<List<SkillGapAnalysis>> GetByJobMatchIdAsync(
        Guid jobMatchId, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Create a new skill gap entry
    /// </summary>
    Task<SkillGapAnalysis> CreateAsync(
        SkillGapAnalysis skillGap, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Create multiple skill gap entries
    /// </summary>
    Task CreateRangeAsync(
        IEnumerable<SkillGapAnalysis> skillGaps, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Update a skill gap entry
    /// </summary>
    Task<SkillGapAnalysis> UpdateAsync(
        SkillGapAnalysis skillGap, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Delete a skill gap entry
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Check if a skill gap exists for user
    /// </summary>
    Task<bool> ExistsAsync(
        Guid userId, 
        string skillName, 
        Guid? jobMatchId = null, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get count by status for a user
    /// </summary>
    Task<Dictionary<string, int>> GetStatusCountsAsync(
        Guid userId, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get count by category for a user
    /// </summary>
    Task<Dictionary<string, int>> GetCategoryCountsAsync(
        Guid userId, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get recently completed skill gaps
    /// </summary>
    Task<List<SkillGapAnalysis>> GetRecentlyCompletedAsync(
        Guid userId, 
        int count = 5, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get currently in-progress skill gaps
    /// </summary>
    Task<List<SkillGapAnalysis>> GetInProgressAsync(
        Guid userId, 
        int count = 10, 
        CancellationToken cancellationToken = default);
}

