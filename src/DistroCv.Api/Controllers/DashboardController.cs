using DistroCv.Core.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace DistroCv.Api.Controllers;

/// <summary>
/// Dashboard and analytics controller
/// </summary>
public class DashboardController : BaseApiController
{
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(ILogger<DashboardController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get dashboard statistics
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var userId = GetCurrentUserId();
        
        _logger.LogInformation("Getting dashboard stats for user: {UserId}", userId);

        // TODO: Calculate stats from database
        var stats = new DashboardStatsDto(
            TotalApplications: 0,
            PendingApplications: 0,
            SentApplications: 0,
            ViewedApplications: 0,
            RespondedApplications: 0,
            RejectedApplications: 0,
            ResponseRate: 0,
            InterviewInvitations: 0,
            MatchingJobs: 0
        );

        return Ok(stats);
    }

    /// <summary>
    /// Get application trends
    /// </summary>
    [HttpGet("trends")]
    public async Task<IActionResult> GetTrends()
    {
        var userId = GetCurrentUserId();
        
        _logger.LogInformation("Getting trends for user: {UserId}", userId);

        // TODO: Calculate trends from database
        var trends = new DashboardTrendsDto(
            WeeklyApplications: new List<TrendDataPoint>(),
            MonthlyApplications: new List<TrendDataPoint>(),
            StatusBreakdown: new List<StatusBreakdown>()
        );

        return Ok(trends);
    }
}
