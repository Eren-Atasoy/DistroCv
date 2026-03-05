using DistroCv.Core.Interfaces;
using DistroCv.Infrastructure.AWS;
using DistroCv.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DistroCv.Infrastructure.Services;

/// <summary>
/// Main orchestrator for the Smart Email Automation Engine.
/// Coordinates the full pipeline: CV analysis → email generation → queue scheduling.
///
/// Pipeline:
/// 1. Load user's DigitalTwin (CV data from S3/pgvector) and job posting
/// 2. Analyze CV against job description via ICvAnalyzerService (Gemini)
/// 3. Generate presigned URL for the CV on S3
/// 4. Generate personalized email via IEmailGeneratorService (Gemini + spintax)
/// 5. Enqueue via IEmailQueueService (Hangfire scheduling with jitter)
///
/// Layer: Infrastructure/Services
/// </summary>
public class SmartEmailAutomationService : ISmartEmailAutomationService
{
    private readonly DistroCvDbContext _context;
    private readonly ICvAnalyzerService _cvAnalyzerService;
    private readonly IEmailGeneratorService _emailGeneratorService;
    private readonly IEmailQueueService _emailQueueService;
    private readonly IGmailDeliveryService _gmailDeliveryService;
    private readonly IS3Service _s3Service;
    private readonly ILogger<SmartEmailAutomationService> _logger;

    // Presigned URL valid for 7 days (enough for HR to review)
    private const int CvPresignedUrlExpirationMinutes = 7 * 24 * 60;

    public SmartEmailAutomationService(
        DistroCvDbContext context,
        ICvAnalyzerService cvAnalyzerService,
        IEmailGeneratorService emailGeneratorService,
        IEmailQueueService emailQueueService,
        IGmailDeliveryService gmailDeliveryService,
        IS3Service s3Service,
        ILogger<SmartEmailAutomationService> logger)
    {
        _context = context;
        _cvAnalyzerService = cvAnalyzerService;
        _emailGeneratorService = emailGeneratorService;
        _emailQueueService = emailQueueService;
        _gmailDeliveryService = gmailDeliveryService;
        _s3Service = s3Service;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<SmartEmailResult> ProcessAsync(
        SmartEmailRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Starting Smart Email Automation for user {UserId}, job posting {JobPostingId}",
            request.UserId, request.JobPostingId);

        try
        {
            // ── Step 0: Validate prerequisites ─────────────────
            var hasCredentials = await _gmailDeliveryService.HasValidCredentialsAsync(
                request.UserId, cancellationToken);
            if (!hasCredentials)
            {
                return new SmartEmailResult
                {
                    IsSuccess = false,
                    JobPostingId = request.JobPostingId,
                    ErrorMessage = "Gmail OAuth2 yetkilendirmesi gerekli. Lütfen önce Gmail hesabınızı bağlayın."
                };
            }

            var canSend = await _emailQueueService.CanSendMoreTodayAsync(
                request.UserId, cancellationToken);
            if (!canSend)
            {
                return new SmartEmailResult
                {
                    IsSuccess = false,
                    JobPostingId = request.JobPostingId,
                    ErrorMessage = "Günlük e-posta gönderim limitine ulaştınız (40/gün)."
                };
            }

            // ── Step 1: Load user data and job posting ─────────
            var user = await _context.Users
                .Include(u => u.DigitalTwin)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

            if (user == null)
                throw new InvalidOperationException($"User {request.UserId} not found");

            if (user.DigitalTwin == null)
                throw new InvalidOperationException($"User {request.UserId} has no DigitalTwin (CV not uploaded)");

            var jobPosting = await _context.JobPostings
                .Include(j => j.VerifiedCompany)
                .AsNoTracking()
                .FirstOrDefaultAsync(j => j.Id == request.JobPostingId, cancellationToken);

            if (jobPosting == null)
                throw new InvalidOperationException($"Job posting {request.JobPostingId} not found");

            // ── Step 2: Determine recipient ────────────────────
            var recipientEmail = jobPosting.VerifiedCompany?.HREmail;
            var recipientName = "İnsan Kaynakları";

            if (string.IsNullOrWhiteSpace(recipientEmail))
            {
                return new SmartEmailResult
                {
                    IsSuccess = false,
                    JobPostingId = request.JobPostingId,
                    ErrorMessage = "Bu ilan için İK e-posta adresi bulunamadı."
                };
            }

            // ── Step 3: Analyze CV against job ─────────────────
            var cvText = user.DigitalTwin.ParsedResumeJson ?? string.Empty;
            var skillsContext = user.DigitalTwin.Skills ?? string.Empty;
            var fullCvContext = $"{cvText}\n\nSkills: {skillsContext}\nExperience: {user.DigitalTwin.Experience}";

            var cvAnalysis = await _cvAnalyzerService.AnalyzeCvForJobAsync(
                fullCvContext,
                jobPosting.Description,
                user.PreferredLanguage,
                cancellationToken);

            // Use name from CV analysis or user profile
            if (string.IsNullOrWhiteSpace(cvAnalysis.CandidateName))
                cvAnalysis.CandidateName = user.FullName;

            // ── Step 4: Generate CV presigned URL ──────────────
            string? cvPresignedUrl = null;
            if (!string.IsNullOrWhiteSpace(user.DigitalTwin.OriginalResumeUrl))
            {
                cvPresignedUrl = await _s3Service.GetPresignedUrlAsync(
                    user.DigitalTwin.OriginalResumeUrl,
                    CvPresignedUrlExpirationMinutes);
            }

            // ── Step 5: Generate email content ─────────────────
            var emailContent = await _emailGeneratorService.GenerateEmailAsync(
                new EmailGenerationRequest
                {
                    CandidateName = cvAnalysis.CandidateName,
                    CvAnalysis = cvAnalysis,
                    JobTitle = jobPosting.Title,
                    CompanyName = jobPosting.CompanyName,
                    JobDescription = jobPosting.Description,
                    CompanyCulture = jobPosting.VerifiedCompany?.CompanyCulture,
                    HrContactName = null, // Could be extracted from VerifiedCompany
                    CvPresignedUrl = cvPresignedUrl ?? string.Empty,
                    Language = user.PreferredLanguage
                },
                cancellationToken);

            // ── Step 6: Enqueue with jitter ────────────────────
            var emailJob = await _emailQueueService.EnqueueEmailAsync(
                new EnqueueEmailRequest
                {
                    UserId = request.UserId,
                    JobPostingId = request.JobPostingId,
                    ApplicationId = request.ApplicationId,
                    RecipientEmail = recipientEmail,
                    RecipientName = recipientName,
                    Subject = emailContent.Subject,
                    Body = emailContent.Body,
                    CvPresignedUrl = cvPresignedUrl
                },
                cancellationToken);

            _logger.LogInformation(
                "Smart Email Automation completed for user {UserId} → {CompanyName}. EmailJob: {EmailJobId}, Scheduled: {ScheduledAt}",
                request.UserId, jobPosting.CompanyName, emailJob.Id, emailJob.ScheduledAtUtc);

            return new SmartEmailResult
            {
                IsSuccess = true,
                EmailJobId = emailJob.Id,
                JobPostingId = request.JobPostingId,
                ScheduledAtUtc = emailJob.ScheduledAtUtc
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Smart Email Automation failed for user {UserId}, job posting {JobPostingId}",
                request.UserId, request.JobPostingId);

            return new SmartEmailResult
            {
                IsSuccess = false,
                JobPostingId = request.JobPostingId,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <inheritdoc />
    public async Task<List<SmartEmailResult>> ProcessBatchAsync(
        Guid userId,
        List<Guid> jobPostingIds,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Starting batch Smart Email Automation for user {UserId} with {Count} job postings",
            userId, jobPostingIds.Count);

        var results = new List<SmartEmailResult>();

        foreach (var jobPostingId in jobPostingIds)
        {
            // Check daily limit before each iteration
            var canSend = await _emailQueueService.CanSendMoreTodayAsync(userId, cancellationToken);
            if (!canSend)
            {
                _logger.LogWarning(
                    "Daily limit reached for user {UserId}. Processed {Count}/{Total} job postings.",
                    userId, results.Count, jobPostingIds.Count);

                // Mark remaining as failed due to limit
                var remainingIds = jobPostingIds.Skip(results.Count);
                foreach (var remainingId in remainingIds)
                {
                    results.Add(new SmartEmailResult
                    {
                        IsSuccess = false,
                        JobPostingId = remainingId,
                        ErrorMessage = "Günlük e-posta gönderim limitine ulaşıldı."
                    });
                }
                break;
            }

            var result = await ProcessAsync(
                new SmartEmailRequest
                {
                    UserId = userId,
                    JobPostingId = jobPostingId
                },
                cancellationToken);

            results.Add(result);
        }

        var successCount = results.Count(r => r.IsSuccess);
        _logger.LogInformation(
            "Batch Smart Email Automation completed for user {UserId}: {Success}/{Total} succeeded",
            userId, successCount, jobPostingIds.Count);

        return results;
    }
}
