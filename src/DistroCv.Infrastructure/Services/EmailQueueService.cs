using DistroCv.Core.Entities;
using DistroCv.Core.Enums;
using DistroCv.Core.Interfaces;
using DistroCv.Infrastructure.Data;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DistroCv.Infrastructure.Services;

/// <summary>
/// Manages the email job queue with intelligent scheduling:
/// - Saves EmailJob records to DB with Status = Pending
/// - Schedules delivery via Hangfire with random jitter (5-15 min)
/// - Enforces daily limit of 40 emails per user
/// - Restricts sending to business hours (Mon-Fri, 09:00-17:00)
/// Layer: Infrastructure/Services
/// </summary>
public class EmailQueueService : IEmailQueueService
{
    private readonly DistroCvDbContext _context;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ILogger<EmailQueueService> _logger;
    private static readonly Random _random = new();

    // ── Business Rules ─────────────────────────────────────────
    private const int DailyEmailLimit = 40;
    private const int MinJitterMinutes = 5;
    private const int MaxJitterMinutes = 15;
    private static readonly TimeOnly BusinessHoursStart = new(9, 0);
    private static readonly TimeOnly BusinessHoursEnd = new(17, 0);

    public EmailQueueService(
        DistroCvDbContext context,
        IBackgroundJobClient backgroundJobClient,
        ILogger<EmailQueueService> logger)
    {
        _context = context;
        _backgroundJobClient = backgroundJobClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<EmailJob> EnqueueEmailAsync(
        EnqueueEmailRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Enqueuing email for user {UserId} to {RecipientEmail}",
            request.UserId, request.RecipientEmail);

        // ── Guard: daily limit ─────────────────────────────────
        var dailyCount = await GetDailySendCountAsync(request.UserId, cancellationToken);
        if (dailyCount >= DailyEmailLimit)
        {
            _logger.LogWarning(
                "User {UserId} has reached daily email limit ({Limit}). Count: {Count}",
                request.UserId, DailyEmailLimit, dailyCount);
            throw new InvalidOperationException(
                $"Günlük e-posta gönderim limitine ({DailyEmailLimit}) ulaştınız. Lütfen yarın tekrar deneyin.");
        }

        // ── Create EmailJob record ─────────────────────────────
        var emailJob = new EmailJob
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            ApplicationId = request.ApplicationId,
            JobPostingId = request.JobPostingId,
            RecipientEmail = request.RecipientEmail,
            RecipientName = request.RecipientName,
            Subject = request.Subject,
            Body = request.Body,
            CvPresignedUrl = request.CvPresignedUrl,
            Status = EmailJobStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        _context.EmailJobs.Add(emailJob);
        await _context.SaveChangesAsync(cancellationToken);

        // ── Calculate scheduled time with jitter ───────────────
        var scheduledTime = CalculateScheduledTime(request.UserId, dailyCount);
        emailJob.ScheduledAtUtc = scheduledTime;
        emailJob.Status = EmailJobStatus.Scheduled;

        // ── Schedule via Hangfire ──────────────────────────────
        var delay = scheduledTime - DateTime.UtcNow;
        if (delay < TimeSpan.Zero) delay = TimeSpan.FromMinutes(1);

        var hangfireJobId = _backgroundJobClient.Schedule<IEmailDeliveryJob>(
            job => job.ExecuteAsync(emailJob.Id, CancellationToken.None),
            delay);

        emailJob.HangfireJobId = hangfireJobId;
        emailJob.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Email job {EmailJobId} scheduled for {ScheduledTime} (delay: {Delay}). Hangfire ID: {HangfireJobId}",
            emailJob.Id, scheduledTime, delay, hangfireJobId);

        return emailJob;
    }

    /// <inheritdoc />
    public async Task<int> GetDailySendCountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var todayUtc = DateTime.UtcNow.Date;
        var tomorrowUtc = todayUtc.AddDays(1);

        return await _context.EmailJobs
            .Where(e => e.UserId == userId
                && e.CreatedAtUtc >= todayUtc
                && e.CreatedAtUtc < tomorrowUtc
                && e.Status != EmailJobStatus.Cancelled)
            .CountAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> CanSendMoreTodayAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var count = await GetDailySendCountAsync(userId, cancellationToken);
        return count < DailyEmailLimit;
    }

    /// <inheritdoc />
    public async Task<List<EmailJob>> GetPendingJobsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.EmailJobs
            .Where(e => e.UserId == userId
                && (e.Status == EmailJobStatus.Pending || e.Status == EmailJobStatus.Scheduled))
            .OrderBy(e => e.ScheduledAtUtc)
            .Include(e => e.JobPosting)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> CancelEmailJobAsync(Guid emailJobId, Guid userId, CancellationToken cancellationToken = default)
    {
        var emailJob = await _context.EmailJobs
            .FirstOrDefaultAsync(e => e.Id == emailJobId && e.UserId == userId, cancellationToken);

        if (emailJob == null)
        {
            _logger.LogWarning("Email job {EmailJobId} not found for user {UserId}", emailJobId, userId);
            return false;
        }

        if (emailJob.Status != EmailJobStatus.Pending && emailJob.Status != EmailJobStatus.Scheduled)
        {
            _logger.LogWarning(
                "Cannot cancel email job {EmailJobId} with status {Status}",
                emailJobId, emailJob.Status);
            return false;
        }

        // Cancel Hangfire job if exists
        if (!string.IsNullOrEmpty(emailJob.HangfireJobId))
        {
            BackgroundJob.Delete(emailJob.HangfireJobId);
        }

        emailJob.Status = EmailJobStatus.Cancelled;
        emailJob.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Email job {EmailJobId} cancelled by user {UserId}", emailJobId, userId);
        return true;
    }

    /// <inheritdoc />
    public async Task<EmailQueueStats> GetQueueStatsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var todayUtc = DateTime.UtcNow.Date;
        var tomorrowUtc = todayUtc.AddDays(1);

        var todayJobs = await _context.EmailJobs
            .Where(e => e.UserId == userId
                && e.CreatedAtUtc >= todayUtc
                && e.CreatedAtUtc < tomorrowUtc)
            .ToListAsync(cancellationToken);

        var nextScheduled = await _context.EmailJobs
            .Where(e => e.UserId == userId && e.Status == EmailJobStatus.Scheduled)
            .OrderBy(e => e.ScheduledAtUtc)
            .Select(e => e.ScheduledAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        return new EmailQueueStats
        {
            TotalToday = todayJobs.Count(j => j.Status != EmailJobStatus.Cancelled),
            SentToday = todayJobs.Count(j => j.Status == EmailJobStatus.Sent),
            PendingCount = todayJobs.Count(j => j.Status == EmailJobStatus.Pending),
            ScheduledCount = todayJobs.Count(j => j.Status == EmailJobStatus.Scheduled),
            FailedToday = todayJobs.Count(j => j.Status == EmailJobStatus.Failed),
            DailyLimit = DailyEmailLimit,
            NextScheduledSendUtc = nextScheduled
        };
    }

    // ── Private Helpers ────────────────────────────────────────

    /// <summary>
    /// Calculates the next valid send time considering:
    /// 1. Random jitter (5-15 min between sends)
    /// 2. Business hours only (Mon-Fri, 09:00-17:00 Turkey time)
    /// </summary>
    private DateTime CalculateScheduledTime(Guid userId, int currentDailyCount)
    {
        // Turkey timezone (UTC+3)
        var turkeyTz = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
        var nowUtc = DateTime.UtcNow;
        var nowTurkey = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, turkeyTz);

        // Base: add jitter from the last scheduled email or from now
        var jitterMinutes = _random.Next(MinJitterMinutes, MaxJitterMinutes + 1);

        // Additional jitter based on position in queue (spread them out)
        var queueOffsetMinutes = currentDailyCount * _random.Next(MinJitterMinutes, MaxJitterMinutes + 1);

        var targetTurkey = nowTurkey.AddMinutes(jitterMinutes + queueOffsetMinutes);

        // Ensure it falls within business hours
        targetTurkey = AdjustToBusinessHours(targetTurkey);

        // Convert back to UTC
        return TimeZoneInfo.ConvertTimeToUtc(targetTurkey, turkeyTz);
    }

    /// <summary>
    /// Adjusts a datetime to fall within business hours (Mon-Fri, 09:00-17:00).
    /// If outside hours, moves to the next valid business-hours window.
    /// </summary>
    private static DateTime AdjustToBusinessHours(DateTime dateTime)
    {
        var time = TimeOnly.FromDateTime(dateTime);
        var date = DateOnly.FromDateTime(dateTime);

        // If weekend, move to Monday
        while (dateTime.DayOfWeek == DayOfWeek.Saturday || dateTime.DayOfWeek == DayOfWeek.Sunday)
        {
            dateTime = dateTime.AddDays(1);
            date = DateOnly.FromDateTime(dateTime);
        }

        time = TimeOnly.FromDateTime(dateTime);

        // If before business hours, set to start
        if (time < BusinessHoursStart)
        {
            return date.ToDateTime(BusinessHoursStart);
        }

        // If after business hours, move to next business day start
        if (time >= BusinessHoursEnd)
        {
            dateTime = dateTime.AddDays(1);
            // Skip weekends
            while (dateTime.DayOfWeek == DayOfWeek.Saturday || dateTime.DayOfWeek == DayOfWeek.Sunday)
            {
                dateTime = dateTime.AddDays(1);
            }
            date = DateOnly.FromDateTime(dateTime);
            return date.ToDateTime(BusinessHoursStart);
        }

        return dateTime;
    }
}
