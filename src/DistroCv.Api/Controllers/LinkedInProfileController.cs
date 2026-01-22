using DistroCv.Core.DTOs;
using DistroCv.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DistroCv.Api.Controllers;

/// <summary>
/// Controller for LinkedIn profile optimization
/// Implements Tasks 18.1-18.5: LinkedIn Profile Optimization
/// </summary>
[Authorize]
[Route("api/linkedin-profile")]
public class LinkedInProfileController : BaseApiController
{
    private readonly ILinkedInProfileService _profileService;
    private readonly ILogger<LinkedInProfileController> _logger;

    public LinkedInProfileController(
        ILinkedInProfileService profileService,
        ILogger<LinkedInProfileController> logger)
    {
        _profileService = profileService;
        _logger = logger;
    }

    /// <summary>
    /// Analyze a LinkedIn profile and generate optimization suggestions
    /// Task 18.1, 18.2, 18.3: Scrape, analyze, and optimize profile
    /// </summary>
    [HttpPost("analyze")]
    public async Task<IActionResult> AnalyzeProfile([FromBody] LinkedInProfileAnalysisRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            _logger.LogInformation("Analyzing LinkedIn profile for user {UserId}: {Url}", userId, request.LinkedInUrl);

            var result = await _profileService.AnalyzeProfileAsync(userId, request);

            return Ok(new
            {
                message = "Profile analysis completed",
                result
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing LinkedIn profile");
            return StatusCode(500, new { message = "An error occurred while analyzing the profile" });
        }
    }

    /// <summary>
    /// Get comparison view between original and optimized profile
    /// Task 18.4: Comparison view
    /// </summary>
    [HttpGet("{optimizationId:guid}/comparison")]
    public async Task<IActionResult> GetComparisonView(Guid optimizationId)
    {
        try
        {
            _logger.LogInformation("Getting comparison view for optimization {OptimizationId}", optimizationId);

            var comparisons = await _profileService.GetComparisonViewAsync(optimizationId);

            return Ok(new
            {
                optimizationId,
                sections = comparisons
            });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting comparison view");
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Calculate profile score for a LinkedIn URL
    /// Task 18.5: Profile score calculation
    /// </summary>
    [HttpPost("score")]
    public async Task<IActionResult> CalculateScore([FromBody] LinkedInProfileScoreRequest request)
    {
        try
        {
            _logger.LogInformation("Calculating score for LinkedIn profile: {Url}", request.LinkedInUrl);

            var profileData = await _profileService.ScrapeProfileAsync(request.LinkedInUrl);
            var score = await _profileService.CalculateProfileScoreAsync(profileData, request.TargetJobTitles);

            return Ok(new
            {
                profileUrl = request.LinkedInUrl,
                score = score.OverallScore,
                breakdown = score,
                profileName = profileData.Name
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating profile score");
            return StatusCode(500, new { message = "An error occurred while calculating the score" });
        }
    }

    /// <summary>
    /// Get a specific optimization by ID
    /// </summary>
    [HttpGet("{optimizationId:guid}")]
    public async Task<IActionResult> GetOptimization(Guid optimizationId)
    {
        try
        {
            var result = await _profileService.GetOptimizationByIdAsync(optimizationId);

            if (result == null)
                return NotFound(new { message = "Optimization not found" });

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting optimization {OptimizationId}", optimizationId);
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Get optimization history for current user
    /// </summary>
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory()
    {
        try
        {
            var userId = GetCurrentUserId();
            var history = await _profileService.GetOptimizationHistoryAsync(userId);

            return Ok(new
            {
                total = history.Count,
                optimizations = history
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting optimization history");
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Delete an optimization record
    /// </summary>
    [HttpDelete("{optimizationId:guid}")]
    public async Task<IActionResult> DeleteOptimization(Guid optimizationId)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _profileService.DeleteOptimizationAsync(optimizationId, userId);

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting optimization");
            return StatusCode(500, new { message = "An error occurred" });
        }
    }
}

/// <summary>
/// Request for profile score calculation
/// </summary>
public record LinkedInProfileScoreRequest(
    string LinkedInUrl,
    List<string>? TargetJobTitles = null
);

