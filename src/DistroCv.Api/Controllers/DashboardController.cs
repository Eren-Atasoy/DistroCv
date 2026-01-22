using DistroCv.Core.DTOs;
using DistroCv.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DistroCv.Api.Controllers;

/// <summary>
/// Dashboard and analytics controller
/// </summary>
public class DashboardController : BaseApiController
{
    private readonly DistroCvDbContext _context;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        DistroCvDbContext context,
        ILogger<DashboardController> logger)
    {
        _context = context;
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

        var applications = await _context.Applications
            .Where(a => a.UserId == userId)
            .ToListAsync();

        var total = applications.Count;
        var pending = applications.Count(a => a.Status == "Queued");
        var sent = applications.Count(a => a.Status == "Sent");
        var viewed = applications.Count(a => a.Status == "Viewed");
        var responded = applications.Count(a => a.Status == "Responded");
        var rejected = applications.Count(a => a.Status == "Rejected");

        // Mock logic for interview invitations if not explicitly tracked yet or use status "Interview"
        // Assuming "Responded" might include interviews or separate status
        var interviews = applications.Count(a => a.Status == "Interview"); 

        var matchingJobs = await _context.JobMatches
            .CountAsync(m => m.UserId == userId);
            
        decimal responseRate = sent > 0 ? (decimal)responded / sent * 100 : 0;

        var stats = new DashboardStatsDto(
            TotalApplications: total,
            PendingApplications: pending,
            SentApplications: sent,
            ViewedApplications: viewed,
            RespondedApplications: responded,
            RejectedApplications: rejected,
            ResponseRate: Math.Round(responseRate, 1),
            InterviewInvitations: interviews,
            MatchingJobs: matchingJobs
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

        var now = DateTime.UtcNow;
        var sevenDaysAgo = now.AddDays(-7);
        var thirtyDaysAgo = now.AddDays(-30);

        // Weekly trends (last 7 days)
        var weeklyData = await _context.Applications
            .Where(a => a.UserId == userId && a.CreatedAt >= sevenDaysAgo)
            .GroupBy(a => a.CreatedAt.Date)
            .Select(g => new TrendDataPoint(g.Key, g.Count()))
            .OrderBy(x => x.Date)
            .ToListAsync();

        // Fill missing days
        var weeklyTrends = new List<TrendDataPoint>();
        for (int i = 0; i < 7; i++)
        {
            var date = sevenDaysAgo.AddDays(i).Date;
            var point = weeklyData.FirstOrDefault(x => x.Date == date);
            weeklyTrends.Add(new TrendDataPoint(date, point?.Count ?? 0));
        }

        // Monthly trends (last 30 days) - simplified to daily counts for the graph
        var monthlyData = await _context.Applications
            .Where(a => a.UserId == userId && a.CreatedAt >= thirtyDaysAgo)
            .GroupBy(a => a.CreatedAt.Date)
            .Select(g => new TrendDataPoint(g.Key, g.Count()))
            .OrderBy(x => x.Date)
            .ToListAsync();

        // Status breakdown
        var statusData = await _context.Applications
            .Where(a => a.UserId == userId)
            .GroupBy(a => a.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        var total = statusData.Sum(x => x.Count);
        var breakdown = statusData.Select(x => new StatusBreakdown(
            x.Status,
            x.Count,
            total > 0 ? Math.Round((decimal)x.Count / total * 100, 1) : 0
        )).ToList();

        var trends = new DashboardTrendsDto(
            WeeklyApplications: weeklyTrends,
            MonthlyApplications: monthlyData,
            StatusBreakdown: breakdown
        );

        return Ok(trends);
    }
}
