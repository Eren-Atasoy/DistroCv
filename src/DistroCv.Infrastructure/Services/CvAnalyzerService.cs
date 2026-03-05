using System.Text.Json;
using DistroCv.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace DistroCv.Infrastructure.Services;

/// <summary>
/// Analyzes CV content against a job description using Google Gemini API.
/// Extracts the most relevant skills, experience highlights, and a fit summary
/// tailored to the target position.
/// Layer: Infrastructure/Services
/// </summary>
public class CvAnalyzerService : ICvAnalyzerService
{
    private readonly IGeminiService _geminiService;
    private readonly ILogger<CvAnalyzerService> _logger;

    public CvAnalyzerService(
        IGeminiService geminiService,
        ILogger<CvAnalyzerService> logger)
    {
        _geminiService = geminiService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<CvAnalysisResult> AnalyzeCvForJobAsync(
        string cvText,
        string jobDescription,
        string language = "tr",
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting CV-to-job analysis for email generation");

        try
        {
            var prompt = BuildAnalysisPrompt(cvText, jobDescription);
            var response = await _geminiService.GenerateContentAsync(prompt, language);

            var result = ParseAnalysisResponse(response);

            _logger.LogInformation(
                "CV analysis complete: {SkillCount} relevant skills, {ExpCount} experience highlights, ~{Years} years",
                result.RelevantSkills.Count,
                result.RelevantExperience.Count,
                result.EstimatedYearsOfExperience);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing CV for job matching");
            throw;
        }
    }

    private static string BuildAnalysisPrompt(string cvText, string jobDescription)
    {
        return $@"You are an expert career advisor. Analyze the following CV against the job description and extract the most relevant information for a personalized application email.

=== CV TEXT ===
{cvText}

=== JOB DESCRIPTION ===
{jobDescription}

Return ONLY a valid JSON object with this exact structure (no markdown, no extra text):
{{
  ""candidateName"": ""Full name from CV"",
  ""relevantSkills"": [""skill1"", ""skill2"", ""skill3"", ""skill4"", ""skill5""],
  ""relevantExperience"": [
    ""One sentence about most relevant work experience"",
    ""One sentence about second most relevant experience""
  ],
  ""fitSummary"": ""2-3 sentence summary of why this candidate is a strong fit for this specific role"",
  ""estimatedYearsOfExperience"": 5
}}

Rules:
1. Select only skills that DIRECTLY match the job requirements (max 5)
2. Highlight experience entries most relevant to THIS job (max 3)
3. The fit summary should be specific and reference both the candidate's strengths and the job requirements
4. estimatedYearsOfExperience should be the total years of professional experience
5. Return ONLY valid JSON";
    }

    private CvAnalysisResult ParseAnalysisResponse(string response)
    {
        try
        {
            var cleaned = CleanJsonResponse(response);
            var doc = JsonDocument.Parse(cleaned);
            var root = doc.RootElement;

            var result = new CvAnalysisResult();

            if (root.TryGetProperty("candidateName", out var name))
                result.CandidateName = name.GetString() ?? string.Empty;

            if (root.TryGetProperty("relevantSkills", out var skills))
                result.RelevantSkills = skills.EnumerateArray()
                    .Select(s => s.GetString() ?? "")
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList();

            if (root.TryGetProperty("relevantExperience", out var exp))
                result.RelevantExperience = exp.EnumerateArray()
                    .Select(s => s.GetString() ?? "")
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList();

            if (root.TryGetProperty("fitSummary", out var summary))
                result.FitSummary = summary.GetString() ?? string.Empty;

            if (root.TryGetProperty("estimatedYearsOfExperience", out var years))
            {
                if (years.ValueKind == JsonValueKind.Number)
                    result.EstimatedYearsOfExperience = years.GetInt32();
                else if (years.ValueKind == JsonValueKind.String && int.TryParse(years.GetString(), out var y))
                    result.EstimatedYearsOfExperience = y;
            }

            return result;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse Gemini CV analysis response: {Response}", response);
            throw new InvalidOperationException("Failed to parse CV analysis response from Gemini", ex);
        }
    }

    private static string CleanJsonResponse(string response)
    {
        var cleaned = response.Trim();
        if (cleaned.StartsWith("```json"))
            cleaned = cleaned["```json".Length..];
        else if (cleaned.StartsWith("```"))
            cleaned = cleaned["```".Length..];

        if (cleaned.EndsWith("```"))
            cleaned = cleaned[..^"```".Length];

        return cleaned.Trim();
    }
}
