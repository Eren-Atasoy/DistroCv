namespace DistroCv.Core.Interfaces;

/// <summary>
/// Service for interacting with Google Gemini API
/// </summary>
public interface IGeminiService
{
    /// <summary>
    /// Analyzes parsed resume data and extracts structured information
    /// </summary>
    /// <param name="parsedResumeJson">The parsed resume data in JSON format</param>
    /// <returns>Structured analysis including skills, experience, education, and career goals</returns>
    Task<ResumeAnalysisResult> AnalyzeResumeAsync(string parsedResumeJson);

    /// <summary>
    /// Generates embedding vector for text using Gemini
    /// </summary>
    /// <param name="text">The text to generate embeddings for</param>
    /// <returns>Embedding vector as float array</returns>
    Task<float[]> GenerateEmbeddingAsync(string text);

    /// <summary>
    /// Calculates match score between digital twin and job posting
    /// </summary>
    /// <param name="digitalTwinData">The digital twin data</param>
    /// <param name="jobPostingData">The job posting data</param>
    /// <returns>Match score and reasoning</returns>
    Task<MatchResult> CalculateMatchScoreAsync(string digitalTwinData, string jobPostingData);
    
    /// <summary>
    /// Generates content using Gemini AI
    /// </summary>
    /// <param name="prompt">The prompt for content generation</param>
    /// <returns>Generated content</returns>
    Task<string> GenerateContentAsync(string prompt);
}

/// <summary>
/// Result of resume analysis by Gemini
/// </summary>
public class ResumeAnalysisResult
{
    public List<string> Skills { get; set; } = new();
    public List<ExperienceEntry> Experience { get; set; } = new();
    public List<EducationEntry> Education { get; set; } = new();
    public string CareerGoals { get; set; } = string.Empty;
    public ContactInfo? ContactInfo { get; set; }
}

/// <summary>
/// Work experience entry
/// </summary>
public class ExperienceEntry
{
    public string JobTitle { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Achievements { get; set; } = new();
}

/// <summary>
/// Education entry
/// </summary>
public class EducationEntry
{
    public string Degree { get; set; } = string.Empty;
    public string Institution { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;
    public string FieldOfStudy { get; set; } = string.Empty;
    public string GPA { get; set; } = string.Empty;
}

/// <summary>
/// Contact information
/// </summary>
public class ContactInfo
{
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? LinkedIn { get; set; }
    public string? GitHub { get; set; }
    public string? Location { get; set; }
}

/// <summary>
/// Match calculation result
/// </summary>
public class MatchResult
{
    public decimal MatchScore { get; set; }
    public string Reasoning { get; set; } = string.Empty;
    public List<string> SkillGaps { get; set; } = new();
}
