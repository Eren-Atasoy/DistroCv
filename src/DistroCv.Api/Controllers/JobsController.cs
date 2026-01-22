using DistroCv.Core.DTOs;
using DistroCv.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DistroCv.Api.Controllers;

/// <summary>
/// Job discovery and matching controller
/// </summary>
[Authorize]
public class JobsController : BaseApiController
{
    private readonly ILogger<JobsController> _logger;
    private readonly IMatchingService _matchingService;

    public JobsController(
        ILogger<JobsController> logger,
        IMatchingService matchingService)
    {
        _logger = logger;
        _matchingService = matchingService;
    }

    /// <summary>
    /// Get matched jobs for current user (score >= 80)
    /// </summary>
    [HttpGet("matches")]
    public async Task<IActionResult> GetMatchedJobs(
        [FromQuery] int skip = 0, 
        [FromQuery] int take = 20,
        [FromQuery] decimal minScore = 80)
    {
        try
        {
            var userId = GetCurrentUserId();
            
            _logger.LogInformation("Getting matched jobs for user: {UserId}, MinScore: {MinScore}", userId, minScore);

            // Get existing matches or find new ones
            var matches = await _matchingService.FindMatchesForUserAsync(userId, minScore);

            var matchDtos = matches
                .Skip(skip)
                .Take(take)
                .Select(m => new JobMatchDto(
                    m.Id,
                    m.JobPosting.Id,
                    m.JobPosting.Title,
                    m.JobPosting.CompanyName,
                    m.JobPosting.Location,
                    m.JobPosting.SalaryRange,
                    m.MatchScore,
                    m.MatchReasoning,
                    m.SkillGaps,
                    m.Status,
                    m.CalculatedAt
                ))
                .ToList();

            return Ok(new 
            { 
                jobs = matchDtos,
                total = matches.Count,
                skip = skip,
                take = take
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting matched jobs");
            return StatusCode(500, new { message = "An error occurred while fetching matched jobs" });
        }
    }

    /// <summary>
    /// Get job details by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetJobDetails(Guid id)
    {
        _logger.LogInformation("Getting job details: {JobId}", id);

        // TODO: Fetch job from repository
        return Ok(new { message = "Job details endpoint - implementation pending" });
    }

    /// <summary>
    /// Submit feedback for a rejected job match
    /// </summary>
    [HttpPost("{id:guid}/feedback")]
    public async Task<IActionResult> SubmitFeedback(Guid id, [FromBody] JobFeedbackDto dto)
    {
        var userId = GetCurrentUserId();
        
        if (string.IsNullOrEmpty(dto.Reason))
        {
            return BadRequest(new { message = "Feedback reason is required" });
        }

        _logger.LogInformation("Submitting feedback for job: {JobId}, Reason: {Reason}", id, dto.Reason);

        // TODO: Save feedback and update learning model
        return Ok(new { message = "Feedback submitted successfully" });
    }

    /// <summary>
    /// Approve a job match (swipe right)
    /// </summary>
    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> ApproveMatch(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            
            _logger.LogInformation("Approving job match: {JobId} for user: {UserId}", id, userId);

            var match = await _matchingService.ApproveMatchAsync(id, userId);

            return Ok(new 
            { 
                message = "Match approved. Starting application process...",
                matchId = match.Id,
                status = match.Status,
                isInQueue = match.IsInQueue
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized approve attempt");
            return Unauthorized(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid approve operation");
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving match");
            return StatusCode(500, new { message = "An error occurred while approving the match" });
        }
    }

    /// <summary>
    /// Reject a job match (swipe left)
    /// </summary>
    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> RejectMatch(Guid id, [FromBody] JobFeedbackDto? dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            
            _logger.LogInformation("Rejecting job match: {JobId} for user: {UserId}", id, userId);

            var match = await _matchingService.RejectMatchAsync(id, userId);

            // TODO: If feedback provided, save it for learning
            if (dto != null && !string.IsNullOrEmpty(dto.Reason))
            {
                _logger.LogInformation("Feedback provided for rejection: {Reason}", dto.Reason);
            }

            return Ok(new 
            { 
                message = "Match rejected",
                matchId = match.Id,
                status = match.Status
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized reject attempt");
            return Unauthorized(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid reject operation");
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting match");
            return StatusCode(500, new { message = "An error occurred while rejecting the match" });
        }
    }
}
