using DistroCv.Core.Entities;
using DistroCv.Core.Enums;
using DistroCv.Core.Interfaces;
using DistroCv.Infrastructure.Data;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DistroCv.Infrastructure.Services;

/// <summary>
/// Hangfire job that processes a single email from the queue.
/// Handles business-hours enforcement, Gmail API delivery,
/// status management, and retry logic with exponential backoff.
///
/// Hangfire Configuration:
/// - AutomaticRetry with 3 attempts and exponential backoff
/// - Retry delays: ~25s, ~2min, ~16min (Hangfire default exponential)
///
/// Layer: Infrastructure/Services
/// </summary>
[AutomaticRetry(Attempts = 3, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
public class EmailDeliveryJob : IEmailDeliveryJob
{
    private readonly DistroCvDbContext _context;
    private readonly IGmailDeliveryService _gmailDeliveryService;
    private readonly ILogger<EmailDeliveryJob> _logger;

    private static readonly TimeOnly BusinessHoursStart = new(9, 0);
    private static readonly TimeOnly BusinessHoursEnd = new(17, 0);
    private const int DailyEmailLimit = 40;

    public EmailDeliveryJob(
        DistroCvDbContext context,
        IGmailDeliveryService gmailDeliveryService,
        ILogger<EmailDeliveryJob> logger)
    {
        _context = context;
        _gmailDeliveryService = gmailDeliveryService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task ExecuteAsync(Guid emailJobId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing email job {EmailJobId}", emailJobId);

        var emailJob = await _context.EmailJobs
            .Include(e => e.User)
            .Include(e => e.JobPosting)
            .FirstOrDefaultAsync(e => e.Id == emailJobId, cancellationToken);

        if (emailJob == null)
        {
            _logger.LogError("Email job {EmailJobId} not found in database", emailJobId);
            return;
        }

        // ── Guard: already processed or cancelled ──────────────
        if (emailJob.Status == EmailJobStatus.Sent || emailJob.Status == EmailJobStatus.Cancelled)
        {
            _logger.LogInformation(
                "Email job {EmailJobId} already has status {Status}, skipping",
                emailJobId, emailJob.Status);
            return;
        }

        // ── Guard: business hours check ────────────────────────
        if (!IsWithinBusinessHours())
        {
            _logger.LogWarning(
                "Email job {EmailJobId} fired outside business hours. Re-scheduling to next business window.",
                emailJobId);
            RescheduleToNextBusinessHour(emailJob);
            await _context.SaveChangesAsync(cancellationToken);
            return;
        }

        // ── Guard: daily limit re-check ────────────────────────
        var dailyCount = await GetDailySentCountAsync(emailJob.UserId, cancellationToken);
        if (dailyCount >= DailyEmailLimit)
        {
            _logger.LogWarning(
                "User {UserId} has reached daily limit ({Limit}) at delivery time. Email job {EmailJobId} rescheduled to tomorrow.",
                emailJob.UserId, DailyEmailLimit, emailJobId);
            RescheduleToNextBusinessDay(emailJob);
            await _context.SaveChangesAsync(cancellationToken);
            return;
        }

        // ── Mark as Processing ─────────────────────────────────
        emailJob.Status = EmailJobStatus.Processing;
        emailJob.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        try
        {
            // ── Send via Gmail API ─────────────────────────────
            var deliveryResult = await _gmailDeliveryService.SendEmailAsync(
                new GmailDeliveryRequest
                {
                    UserId = emailJob.UserId,
                    SenderEmail = emailJob.User.Email,
                    SenderName = emailJob.User.FullName,
                    RecipientEmail = emailJob.RecipientEmail,
                    RecipientName = emailJob.RecipientName,
                    Subject = emailJob.Subject,
                    PlainTextBody = emailJob.Body
                },
                cancellationToken);

            if (deliveryResult.IsSuccess)
            {
                // ── Success ────────────────────────────────────
                emailJob.Status = EmailJobStatus.Sent;
                emailJob.SentAtUtc = DateTime.UtcNow;
                emailJob.GmailMessageId = deliveryResult.GmailMessageId;
                emailJob.UpdatedAtUtc = DateTime.UtcNow;

                await _context.SaveChangesAsync(cancellationToken);

                // Update the related Application status if exists
                if (emailJob.ApplicationId.HasValue)
                {
                    await UpdateApplicationStatusAsync(emailJob.ApplicationId.Value, cancellationToken);
                }

                _logger.LogInformation(
                    "Email job {EmailJobId} sent successfully. Gmail ID: {GmailMessageId}",
                    emailJobId, deliveryResult.GmailMessageId);
            }
            else
            {
                HandleDeliveryFailure(emailJob, deliveryResult.ErrorMessage ?? "Unknown error",
                    deliveryResult.IsRetryable);
                await _context.SaveChangesAsync(cancellationToken);

                if (deliveryResult.IsRetryable)
                {
                    throw new GmailDeliveryException(
                        deliveryResult.ErrorMessage ?? "Retryable Gmail delivery failure",
                        isRetryable: true,
                        isRateLimited: deliveryResult.IsRateLimited,
                        isTokenError: deliveryResult.IsTokenError);
                }
            }
        }
        catch (GmailDeliveryException ex)
        {
            // Let Hangfire's retry mechanism handle retryable errors
            emailJob.RetryCount++;
            emailJob.LastError = ex.Message;
            emailJob.UpdatedAtUtc = DateTime.UtcNow;

            if (emailJob.RetryCount >= emailJob.MaxRetries)
            {
                emailJob.Status = EmailJobStatus.Failed;
                _logger.LogError(
                    "Email job {EmailJobId} failed permanently after {RetryCount} attempts: {Error}",
                    emailJobId, emailJob.RetryCount, ex.Message);
            }
            else
            {
                emailJob.Status = EmailJobStatus.Scheduled; // Back to scheduled for retry
                if (ex.IsRateLimited)
                {
                    _logger.LogWarning(
                        "Email job {EmailJobId} rate-limited (429). Retry {Retry}/{Max}. Error: {Error}",
                        emailJobId, emailJob.RetryCount, emailJob.MaxRetries, ex.Message);
                }
                else if (ex.IsTokenError)
                {
                    _logger.LogWarning(
                        "Email job {EmailJobId} token error. Retry {Retry}/{Max}. Error: {Error}",
                        emailJobId, emailJob.RetryCount, emailJob.MaxRetries, ex.Message);
                }
                else
                {
                    _logger.LogWarning(
                        "Email job {EmailJobId} delivery failed. Retry {Retry}/{Max}. Error: {Error}",
                        emailJobId, emailJob.RetryCount, emailJob.MaxRetries, ex.Message);
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            // Re-throw for Hangfire retry (only if retryable and under max)
            if (ex.IsRetryable && emailJob.RetryCount < emailJob.MaxRetries)
            {
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing email job {EmailJobId}", emailJobId);

            emailJob.RetryCount++;
            emailJob.LastError = ex.Message;
            emailJob.UpdatedAtUtc = DateTime.UtcNow;

            if (emailJob.RetryCount >= emailJob.MaxRetries)
            {
                emailJob.Status = EmailJobStatus.Failed;
                _logger.LogError(
                    "Email job {EmailJobId} failed permanently after {RetryCount} attempts",
                    emailJobId, emailJob.RetryCount);
            }
            else
            {
                emailJob.Status = EmailJobStatus.Scheduled;
            }

            await _context.SaveChangesAsync(cancellationToken);
            throw; // Re-throw for Hangfire retry
        }
    }

    // ── Private Helpers ────────────────────────────────────────

    private static bool IsWithinBusinessHours()
    {
        var turkeyTz = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
        var nowTurkey = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, turkeyTz);
        var time = TimeOnly.FromDateTime(nowTurkey);

        // Monday = 1 ... Friday = 5
        var isWeekday = nowTurkey.DayOfWeek >= DayOfWeek.Monday
                     && nowTurkey.DayOfWeek <= DayOfWeek.Friday;

        return isWeekday && time >= BusinessHoursStart && time < BusinessHoursEnd;
    }

    private void RescheduleToNextBusinessHour(EmailJob emailJob)
    {
        var turkeyTz = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
        var nowTurkey = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, turkeyTz);

        DateTime nextSlot;
        var time = TimeOnly.FromDateTime(nowTurkey);

        if (time >= BusinessHoursEnd || nowTurkey.DayOfWeek == DayOfWeek.Saturday || nowTurkey.DayOfWeek == DayOfWeek.Sunday)
        {
            // Move to next business day 09:00 + small random offset
            nextSlot = GetNextBusinessDayStart(nowTurkey);
        }
        else if (time < BusinessHoursStart)
        {
            // Same day, start of business hours + small random offset
            var date = DateOnly.FromDateTime(nowTurkey);
            nextSlot = date.ToDateTime(BusinessHoursStart);
        }
        else
        {
            // Shouldn't reach here, but fallback to next day
            nextSlot = GetNextBusinessDayStart(nowTurkey);
        }

        // Add small random jitter (1-30 min into business hours)
        var jitter = Random.Shared.Next(1, 31);
        nextSlot = nextSlot.AddMinutes(jitter);

        var nextSlotUtc = TimeZoneInfo.ConvertTimeToUtc(nextSlot, turkeyTz);

        emailJob.ScheduledAtUtc = nextSlotUtc;
        emailJob.Status = EmailJobStatus.Scheduled;
        emailJob.UpdatedAtUtc = DateTime.UtcNow;

        // Reschedule in Hangfire
        var delay = nextSlotUtc - DateTime.UtcNow;
        if (delay < TimeSpan.Zero) delay = TimeSpan.FromMinutes(1);

        var newHangfireJobId = BackgroundJob.Schedule<IEmailDeliveryJob>(
            job => job.ExecuteAsync(emailJob.Id, CancellationToken.None),
            delay);

        emailJob.HangfireJobId = newHangfireJobId;

        _logger.LogInformation(
            "Email job {EmailJobId} rescheduled to {NextSlot} (Hangfire: {HangfireId})",
            emailJob.Id, nextSlotUtc, newHangfireJobId);
    }

    private void RescheduleToNextBusinessDay(EmailJob emailJob)
    {
        var turkeyTz = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
        var nowTurkey = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, turkeyTz);

        var nextSlot = GetNextBusinessDayStart(nowTurkey);
        var jitter = Random.Shared.Next(1, 31);
        nextSlot = nextSlot.AddMinutes(jitter);

        var nextSlotUtc = TimeZoneInfo.ConvertTimeToUtc(nextSlot, turkeyTz);

        emailJob.ScheduledAtUtc = nextSlotUtc;
        emailJob.Status = EmailJobStatus.Scheduled;
        emailJob.UpdatedAtUtc = DateTime.UtcNow;

        var delay = nextSlotUtc - DateTime.UtcNow;
        if (delay < TimeSpan.Zero) delay = TimeSpan.FromMinutes(1);

        var newHangfireJobId = BackgroundJob.Schedule<IEmailDeliveryJob>(
            job => job.ExecuteAsync(emailJob.Id, CancellationToken.None),
            delay);

        emailJob.HangfireJobId = newHangfireJobId;
    }

    private static DateTime GetNextBusinessDayStart(DateTime current)
    {
        var next = current.Date.AddDays(1);
        while (next.DayOfWeek == DayOfWeek.Saturday || next.DayOfWeek == DayOfWeek.Sunday)
        {
            next = next.AddDays(1);
        }
        return next.Date.Add(BusinessHoursStart.ToTimeSpan());
    }

    private async Task<int> GetDailySentCountAsync(Guid userId, CancellationToken cancellationToken)
    {
        var todayUtc = DateTime.UtcNow.Date;
        var tomorrowUtc = todayUtc.AddDays(1);

        return await _context.EmailJobs
            .Where(e => e.UserId == userId
                && e.Status == EmailJobStatus.Sent
                && e.SentAtUtc >= todayUtc
                && e.SentAtUtc < tomorrowUtc)
            .CountAsync(cancellationToken);
    }

    private async Task UpdateApplicationStatusAsync(Guid applicationId, CancellationToken cancellationToken)
    {
        var application = await _context.Applications
            .FirstOrDefaultAsync(a => a.Id == applicationId, cancellationToken);

        if (application != null)
        {
            application.Status = "Sent";
            application.SentAt = DateTime.UtcNow;

            var log = new ApplicationLog
            {
                Id = Guid.NewGuid(),
                ApplicationId = applicationId,
                ActionType = "EmailSent",
                Details = "Email sent via Smart Email Automation Engine (Gmail API)",
                Timestamp = DateTime.UtcNow
            };

            _context.ApplicationLogs.Add(log);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    private void HandleDeliveryFailure(EmailJob emailJob, string error, bool isRetryable)
    {
        emailJob.RetryCount++;
        emailJob.LastError = error;
        emailJob.UpdatedAtUtc = DateTime.UtcNow;

        if (!isRetryable || emailJob.RetryCount >= emailJob.MaxRetries)
        {
            emailJob.Status = EmailJobStatus.Failed;
        }
        else
        {
            emailJob.Status = EmailJobStatus.Scheduled;
        }
    }
}
