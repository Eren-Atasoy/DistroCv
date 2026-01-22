using DistroCv.Core.Entities;

namespace DistroCv.Core.Interfaces;

/// <summary>
/// Repository interface for LinkedIn profile optimization data
/// </summary>
public interface ILinkedInProfileRepository
{
    /// <summary>
    /// Get optimization by ID
    /// </summary>
    Task<LinkedInProfileOptimization?> GetByIdAsync(
        Guid id, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get all optimizations for a user
    /// </summary>
    Task<List<LinkedInProfileOptimization>> GetByUserIdAsync(
        Guid userId, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get latest optimization for a user
    /// </summary>
    Task<LinkedInProfileOptimization?> GetLatestByUserIdAsync(
        Guid userId, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Create a new optimization record
    /// </summary>
    Task<LinkedInProfileOptimization> CreateAsync(
        LinkedInProfileOptimization optimization, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Update an optimization record
    /// </summary>
    Task<LinkedInProfileOptimization> UpdateAsync(
        LinkedInProfileOptimization optimization, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Delete an optimization record
    /// </summary>
    Task DeleteAsync(
        Guid id, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Check if user has recent optimization (within 24 hours)
    /// </summary>
    Task<bool> HasRecentOptimizationAsync(
        Guid userId, 
        string linkedInUrl, 
        CancellationToken cancellationToken = default);
}

