using DistroCv.Core.Interfaces;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DistroCv.Api.Controllers;

/// <summary>
/// Controller for managing job scraping operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class JobScrapingController : BaseApiController
{
    private readonly IJobScrapingService _jobScrapingService;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ILogger<JobScrapingController> _logger;

    public JobScrapingController(
        IJobScrapingService jobScrapingService,
        IBackgroundJobClient backgroundJobClient,
        ILogger<JobScrapingController> logger)
    {
        _jobScrapingService = jobScrapingService;
        _backgroundJobClient = backgroundJobClient;
        _logger = logger;
    }

    /// <summary>
    /// Manually trigger LinkedIn job scraping
    /// </summary>
    [HttpPost("scrape/linkedin")]
    public IActionResult TriggerLinkedInScraping()
    {
        try
        {
            var jobId = _backgroundJobClient.Enqueue<IJobScrapingService>(
                service => service.ScrapeLinkedInJobsAsync(CancellationToken.None));

            _logger.LogInformation("LinkedIn scraping job enqueued with ID: {JobId}", jobId);

            return Ok(new
            {
                Message = "LinkedIn scraping job has been queued",
                JobId = jobId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering LinkedIn scraping");
            return StatusCode(500, new { Error = "Failed to trigger LinkedIn scraping" });
        }
    }

    /// <summary>
    /// Manually trigger Indeed job scraping
    /// </summary>
    [HttpPost("scrape/indeed")]
    public IActionResult TriggerIndeedScraping()
    {
        try
        {
            var jobId = _backgroundJobClient.Enqueue<IJobScrapingService>(
                service => service.ScrapeIndeedJobsAsync(CancellationToken.None));

            _logger.LogInformation("Indeed scraping job enqueued with ID: {JobId}", jobId);

            return Ok(new
            {
                Message = "Indeed scraping job has been queued",
                JobId = jobId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering Indeed scraping");
            return StatusCode(500, new { Error = "Failed to trigger Indeed scraping" });
        }
    }

    /// <summary>
    /// Get job scraping statistics
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetScrapingStats()
    {
        try
        {
            // This would typically query the database for statistics
            // For now, return a placeholder
            return Ok(new
            {
                Message = "Job scraping statistics",
                TotalJobsScraped = 0,
                LastScrapingTime = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving scraping stats");
            return StatusCode(500, new { Error = "Failed to retrieve scraping statistics" });
        }
    }
}
