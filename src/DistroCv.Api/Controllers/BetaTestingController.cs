using System;
using System.Threading;
using System.Threading.Tasks;
using DistroCv.Core.DTOs;
using DistroCv.Core.Entities;
using DistroCv.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DistroCv.Api.Controllers;

/// <summary>
/// Controller for beta testing management - bug reports, feature requests, surveys
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class BetaTestingController : BaseApiController
{
    private readonly IBetaTestingService _betaTestingService;

    public BetaTestingController(IBetaTestingService betaTestingService)
    {
        _betaTestingService = betaTestingService;
    }

    #region Beta Tester Endpoints

    /// <summary>
    /// Apply to become a beta tester (public)
    /// </summary>
    [HttpPost("apply")]
    [AllowAnonymous]
    public async Task<ActionResult<BetaTesterResponseDto>> ApplyForBeta(
        [FromBody] BetaTesterApplicationDto application,
        CancellationToken cancellationToken)
    {
        var result = await _betaTestingService.ApplyForBetaAsync(application, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get all beta testers (admin only)
    /// </summary>
    [HttpGet("testers")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> GetBetaTesters(
        [FromQuery] BetaTesterStatus? status,
        CancellationToken cancellationToken)
    {
        var testers = await _betaTestingService.GetBetaTestersAsync(status, cancellationToken);
        return Ok(testers);
    }

    /// <summary>
    /// Get beta tester by ID (admin only)
    /// </summary>
    [HttpGet("testers/{testerId}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> GetBetaTester(Guid testerId, CancellationToken cancellationToken)
    {
        var tester = await _betaTestingService.GetBetaTesterAsync(testerId, cancellationToken);
        if (tester == null) return NotFound();
        return Ok(tester);
    }

    /// <summary>
    /// Approve a beta tester (admin only)
    /// </summary>
    [HttpPost("testers/{testerId}/approve")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> ApproveBetaTester(Guid testerId, CancellationToken cancellationToken)
    {
        var result = await _betaTestingService.ApproveBetaTesterAsync(testerId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Reject a beta tester (admin only)
    /// </summary>
    [HttpPost("testers/{testerId}/reject")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> RejectBetaTester(
        Guid testerId,
        [FromBody] RejectBetaTesterRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _betaTestingService.RejectBetaTesterAsync(testerId, request.Reason, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get beta testing statistics (admin only)
    /// </summary>
    [HttpGet("stats")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<BetaTesterStatsDto>> GetBetaStats(CancellationToken cancellationToken)
    {
        var stats = await _betaTestingService.GetBetaStatsAsync(cancellationToken);
        return Ok(stats);
    }

    #endregion

    #region Bug Report Endpoints

    /// <summary>
    /// Submit a bug report
    /// </summary>
    [HttpPost("bugs")]
    [Authorize]
    public async Task<ActionResult<BugReportResponseDto>> SubmitBugReport(
        [FromBody] CreateBugReportDto bugReport,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _betaTestingService.SubmitBugReportAsync(userId, bugReport, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get all bug reports
    /// </summary>
    [HttpGet("bugs")]
    [Authorize]
    public async Task<ActionResult> GetBugReports(
        [FromQuery] BugStatus? status,
        [FromQuery] BugPriority? priority,
        CancellationToken cancellationToken)
    {
        var bugs = await _betaTestingService.GetBugReportsAsync(status, priority, cancellationToken);
        return Ok(bugs);
    }

    /// <summary>
    /// Get bug report by ID
    /// </summary>
    [HttpGet("bugs/{bugId}")]
    [Authorize]
    public async Task<ActionResult> GetBugReport(Guid bugId, CancellationToken cancellationToken)
    {
        var bug = await _betaTestingService.GetBugReportAsync(bugId, cancellationToken);
        if (bug == null) return NotFound();
        return Ok(bug);
    }

    /// <summary>
    /// Update bug report (admin only)
    /// </summary>
    [HttpPut("bugs/{bugId}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> UpdateBugReport(
        Guid bugId,
        [FromBody] UpdateBugReportDto update,
        CancellationToken cancellationToken)
    {
        var result = await _betaTestingService.UpdateBugReportAsync(bugId, update, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get bug statistics (admin only)
    /// </summary>
    [HttpGet("bugs/stats")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<BugReportStatsDto>> GetBugStats(CancellationToken cancellationToken)
    {
        var stats = await _betaTestingService.GetBugStatsAsync(cancellationToken);
        return Ok(stats);
    }

    /// <summary>
    /// Add comment to bug report
    /// </summary>
    [HttpPost("bugs/{bugId}/comments")]
    [Authorize]
    public async Task<ActionResult> AddBugComment(
        Guid bugId,
        [FromBody] AddCommentRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        await _betaTestingService.AddBugCommentAsync(bugId, userId, request.Comment, request.IsInternal, cancellationToken);
        return Ok();
    }

    /// <summary>
    /// Vote for a bug report (confirm it's real)
    /// </summary>
    [HttpPost("bugs/{bugId}/vote")]
    [Authorize]
    public async Task<ActionResult> VoteBugReport(Guid bugId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        await _betaTestingService.VoteBugReportAsync(bugId, userId, cancellationToken);
        return Ok();
    }

    #endregion

    #region Feature Request Endpoints

    /// <summary>
    /// Submit a feature request
    /// </summary>
    [HttpPost("features")]
    [Authorize]
    public async Task<ActionResult<FeatureRequestResponseDto>> SubmitFeatureRequest(
        [FromBody] CreateFeatureRequestDto request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _betaTestingService.SubmitFeatureRequestAsync(userId, request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get all feature requests
    /// </summary>
    [HttpGet("features")]
    [Authorize]
    public async Task<ActionResult> GetFeatureRequests(
        [FromQuery] FeatureStatus? status,
        [FromQuery] FeatureCategory? category,
        CancellationToken cancellationToken)
    {
        var requests = await _betaTestingService.GetFeatureRequestsAsync(status, category, cancellationToken);
        return Ok(requests);
    }

    /// <summary>
    /// Get feature request by ID
    /// </summary>
    [HttpGet("features/{requestId}")]
    [Authorize]
    public async Task<ActionResult> GetFeatureRequest(Guid requestId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var request = await _betaTestingService.GetFeatureRequestAsync(requestId, userId, cancellationToken);
        if (request == null) return NotFound();
        return Ok(request);
    }

    /// <summary>
    /// Update feature request (admin only)
    /// </summary>
    [HttpPut("features/{requestId}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> UpdateFeatureRequest(
        Guid requestId,
        [FromBody] UpdateFeatureRequestDto update,
        CancellationToken cancellationToken)
    {
        var result = await _betaTestingService.UpdateFeatureRequestAsync(requestId, update, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get feature request statistics (admin only)
    /// </summary>
    [HttpGet("features/stats")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<FeatureRequestStatsDto>> GetFeatureRequestStats(CancellationToken cancellationToken)
    {
        var stats = await _betaTestingService.GetFeatureRequestStatsAsync(cancellationToken);
        return Ok(stats);
    }

    /// <summary>
    /// Vote for a feature request
    /// </summary>
    [HttpPost("features/{requestId}/vote")]
    [Authorize]
    public async Task<ActionResult<int>> VoteFeatureRequest(Guid requestId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var voteCount = await _betaTestingService.VoteFeatureRequestAsync(requestId, userId, cancellationToken);
        return Ok(new { voteCount });
    }

    /// <summary>
    /// Remove vote from a feature request
    /// </summary>
    [HttpDelete("features/{requestId}/vote")]
    [Authorize]
    public async Task<ActionResult<int>> UnvoteFeatureRequest(Guid requestId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var voteCount = await _betaTestingService.UnvoteFeatureRequestAsync(requestId, userId, cancellationToken);
        return Ok(new { voteCount });
    }

    /// <summary>
    /// Add comment to feature request
    /// </summary>
    [HttpPost("features/{requestId}/comments")]
    [Authorize]
    public async Task<ActionResult> AddFeatureComment(
        Guid requestId,
        [FromBody] AddCommentRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        await _betaTestingService.AddFeatureCommentAsync(requestId, userId, request.Comment, request.IsInternal, cancellationToken);
        return Ok();
    }

    #endregion

    #region Survey Endpoints

    /// <summary>
    /// Create a new survey (admin only)
    /// </summary>
    [HttpPost("surveys")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<SurveyResponseDto>> CreateSurvey(
        [FromBody] CreateSurveyDto survey,
        CancellationToken cancellationToken)
    {
        var result = await _betaTestingService.CreateSurveyAsync(survey, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get active surveys for current user
    /// </summary>
    [HttpGet("surveys/active")]
    [Authorize]
    public async Task<ActionResult> GetActiveSurveys(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var surveys = await _betaTestingService.GetActiveSurveysForUserAsync(userId, cancellationToken);
        return Ok(surveys);
    }

    /// <summary>
    /// Get survey by ID
    /// </summary>
    [HttpGet("surveys/{surveyId}")]
    [Authorize]
    public async Task<ActionResult> GetSurvey(Guid surveyId, CancellationToken cancellationToken)
    {
        var survey = await _betaTestingService.GetSurveyAsync(surveyId, cancellationToken);
        if (survey == null) return NotFound();
        return Ok(survey);
    }

    /// <summary>
    /// Submit survey response
    /// </summary>
    [HttpPost("surveys/{surveyId}/responses")]
    [Authorize]
    public async Task<ActionResult> SubmitSurveyResponse(
        Guid surveyId,
        [FromBody] SubmitSurveyResponseDto response,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        response.SurveyId = surveyId;
        await _betaTestingService.SubmitSurveyResponseAsync(userId, response, cancellationToken);
        return Ok();
    }

    /// <summary>
    /// Get survey results (admin only)
    /// </summary>
    [HttpGet("surveys/{surveyId}/results")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<SurveyResultsDto>> GetSurveyResults(
        Guid surveyId,
        CancellationToken cancellationToken)
    {
        var results = await _betaTestingService.GetSurveyResultsAsync(surveyId, cancellationToken);
        return Ok(results);
    }

    /// <summary>
    /// Activate/Deactivate survey (admin only)
    /// </summary>
    [HttpPut("surveys/{surveyId}/active")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> SetSurveyActive(
        Guid surveyId,
        [FromBody] SetSurveyActiveRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _betaTestingService.SetSurveyActiveAsync(surveyId, request.IsActive, cancellationToken);
        return Ok(result);
    }

    #endregion

    #region Performance Monitoring Endpoints

    /// <summary>
    /// Get current performance metrics (admin only)
    /// </summary>
    [HttpGet("performance")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PerformanceMetricsDto>> GetPerformanceMetrics(CancellationToken cancellationToken)
    {
        var metrics = await _betaTestingService.GetPerformanceMetricsAsync(cancellationToken);
        return Ok(metrics);
    }

    /// <summary>
    /// Get historical performance metrics (admin only)
    /// </summary>
    [HttpGet("performance/history")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> GetHistoricalMetrics(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        CancellationToken cancellationToken)
    {
        var metrics = await _betaTestingService.GetHistoricalMetricsAsync(startDate, endDate, cancellationToken);
        return Ok(metrics);
    }

    /// <summary>
    /// Get user engagement metrics (admin only)
    /// </summary>
    [HttpGet("engagement")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UserEngagementMetricsDto>> GetUserEngagementMetrics(CancellationToken cancellationToken)
    {
        var metrics = await _betaTestingService.GetUserEngagementMetricsAsync(cancellationToken);
        return Ok(metrics);
    }

    #endregion
}

#region Request Models

public class RejectBetaTesterRequest
{
    public string Reason { get; set; } = string.Empty;
}

public class AddCommentRequest
{
    public string Comment { get; set; } = string.Empty;
    public bool IsInternal { get; set; }
}

public class SetSurveyActiveRequest
{
    public bool IsActive { get; set; }
}

#endregion

