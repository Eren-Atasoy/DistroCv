using DistroCv.Core.DTOs;
using DistroCv.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DistroCv.Api.Controllers;

/// <summary>
/// Controller for skill gap analysis and learning recommendations
/// Implements Tasks 17.1-17.5: Skill Gap Analysis
/// </summary>
[Authorize]
[Route("api/skill-gaps")]
public class SkillGapController : BaseApiController
{
    private readonly ILogger<SkillGapController> _logger;
    private readonly ISkillGapService _skillGapService;

    public SkillGapController(
        ILogger<SkillGapController> logger,
        ISkillGapService skillGapService)
    {
        _logger = logger;
        _skillGapService = skillGapService;
    }

    /// <summary>
    /// Analyze skill gaps for a specific job match
    /// Task 17.1: Skill gap detection
    /// </summary>
    [HttpPost("analyze/{jobMatchId:guid}")]
    public async Task<IActionResult> AnalyzeSkillGaps(Guid jobMatchId)
    {
        try
        {
            var userId = GetCurrentUserId();
            _logger.LogInformation("Analyzing skill gaps for user {UserId} and job match {JobMatchId}", userId, jobMatchId);

            var result = await _skillGapService.AnalyzeSkillGapsAsync(userId, jobMatchId);

            return Ok(new
            {
                message = "Skill gap analysis completed",
                result
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Analysis failed: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing skill gaps");
            return StatusCode(500, new { message = "An error occurred while analyzing skill gaps" });
        }
    }

    /// <summary>
    /// Analyze skill gaps based on career goals (without specific job)
    /// </summary>
    [HttpPost("analyze-career")]
    public async Task<IActionResult> AnalyzeCareerGaps()
    {
        try
        {
            var userId = GetCurrentUserId();
            _logger.LogInformation("Analyzing career gaps for user {UserId}", userId);

            var result = await _skillGapService.AnalyzeCareerGapsAsync(userId);

            return Ok(new
            {
                message = "Career gap analysis completed",
                result
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Analysis failed: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing career gaps");
            return StatusCode(500, new { message = "An error occurred while analyzing career gaps" });
        }
    }

    /// <summary>
    /// Get all skill gaps for the current user
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetSkillGaps([FromQuery] SkillGapFilterDto? filter)
    {
        try
        {
            var userId = GetCurrentUserId();
            var gaps = await _skillGapService.GetUserSkillGapsAsync(userId, filter);

            return Ok(new
            {
                gaps,
                total = gaps.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting skill gaps");
            return StatusCode(500, new { message = "An error occurred while fetching skill gaps" });
        }
    }

    /// <summary>
    /// Get a specific skill gap by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetSkillGap(Guid id)
    {
        try
        {
            var gap = await _skillGapService.GetSkillGapByIdAsync(id);

            if (gap == null)
                return NotFound(new { message = "Skill gap not found" });

            return Ok(gap);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting skill gap {Id}", id);
            return StatusCode(500, new { message = "An error occurred while fetching skill gap" });
        }
    }

    /// <summary>
    /// Get course recommendations for a skill
    /// Task 17.3: Course recommendation
    /// </summary>
    [HttpGet("courses/{skillName}")]
    public async Task<IActionResult> GetCourseRecommendations(string skillName, [FromQuery] string category = "Technical")
    {
        try
        {
            _logger.LogInformation("Getting course recommendations for skill: {SkillName}", skillName);

            var courses = await _skillGapService.GetCourseRecommendationsAsync(skillName, category);

            return Ok(new
            {
                skillName,
                category,
                courses,
                count = courses.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting course recommendations");
            return StatusCode(500, new { message = "An error occurred while fetching course recommendations" });
        }
    }

    /// <summary>
    /// Get project suggestions for a skill
    /// Task 17.4: Project suggestions
    /// </summary>
    [HttpGet("projects/{skillName}")]
    public async Task<IActionResult> GetProjectSuggestions(string skillName, [FromQuery] string category = "Technical")
    {
        try
        {
            _logger.LogInformation("Getting project suggestions for skill: {SkillName}", skillName);

            var projects = await _skillGapService.GetProjectSuggestionsAsync(skillName, category);

            return Ok(new
            {
                skillName,
                category,
                projects,
                count = projects.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting project suggestions");
            return StatusCode(500, new { message = "An error occurred while fetching project suggestions" });
        }
    }

    /// <summary>
    /// Get certification recommendations for a skill
    /// </summary>
    [HttpGet("certifications/{skillName}")]
    public async Task<IActionResult> GetCertificationRecommendations(string skillName, [FromQuery] string category = "Technical")
    {
        try
        {
            _logger.LogInformation("Getting certification recommendations for skill: {SkillName}", skillName);

            var certifications = await _skillGapService.GetCertificationRecommendationsAsync(skillName, category);

            return Ok(new
            {
                skillName,
                category,
                certifications,
                count = certifications.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting certification recommendations");
            return StatusCode(500, new { message = "An error occurred while fetching certification recommendations" });
        }
    }

    /// <summary>
    /// Update progress for a skill gap
    /// Task 17.5: Progress tracking
    /// </summary>
    [HttpPut("{id:guid}/progress")]
    public async Task<IActionResult> UpdateProgress(Guid id, [FromBody] UpdateSkillGapProgressDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            _logger.LogInformation("Updating progress for skill gap {Id}", id);

            var result = await _skillGapService.UpdateProgressAsync(id, userId, dto);

            return Ok(new
            {
                message = "Progress updated",
                skillGap = result
            });
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
            _logger.LogError(ex, "Error updating progress");
            return StatusCode(500, new { message = "An error occurred while updating progress" });
        }
    }

    /// <summary>
    /// Mark a skill gap as completed
    /// </summary>
    [HttpPost("{id:guid}/complete")]
    public async Task<IActionResult> MarkAsCompleted(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            _logger.LogInformation("Marking skill gap {Id} as completed", id);

            var result = await _skillGapService.MarkAsCompletedAsync(id, userId);

            return Ok(new
            {
                message = "Skill marked as completed",
                skillGap = result
            });
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
            _logger.LogError(ex, "Error marking skill as completed");
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Get overall skill development progress
    /// </summary>
    [HttpGet("progress")]
    public async Task<IActionResult> GetDevelopmentProgress()
    {
        try
        {
            var userId = GetCurrentUserId();
            var progress = await _skillGapService.GetDevelopmentProgressAsync(userId);

            return Ok(progress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting development progress");
            return StatusCode(500, new { message = "An error occurred while fetching progress" });
        }
    }

    /// <summary>
    /// Delete a skill gap entry
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteSkillGap(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _skillGapService.DeleteSkillGapAsync(id, userId);

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
            _logger.LogError(ex, "Error deleting skill gap");
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Recalculate match score after skill completion
    /// </summary>
    [HttpPost("recalculate-match/{jobMatchId:guid}")]
    public async Task<IActionResult> RecalculateMatchScore(Guid jobMatchId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var newScore = await _skillGapService.RecalculateMatchScoreAsync(userId, jobMatchId);

            return Ok(new
            {
                message = "Match score recalculated",
                jobMatchId,
                newScore
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recalculating match score");
            return StatusCode(500, new { message = "An error occurred" });
        }
    }
}

