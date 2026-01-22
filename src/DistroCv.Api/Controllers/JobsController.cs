using DistroCv.Core.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace DistroCv.Api.Controllers;

/// <summary>
/// Job discovery and matching controller
/// </summary>
public class JobsController : BaseApiController
{
    private readonly ILogger<JobsController> _logger;

    public JobsController(ILogger<JobsController> logger)
    {
        _logger = logger;
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
        var userId = GetCurrentUserId();
        
        _logger.LogInformation("Getting matched jobs for user: {UserId}, MinScore: {MinScore}", userId, minScore);

        // TODO: Fetch matches from MatchingService
        return Ok(new 
        { 
            jobs = new List<JobMatchDto>(),
            total = 0,
            message = "Matched jobs endpoint - implementation pending"
        });
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
        var userId = GetCurrentUserId();
        
        _logger.LogInformation("Approving job match: {JobId} for user: {UserId}", id, userId);

        // TODO: Update match status and start application process
        return Ok(new { message = "Match approved. Starting application process..." });
    }

    /// <summary>
    /// Reject a job match (swipe left)
    /// </summary>
    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> RejectMatch(Guid id, [FromBody] JobFeedbackDto? dto)
    {
        var userId = GetCurrentUserId();
        
        _logger.LogInformation("Rejecting job match: {JobId} for user: {UserId}", id, userId);

        // TODO: Update match status and optionally collect feedback
        return Ok(new { message = "Match rejected" });
    }
}
