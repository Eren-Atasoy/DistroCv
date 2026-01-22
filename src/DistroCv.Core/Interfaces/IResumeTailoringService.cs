using DistroCv.Core.Entities;

namespace DistroCv.Core.Interfaces;

/// <summary>
/// Service interface for resume tailoring and cover letter generation
/// </summary>
public interface IResumeTailoringService
{
    /// <summary>
    /// Generates a tailored resume for a specific job posting
    /// </summary>
    Task<string> GenerateTailoredResumeAsync(DigitalTwin twin, JobPosting job);
    
    /// <summary>
    /// Generates a personalized cover letter
    /// </summary>
    Task<string> GenerateCoverLetterAsync(DigitalTwin twin, JobPosting job, VerifiedCompany? company = null);
    
    /// <summary>
    /// Exports HTML content to PDF
    /// </summary>
    Task<byte[]> ExportToPdfAsync(string htmlContent);
    
    /// <summary>
    /// Compares original and tailored resume
    /// </summary>
    Task<string> GetResumeDiffAsync(string original, string tailored);
}
