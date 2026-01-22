using DistroCv.Core.Entities;

namespace DistroCv.Core.Interfaces;

/// <summary>
/// Service for tailoring resumes to specific job postings
/// </summary>
public interface IResumeTailoringService
{
    /// <summary>
    /// Generates a tailored resume for a specific job posting
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="jobPostingId">Job posting ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tailored resume content in HTML format</returns>
    Task<TailoredResumeResult> GenerateTailoredResumeAsync(
        Guid userId, 
        Guid jobPostingId, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Optimizes keywords in resume for ATS systems
    /// </summary>
    /// <param name="resumeContent">Original resume content</param>
    /// <param name="jobDescription">Job description</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Optimized resume content</returns>
    Task<string> OptimizeKeywordsAsync(
        string resumeContent, 
        string jobDescription, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Generates a cover letter for a specific job posting
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="jobPostingId">Job posting ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cover letter content</returns>
    Task<string> GenerateCoverLetterAsync(
        Guid userId, 
        Guid jobPostingId, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Analyzes company culture from website and social media
    /// </summary>
    /// <param name="companyName">Company name</param>
    /// <param name="companyWebsite">Company website URL</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Company culture analysis</returns>
    Task<CompanyCultureAnalysis> AnalyzeCompanyCultureAsync(
        string companyName, 
        string? companyWebsite, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Exports tailored resume to PDF format
    /// </summary>
    /// <param name="htmlContent">HTML content of resume</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>PDF file as byte array</returns>
    Task<byte[]> ExportToPdfAsync(
        string htmlContent, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Compares original and tailored resume
    /// </summary>
    /// <param name="originalContent">Original resume content</param>
    /// <param name="tailoredContent">Tailored resume content</param>
    /// <returns>Comparison result with highlighted changes</returns>
    Task<ResumeComparisonResult> CompareResumesAsync(
        string originalContent, 
        string tailoredContent);
}

/// <summary>
/// Result of tailored resume generation
/// </summary>
public class TailoredResumeResult
{
    public string HtmlContent { get; set; } = string.Empty;
    public string PlainTextContent { get; set; } = string.Empty;
    public List<string> OptimizedKeywords { get; set; } = new();
    public List<string> AddedSkills { get; set; } = new();
    public List<string> HighlightedExperiences { get; set; } = new();
    public int AtsScore { get; set; } // 0-100
}

/// <summary>
/// Company culture analysis result
/// </summary>
public class CompanyCultureAnalysis
{
    public string CompanyName { get; set; } = string.Empty;
    public string CultureSummary { get; set; } = string.Empty;
    public List<string> CoreValues { get; set; } = new();
    public List<string> WorkEnvironmentKeywords { get; set; } = new();
    public string ToneRecommendation { get; set; } = string.Empty; // "Formal", "Casual", "Technical", etc.
}

/// <summary>
/// Resume comparison result
/// </summary>
public class ResumeComparisonResult
{
    public string OriginalContent { get; set; } = string.Empty;
    public string TailoredContent { get; set; } = string.Empty;
    public List<ResumeChange> Changes { get; set; } = new();
    public int SimilarityScore { get; set; } // 0-100
}

/// <summary>
/// Represents a change between original and tailored resume
/// </summary>
public class ResumeChange
{
    public string Section { get; set; } = string.Empty; // "Skills", "Experience", "Summary", etc.
    public string ChangeType { get; set; } = string.Empty; // "Added", "Modified", "Removed", "Highlighted"
    public string OriginalText { get; set; } = string.Empty;
    public string NewText { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}
