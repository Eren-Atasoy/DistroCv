using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using DistroCv.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace DistroCv.Infrastructure.Services;

/// <summary>
/// Generates personalized, spam-free, plain-text application emails
/// using Google Gemini API. Includes spintax (variation) support for
/// greetings and closings to make each email unique.
/// Layer: Infrastructure/Services
/// </summary>
public partial class EmailGeneratorService : IEmailGeneratorService
{
    private readonly IGeminiService _geminiService;
    private readonly ILogger<EmailGeneratorService> _logger;
    private static readonly Random _random = new();

    // Spam words to filter out of generated content
    private static readonly HashSet<string> SpamWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "free", "guarantee", "no obligation", "winner", "congratulations",
        "act now", "limited time", "urgent", "click here", "buy now",
        "order now", "special promotion", "exclusive deal", "risk-free",
        "100%", "amazing", "incredible offer", "once in a lifetime",
        "bedava", "tebrikler", "kazandınız", "acele", "hemen", "fırsat",
        "kaçırmayın", "sınırlı süre", "garantili", "risksiz"
    };

    public EmailGeneratorService(
        IGeminiService geminiService,
        ILogger<EmailGeneratorService> logger)
    {
        _geminiService = geminiService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<GeneratedEmailContent> GenerateEmailAsync(
        EmailGenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Generating personalized email for {CandidateName} → {CompanyName} ({JobTitle})",
            request.CandidateName, request.CompanyName, request.JobTitle);

        try
        {
            var prompt = BuildEmailPrompt(request);
            var response = await _geminiService.GenerateContentAsync(prompt, request.Language);

            var emailContent = ParseEmailResponse(response);

            // Resolve spintax variations
            emailContent.Subject = ResolveSpintax(emailContent.Subject);
            emailContent.Body = ResolveSpintax(emailContent.Body);

            // Remove any spam words that slipped through
            emailContent.Body = RemoveSpamWords(emailContent.Body);

            // Append the CV presigned URL at the end
            emailContent.Body = AppendCvLink(emailContent.Body, request.CvPresignedUrl, request.Language);

            _logger.LogInformation("Email generated successfully for {CompanyName}", request.CompanyName);
            return emailContent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating email for {CompanyName}", request.CompanyName);
            throw;
        }
    }

    private static string BuildEmailPrompt(EmailGenerationRequest request)
    {
        var hrName = !string.IsNullOrWhiteSpace(request.HrContactName)
            ? request.HrContactName
            : "İnsan Kaynakları Yetkilisi";

        var companyContext = !string.IsNullOrWhiteSpace(request.CompanyCulture)
            ? $"\nCompany Culture/About:\n{request.CompanyCulture}"
            : "";

        var skillsList = string.Join(", ", request.CvAnalysis.RelevantSkills);
        var expList = string.Join("\n- ", request.CvAnalysis.RelevantExperience);

        return $@"You are an expert professional email writer. Write a personalized job application email.

=== CANDIDATE INFO ===
Name: {request.CandidateName}
Relevant Skills: {skillsList}
Relevant Experience:
- {expList}
Fit Summary: {request.CvAnalysis.FitSummary}
Years of Experience: {request.CvAnalysis.EstimatedYearsOfExperience}

=== TARGET JOB ===
Position: {request.JobTitle}
Company: {request.CompanyName}
HR Contact: {hrName}
{companyContext}

Job Description:
{request.JobDescription}

=== INSTRUCTIONS ===
Generate a professional plain-text job application email following these STRICT rules:

1. SUBJECT LINE: Create a clear, professional subject line specific to this position.

2. GREETING: Use spintax for variation. Example: {{Sayın {hrName}|Değerli {hrName}|{hrName}}} (pick one greeting style using spintax braces format).

3. BODY (150-200 words):
   - First paragraph: Express genuine interest in the SPECIFIC position and company. Reference something specific about the company if context is available.
   - Second paragraph: Highlight 2-3 most relevant qualifications that directly match the job requirements. Be specific with examples.
   - Third paragraph: Brief closing expressing enthusiasm and availability for an interview.

4. CLOSING: Use spintax for sign-off variation. Example: {{Saygılarımla|İlginiz için teşekkür ederim|En iyi dileklerimle}}

5. CRITICAL RULES:
   - Output MUST be plain-text only (NO HTML, NO markdown)
   - Do NOT use any spam trigger words (free, guarantee, urgent, act now, etc.)
   - Do NOT mention any attachments directly
   - Make the email sound natural and human-written, not template-like
   - Be specific to THIS company and THIS position — no generic content
   - Keep a professional but warm tone

Return the response in EXACTLY this format (no additional text, no markdown):
Subject: [subject line]

[email body including greeting and sign-off]";
    }

    private GeneratedEmailContent ParseEmailResponse(string response)
    {
        var lines = response.Split('\n');
        var subject = string.Empty;
        var bodyBuilder = new StringBuilder();
        var foundSubject = false;
        var bodyStarted = false;

        foreach (var line in lines)
        {
            if (!foundSubject && line.StartsWith("Subject:", StringComparison.OrdinalIgnoreCase))
            {
                subject = line["Subject:".Length..].Trim();
                foundSubject = true;
                continue;
            }

            if (foundSubject)
            {
                // Skip the first empty line after Subject
                if (!bodyStarted && string.IsNullOrWhiteSpace(line))
                {
                    bodyStarted = true;
                    continue;
                }
                bodyBuilder.AppendLine(line);
            }
        }

        // Fallback if no Subject: prefix found
        if (string.IsNullOrWhiteSpace(subject))
        {
            subject = "İş Başvurusu";
            bodyBuilder.Clear();
            bodyBuilder.Append(response);
        }

        return new GeneratedEmailContent
        {
            Subject = subject.Trim(),
            Body = bodyBuilder.ToString().Trim()
        };
    }

    /// <summary>
    /// Resolves spintax patterns like {option1|option2|option3} by randomly selecting one option.
    /// </summary>
    private static string ResolveSpintax(string text)
    {
        return SpintaxRegex().Replace(text, match =>
        {
            var options = match.Groups[1].Value.Split('|');
            return options[_random.Next(options.Length)].Trim();
        });
    }

    [GeneratedRegex(@"\{([^{}]+\|[^{}]+)\}", RegexOptions.Compiled)]
    private static partial Regex SpintaxRegex();

    /// <summary>
    /// Scans the email body and removes/replaces known spam trigger words
    /// </summary>
    private static string RemoveSpamWords(string text)
    {
        foreach (var word in SpamWords)
        {
            // Replace whole-word matches only (case-insensitive)
            var pattern = $@"\b{Regex.Escape(word)}\b";
            text = Regex.Replace(text, pattern, "", RegexOptions.IgnoreCase);
        }

        // Clean up any double spaces left behind
        text = Regex.Replace(text, @"  +", " ");
        return text.Trim();
    }

    /// <summary>
    /// Appends the CV presigned URL link at the end of the email body
    /// </summary>
    private static string AppendCvLink(string body, string cvPresignedUrl, string language)
    {
        if (string.IsNullOrWhiteSpace(cvPresignedUrl))
            return body;

        var linkText = language?.ToLower() switch
        {
            "en" => $"\n\nYou can review my CV here: {cvPresignedUrl}",
            _ => $"\n\nCV'mi incelemek için tıklayabilirsiniz: {cvPresignedUrl}"
        };

        return body + linkText;
    }
}
