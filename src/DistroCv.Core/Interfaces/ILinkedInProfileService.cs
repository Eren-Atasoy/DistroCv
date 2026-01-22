using DistroCv.Core.DTOs;
using DistroCv.Core.Entities;

namespace DistroCv.Core.Interfaces;

/// <summary>
/// Service interface for LinkedIn profile optimization
/// </summary>
public interface ILinkedInProfileService
{
    /// <summary>
    /// Task 18.1: Scrape LinkedIn profile data using Playwright
    /// </summary>
    Task<LinkedInProfileData> ScrapeProfileAsync(
        string linkedInUrl, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Task 18.2: Analyze profile using Gemini AI
    /// </summary>
    Task<LinkedInOptimizationResultDto> AnalyzeProfileAsync(
        Guid userId,
        LinkedInProfileAnalysisRequest request, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Task 18.3: Generate SEO and ATS-friendly optimization suggestions
    /// </summary>
    Task<OptimizedProfileDto> GenerateOptimizationsAsync(
        LinkedInProfileData profileData,
        List<string>? targetJobTitles = null,
        List<string>? targetIndustries = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Task 18.4: Get comparison view between original and optimized
    /// </summary>
    Task<List<ProfileComparisonDto>> GetComparisonViewAsync(
        Guid optimizationId,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Task 18.5: Calculate profile score (0-100)
    /// </summary>
    Task<ProfileScoreBreakdownDto> CalculateProfileScoreAsync(
        LinkedInProfileData profileData,
        List<string>? targetJobTitles = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get optimization by ID
    /// </summary>
    Task<LinkedInOptimizationResultDto?> GetOptimizationByIdAsync(
        Guid optimizationId,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get optimization history for user
    /// </summary>
    Task<List<ProfileOptimizationHistoryDto>> GetOptimizationHistoryAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Delete an optimization record
    /// </summary>
    Task DeleteOptimizationAsync(
        Guid optimizationId,
        Guid userId,
        CancellationToken cancellationToken = default);
}

