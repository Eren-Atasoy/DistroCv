using DistroCv.Api.Controllers;
using DistroCv.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DistroCv.Api.Controllers;

/// <summary>
/// Controller for managing user feedback and learning system
/// </summary>
[Authorize]
public class FeedbackController : BaseApiController
{
    private readonly IFeedbackService _feedbackService;
    private readonly ILogger<FeedbackController> _logger;

    public FeedbackController(
        IFeedbackService feedbackService,
        ILogger<FeedbackController> logger)
    {
        _feedbackService = feedbackService;
        _logger = logger;
    }

    /// <summary>
    /// Submit feedback for a job match (Validates: Requirement 16.1, 16.2, 16.3)
    /// </summary>
    [HttpPost("submit")]
    public async Task<IActionResult> SubmitFeedback([FromBody] SubmitFeedbackRequest request)
    {
        try
        {
            var userId = GetUserId();
            
            await _feedbackService.SubmitFeedbackAsync(
                userId,
                request.JobMatchId,
                request.FeedbackType,
                request.Reason,
                request.AdditionalNotes);

            _logger.LogInformation("Feedback submitted for user {UserId}, job match {JobMatchId}", 
                userId, request.JobMatchId);

            return Ok(new { message = "Feedback submitted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting feedback");
            return StatusCode(500, new { error = "Failed to submit feedback" });
        }
    }

    /// <summary>
    /// Get feedback analytics for the current user
    /// </summary>
    [HttpGet("analytics")]
    public async Task<IActionResult> GetAnalytics()
    {
        try
        {
            var userId = GetUserId();
            var analytics = await _feedbackService.GetFeedbackAnalyticsAsync(userId);

            return Ok(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting feedback analytics");
            return StatusCode(500, new { error = "Failed to get analytics" });
        }
    }

    /// <summary>
    /// Get all feedback for the current user
    /// </summary>
    [HttpGet("history")]
    public async Task<IActionResult> GetFeedbackHistory()
    {
        try
        {
            var userId = GetUserId();
            var feedbacks = await _feedbackService.GetUserFeedbackAsync(userId);

            return Ok(feedbacks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting feedback history");
            return StatusCode(500, new { error = "Failed to get feedback history" });
        }
    }

    /// <summary>
    /// Check if learning model is active for the current user
    /// </summary>
    [HttpGet("learning-status")]
    public async Task<IActionResult> GetLearningStatus()
    {
        try
        {
            var userId = GetUserId();
            var isActive = await _feedbackService.ShouldActivateLearningModelAsync(userId);
            var feedbackCount = await _feedbackService.GetFeedbackCountAsync(userId);

            return Ok(new 
            { 
                isLearningModelActive = isActive,
                feedbackCount = feedbackCount,
                threshold = 10
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting learning status");
            return StatusCode(500, new { error = "Failed to get learning status" });
        }
    }

    /// <summary>
    /// Manually trigger learning model analysis (admin/testing)
    /// </summary>
    [HttpPost("analyze")]
    public async Task<IActionResult> TriggerAnalysis()
    {
        try
        {
            var userId = GetUserId();
            
            var shouldActivate = await _feedbackService.ShouldActivateLearningModelAsync(userId);
            if (!shouldActivate)
            {
                return BadRequest(new { error = "Not enough feedback to activate learning model (minimum 10 required)" });
            }

            await _feedbackService.AnalyzeFeedbackAndUpdateWeightsAsync(userId);

            _logger.LogInformation("Learning model analysis triggered for user {UserId}", userId);

            return Ok(new { message = "Learning model analysis completed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering learning model analysis");
            return StatusCode(500, new { error = "Failed to trigger analysis" });
        }
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                          ?? User.FindFirst("sub")?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("User ID not found in token");
        }

        return userId;
    }
}

/// <summary>
/// Request model for submitting feedback
/// </summary>
public class SubmitFeedbackRequest
{
    public Guid JobMatchId { get; set; }
    public string FeedbackType { get; set; } = "Rejected"; // "Rejected" or "Approved"
    public string? Reason { get; set; }
    public string? AdditionalNotes { get; set; }
}
