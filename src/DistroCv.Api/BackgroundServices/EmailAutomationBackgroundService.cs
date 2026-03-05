using DistroCv.Core.Enums;
using DistroCv.Core.Interfaces;
using DistroCv.Infrastructure.Data;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DistroCv.Api.BackgroundServices;

/// <summary>
/// Background service that registers Hangfire recurring jobs for the
/// Smart Email Automation Engine:
/// - Periodically scans for stuck/orphaned email jobs
/// - Reschedules any jobs that missed their window (e.g., after a server restart)
///
/// This is NOT the main scheduler — individual emails are scheduled on-demand
/// by IEmailQueueService.EnqueueEmailAsync(). This service handles edge cases.
/// </summary>
public class EmailAutomationBackgroundService : IHostedService
{
    private readonly ILogger<EmailAutomationBackgroundService> _logger;
    private readonly IRecurringJobManager _recurringJobManager;

    public EmailAutomationBackgroundService(
        ILogger<EmailAutomationBackgroundService> logger,
        IRecurringJobManager recurringJobManager)
    {
        _logger = logger;
        _recurringJobManager = recurringJobManager;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Email Automation Background Service is starting");

        // ── Recurring Job: Rescue stuck email jobs ─────────────
        // Runs every 30 minutes during business hours (Mon-Fri)
        // Picks up any jobs stuck in "Processing" state for > 10 minutes
        // or "Scheduled" jobs whose scheduled time has passed
        _recurringJobManager.AddOrUpdate<IEmailJobRescueTask>(
            "rescue-stuck-email-jobs",
            task => task.ExecuteAsync(CancellationToken.None),
            "*/30 9-17 * * 1-5", // Every 30 min, 09-17, Mon-Fri
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul")
            });

        _logger.LogInformation("Email automation recurring jobs registered successfully");

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Email Automation Background Service is stopping");
        return Task.CompletedTask;
    }
}

/// <summary>
/// Hangfire task that rescues stuck/orphaned email jobs.
/// Registered as a recurring job by EmailAutomationBackgroundService.
/// </summary>
public interface IEmailJobRescueTask
{
    Task ExecuteAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Implementation of the email job rescue task.
/// Finds and re-schedules email jobs that got stuck due to:
/// - Server restarts during processing
/// - Hangfire job failures that weren't properly handled
/// - Scheduled jobs whose time has passed
/// </summary>
public class EmailJobRescueTask : IEmailJobRescueTask
{
    private readonly DistroCvDbContext _context;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ILogger<EmailJobRescueTask> _logger;

    private static readonly TimeSpan StuckThreshold = TimeSpan.FromMinutes(10);

    public EmailJobRescueTask(
        DistroCvDbContext context,
        IBackgroundJobClient backgroundJobClient,
        ILogger<EmailJobRescueTask> logger)
    {
        _context = context;
        _backgroundJobClient = backgroundJobClient;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Running email job rescue task");

        var stuckThreshold = DateTime.UtcNow.Subtract(StuckThreshold);

        // Find stuck "Processing" jobs (older than 10 minutes)
        var stuckJobs = await _context.EmailJobs
            .Where(e => e.Status == EmailJobStatus.Processing
                && e.UpdatedAtUtc < stuckThreshold)
            .ToListAsync(cancellationToken);

        // Find missed "Scheduled" jobs (scheduled time has passed by > 10 min)
        var missedJobs = await _context.EmailJobs
            .Where(e => e.Status == EmailJobStatus.Scheduled
                && e.ScheduledAtUtc.HasValue
                && e.ScheduledAtUtc.Value < stuckThreshold)
            .ToListAsync(cancellationToken);

        var totalRescued = 0;

        foreach (var job in stuckJobs.Concat(missedJobs))
        {
            if (job.RetryCount >= job.MaxRetries)
            {
                job.Status = EmailJobStatus.Failed;
                job.LastError = "Job stuck/missed and exceeded max retries";
                job.UpdatedAtUtc = DateTime.UtcNow;
                _logger.LogWarning(
                    "Email job {EmailJobId} marked as Failed (stuck, max retries exceeded)",
                    job.Id);
            }
            else
            {
                // Re-schedule with a small delay
                var delay = TimeSpan.FromMinutes(Random.Shared.Next(1, 5));
                var hangfireJobId = _backgroundJobClient.Schedule<IEmailDeliveryJob>(
                    j => j.ExecuteAsync(job.Id, CancellationToken.None),
                    delay);

                job.Status = EmailJobStatus.Scheduled;
                job.HangfireJobId = hangfireJobId;
                job.ScheduledAtUtc = DateTime.UtcNow.Add(delay);
                job.UpdatedAtUtc = DateTime.UtcNow;
                totalRescued++;

                _logger.LogInformation(
                    "Email job {EmailJobId} rescued and rescheduled. Hangfire: {HangfireId}",
                    job.Id, hangfireJobId);
            }
        }

        if (stuckJobs.Count > 0 || missedJobs.Count > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation(
            "Email job rescue task completed. Stuck: {StuckCount}, Missed: {MissedCount}, Rescued: {RescuedCount}",
            stuckJobs.Count, missedJobs.Count, totalRescued);
    }
}
