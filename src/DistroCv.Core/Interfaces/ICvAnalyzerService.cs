namespace DistroCv.Core.Interfaces;

/// <summary>
/// Analyzes a user's CV/resume content to extract structured data
/// for personalized email generation.
/// Layer: Application (Core)
/// </summary>
public interface ICvAnalyzerService
{
    /// <summary>
    /// Extracts key highlights from the user's CV text that are relevant
    /// to a specific job posting. Uses Gemini API under the hood.
    /// </summary>
    /// <param name="cvText">Raw or parsed CV text (from S3 / DigitalTwin)</param>
    /// <param name="jobDescription">Full job posting description</param>
    /// <param name="language">Target language code (tr, en)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Structured CV analysis tailored to the target job</returns>
    Task<CvAnalysisResult> AnalyzeCvForJobAsync(
        string cvText,
        string jobDescription,
        string language = "tr",
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of CV analysis tailored to a specific job posting
/// </summary>
public class CvAnalysisResult
{
    /// <summary>Top 5 skills matching the job description</summary>
    public List<string> RelevantSkills { get; set; } = new();

    /// <summary>Most relevant experience entries for this job</summary>
    public List<string> RelevantExperience { get; set; } = new();

    /// <summary>A 2-3 sentence summary of why the candidate fits</summary>
    public string FitSummary { get; set; } = string.Empty;

    /// <summary>Candidate's full name extracted from CV</summary>
    public string CandidateName { get; set; } = string.Empty;

    /// <summary>Years of relevant experience</summary>
    public int EstimatedYearsOfExperience { get; set; }
}
