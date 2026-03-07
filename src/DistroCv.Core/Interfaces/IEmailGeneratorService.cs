namespace DistroCv.Core.Interfaces;

/// <summary>
/// Generates personalized, spam-free, plain-text email content
/// using Gemini API with spintax variations.
/// Layer: Application (Core)
/// </summary>
public interface IEmailGeneratorService
{
    /// <summary>
    /// Generates a complete email (subject + body) personalized for a specific
    /// company/job posting using the candidate's CV analysis and a presigned CV URL.
    /// </summary>
    /// <param name="request">All data needed to generate the email</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Generated email content ready for queuing</returns>
    Task<GeneratedEmailContent> GenerateEmailAsync(
        EmailGenerationRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Input data for email generation
/// </summary>
public class EmailGenerationRequest
{
    /// <summary>Candidate's full name</summary>
    public string CandidateName { get; set; } = string.Empty;

    /// <summary>Structured CV analysis result</summary>
    public CvAnalysisResult CvAnalysis { get; set; } = new();

    /// <summary>Job posting title</summary>
    public string JobTitle { get; set; } = string.Empty;

    /// <summary>Company name</summary>
    public string CompanyName { get; set; } = string.Empty;

    /// <summary>Full job description</summary>
    public string JobDescription { get; set; } = string.Empty;

    /// <summary>Company culture/about info (if available from VerifiedCompany)</summary>
    public string? CompanyCulture { get; set; }

    /// <summary>HR contact name (if known)</summary>
    public string? HrContactName { get; set; }

    /// <summary>AWS S3 Presigned URL for the candidate's CV</summary>
    public string CvPresignedUrl { get; set; } = string.Empty;

    /// <summary>Target language (tr, en)</summary>
    public string Language { get; set; } = "tr";
}

/// <summary>
/// Output of the email generation process
/// </summary>
public class GeneratedEmailContent
{
    /// <summary>Email subject line</summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>Plain-text email body (with spintax resolved)</summary>
    public string Body { get; set; } = string.Empty;
}
