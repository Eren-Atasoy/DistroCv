using DistroCv.Core.Interfaces;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace DistroCv.Api.BackgroundServices;

/// <summary>
/// Background service for scheduling recurring job scraping tasks
/// </summary>
public class JobScrapingBackgroundService : IHostedService
{
    private readonly ILogger<JobScrapingBackgroundService> _logger;
    private readonly IRecurringJobManager _recurringJobManager;

    public JobScrapingBackgroundService(
        ILogger<JobScrapingBackgroundService> logger,
        IRecurringJobManager recurringJobManager)
    {
        _logger = logger;
        _recurringJobManager = recurringJobManager;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Job Scraping Background Service is starting");

        // Schedule daily job scraping at 2 AM
        _recurringJobManager.AddOrUpdate(
            "scrape-linkedin-jobs",
            (IJobScrapingService service) => service.ScrapeLinkedInJobsAsync(CancellationToken.None),
            "0 2 * * *", // Cron expression: Every day at 2 AM
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Local
            });

        // Schedule daily Indeed scraping at 3 AM
        _recurringJobManager.AddOrUpdate(
            "scrape-indeed-jobs",
            (IJobScrapingService service) => service.ScrapeIndeedJobsAsync(CancellationToken.None),
            "0 3 * * *", // Cron expression: Every day at 3 AM
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Local
            });

        _logger.LogInformation("Job scraping tasks scheduled successfully");

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Job Scraping Background Service is stopping");
        return Task.CompletedTask;
    }
}
