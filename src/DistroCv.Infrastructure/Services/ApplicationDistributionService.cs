using DistroCv.Core.Entities;
using DistroCv.Core.Interfaces;
using DistroCv.Infrastructure.Data;
using DistroCv.Infrastructure.Gmail;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text;

namespace DistroCv.Infrastructure.Services;

/// <summary>
/// Service for distributing job applications via email and LinkedIn
/// </summary>
public class ApplicationDistributionService : IApplicationDistributionService
{
    private readonly DistroCvDbContext _context;
    private readonly IGeminiService _geminiService;
    private readonly IGmailService _gmailService;
    private readonly ILogger<ApplicationDistributionService> _logger;

    public ApplicationDistributionService(
        DistroCvDbContext context,
        IGeminiService geminiService,
        IGmailService gmailService,
        ILogger<ApplicationDistributionService> logger)
    {
        _context = context;
        _geminiService = geminiService;
        _gmailService = gmailService;
        _logger = logger;
    }

    /// <summary>
    /// Sends application via Gmail API (Validates: Requirement 5.1, 5.2)
    /// </summary>
    public async Task<bool> SendViaEmailAsync(Guid applicationId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Sending application {ApplicationId} via email", applicationId);

        try
        {
            // Get application with related data
            var application = await _context.Applications
                .Include(a => a.JobMatch)
                    .ThenInclude(jm => jm.JobPosting)
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Id == applicationId, cancellationToken);

            if (application == null)
            {
                throw new InvalidOperationException($"Application not found: {applicationId}");
            }

            // Generate personalized email
            var emailContent = await GeneratePersonalizedEmailAsync(applicationId, cancellationToken);

            // Send email via Gmail API
            var messageId = await _gmailService.SendEmailAsync(
                userEmail: application.User.Email,
                recipientEmail: emailContent.RecipientEmail,
                subject: emailContent.Subject,
                body: emailContent.Body,
                attachments: null, // TODO: Add resume and cover letter attachments
                cancellationToken: cancellationToken);

            // Log successful send
            await LogApplicationActionAsync(
                applicationId,
                "EmailSent",
                $"Email sent successfully. Message ID: {messageId}",
                cancellationToken);

            // Update application status
            await UpdateApplicationStatusAsync(
                applicationId, 
                "Sent", 
                $"Sent via email. Message ID: {messageId}", 
                cancellationToken);

            _logger.LogInformation("Successfully sent application {ApplicationId} via email", applicationId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending application {ApplicationId} via email", applicationId);
            
            // Log error to ApplicationLog
            await LogApplicationErrorAsync(applicationId, "Email sending failed", ex.Message, cancellationToken);
            
            throw;
        }
    }

    /// <summary>
    /// Sends application via LinkedIn Easy Apply (Validates: Requirement 5.3)
    /// </summary>
    public async Task<bool> SendViaLinkedInAsync(Guid applicationId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Sending application {ApplicationId} via LinkedIn", applicationId);

        try
        {
            // Get application with related data
            var application = await _context.Applications
                .Include(a => a.JobMatch)
                    .ThenInclude(jm => jm.JobPosting)
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Id == applicationId, cancellationToken);

            if (application == null)
            {
                throw new InvalidOperationException($"Application not found: {applicationId}");
            }

            // TODO: Implement LinkedIn automation with Playwright
            // This will be implemented in task 8.4
            _logger.LogWarning("LinkedIn automation not yet implemented");

            // Update application status
            await UpdateApplicationStatusAsync(
                applicationId, 
                "Sent", 
                "Sent via LinkedIn Easy Apply", 
                cancellationToken);

            _logger.LogInformation("Successfully sent application {ApplicationId} via LinkedIn", applicationId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending application {ApplicationId} via LinkedIn", applicationId);
            
            // Log error to ApplicationLog
            await LogApplicationErrorAsync(applicationId, "LinkedIn sending failed", ex.Message, cancellationToken);
            
            throw;
        }
    }

    /// <summary>
    /// Tracks application status (Validates: Requirement 5.6)
    /// </summary>
    public async Task UpdateApplicationStatusAsync(
        Guid applicationId, 
        string status, 
        string? notes = null, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating application {ApplicationId} status to {Status}", applicationId, status);

        try
        {
            var application = await _context.Applications
                .FirstOrDefaultAsync(a => a.Id == applicationId, cancellationToken);

            if (application == null)
            {
                throw new InvalidOperationException($"Application not found: {applicationId}");
            }

            // Update status
            application.Status = status;

            if (status == "Sent")
            {
                application.SentAt = DateTime.UtcNow;
            }

            // Create application log entry
            var log = new ApplicationLog
            {
                Id = Guid.NewGuid(),
                ApplicationId = applicationId,
                ActionType = $"StatusChange",
                Details = $"Status changed to {status}. {notes ?? string.Empty}",
                Timestamp = DateTime.UtcNow
            };

            _context.ApplicationLogs.Add(log);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully updated application {ApplicationId} status", applicationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating application {ApplicationId} status", applicationId);
            throw;
        }
    }

    /// <summary>
    /// Generates personalized email message for HR contact (Validates: Requirement 5.2)
    /// </summary>
    public async Task<EmailContent> GeneratePersonalizedEmailAsync(
        Guid applicationId, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating personalized email for application {ApplicationId}", applicationId);

        try
        {
            // Get application with related data
            var application = await _context.Applications
                .Include(a => a.JobMatch)
                    .ThenInclude(jm => jm.JobPosting)
                .Include(a => a.User)
                    .ThenInclude(u => u.DigitalTwin)
                .FirstOrDefaultAsync(a => a.Id == applicationId, cancellationToken);

            if (application == null)
            {
                throw new InvalidOperationException($"Application not found: {applicationId}");
            }

            var jobPosting = application.JobMatch?.JobPosting;
            var digitalTwin = application.User?.DigitalTwin;

            if (jobPosting == null || digitalTwin == null)
            {
                throw new InvalidOperationException("Job posting or digital twin not found");
            }

            // Build prompt for Gemini
            var prompt = BuildEmailPrompt(digitalTwin, jobPosting, application);

            // Generate email using Gemini
            var emailText = await _geminiService.GenerateContentAsync(prompt);

            // Parse email (assuming format: Subject: ...\n\nBody: ...)
            var emailContent = ParseEmailContent(emailText, jobPosting);

            _logger.LogInformation("Successfully generated personalized email for application {ApplicationId}", applicationId);
            return emailContent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating personalized email for application {ApplicationId}", applicationId);
            throw;
        }
    }

    /// <summary>
    /// Builds prompt for email generation
    /// </summary>
    private string BuildEmailPrompt(DigitalTwin digitalTwin, JobPosting jobPosting, Application application)
    {
        return $@"You are an expert email writer. Create a professional, personalized email to the HR department for a job application.

Candidate Profile:
- Name: {digitalTwin.UserId} (use placeholder [Your Name])
- Skills: {digitalTwin.Skills}
- Experience: {digitalTwin.Experience}
- Career Goals: {digitalTwin.CareerGoals}

Job Posting:
- Title: {jobPosting.Title}
- Company: {jobPosting.CompanyName}
- Location: {jobPosting.Location}
- Description: {jobPosting.Description}

Create a professional email that:
1. Has a clear, professional subject line
2. Opens with a polite greeting
3. Expresses genuine interest in the position
4. Highlights 2-3 relevant qualifications/experiences
5. Mentions the attached resume and cover letter
6. Closes with a call to action and professional sign-off

Keep it concise (150-250 words) and professional.

Return the response in the following format:
Subject: [subject line]

[email body]

Do not include any additional formatting or explanations.";
    }

    /// <summary>
    /// Parses email content from Gemini response
    /// </summary>
    private EmailContent ParseEmailContent(string emailText, JobPosting jobPosting)
    {
        var lines = emailText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var subject = string.Empty;
        var bodyBuilder = new StringBuilder();
        var foundSubject = false;

        foreach (var line in lines)
        {
            if (line.StartsWith("Subject:", StringComparison.OrdinalIgnoreCase))
            {
                subject = line.Substring("Subject:".Length).Trim();
                foundSubject = true;
            }
            else if (foundSubject)
            {
                bodyBuilder.AppendLine(line);
            }
        }

        // If no subject found, use default
        if (string.IsNullOrEmpty(subject))
        {
            subject = $"Application for {jobPosting.Title} Position";
            bodyBuilder.Clear();
            bodyBuilder.Append(emailText);
        }

        return new EmailContent
        {
            Subject = subject,
            Body = bodyBuilder.ToString().Trim(),
            RecipientEmail = "hr@company.com", // TODO: Get from verified company database
            RecipientName = "Hiring Manager"
        };
    }

    /// <summary>
    /// Logs application error
    /// </summary>
    private async Task LogApplicationErrorAsync(
        Guid applicationId, 
        string action, 
        string details, 
        CancellationToken cancellationToken)
    {
        try
        {
            var log = new ApplicationLog
            {
                Id = Guid.NewGuid(),
                ApplicationId = applicationId,
                ActionType = "Error",
                Details = $"{action}: {details}",
                Timestamp = DateTime.UtcNow
            };

            _context.ApplicationLogs.Add(log);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging application error for {ApplicationId}", applicationId);
        }
    }

    /// <summary>
    /// Logs application action
    /// </summary>
    private async Task LogApplicationActionAsync(
        Guid applicationId,
        string actionType,
        string details,
        CancellationToken cancellationToken)
    {
        try
        {
            var log = new ApplicationLog
            {
                Id = Guid.NewGuid(),
                ApplicationId = applicationId,
                ActionType = actionType,
                Details = details,
                Timestamp = DateTime.UtcNow
            };

            _context.ApplicationLogs.Add(log);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging application action for {ApplicationId}", applicationId);
        }
    }
}
