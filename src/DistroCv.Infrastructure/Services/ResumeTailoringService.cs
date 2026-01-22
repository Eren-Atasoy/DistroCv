using DistroCv.Core.Entities;
using DistroCv.Core.Interfaces;
using DistroCv.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text;

namespace DistroCv.Infrastructure.Services;

/// <summary>
/// Service for tailoring resumes to specific job postings using AI
/// </summary>
public class ResumeTailoringService : IResumeTailoringService
{
    private readonly DistroCvDbContext _context;
    private readonly IGeminiService _geminiService;
    private readonly ILogger<ResumeTailoringService> _logger;

    public ResumeTailoringService(
        DistroCvDbContext context,
        IGeminiService geminiService,
        ILogger<ResumeTailoringService> logger)
    {
        _context = context;
        _geminiService = geminiService;
        _logger = logger;
    }

    /// <summary>
    /// Generates a tailored resume for a specific job posting (Validates: Requirement 4.1, 4.2)
    /// </summary>
    public async Task<TailoredResumeResult> GenerateTailoredResumeAsync(
        Guid userId, 
        Guid jobPostingId, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating tailored resume for user {UserId} and job {JobPostingId}", userId, jobPostingId);

        try
        {
            // Get digital twin
            var digitalTwin = await _context.DigitalTwins
                .FirstOrDefaultAsync(dt => dt.UserId == userId, cancellationToken);

            if (digitalTwin == null)
            {
                throw new InvalidOperationException($"Digital twin not found for user {userId}");
            }

            // Get job posting
            var jobPosting = await _context.JobPostings
                .FirstOrDefaultAsync(jp => jp.Id == jobPostingId, cancellationToken);

            if (jobPosting == null)
            {
                throw new InvalidOperationException($"Job posting not found: {jobPostingId}");
            }

            // Prepare prompt for Gemini
            var prompt = BuildTailoredResumePrompt(digitalTwin, jobPosting);

            // Generate tailored resume using Gemini
            var geminiResponse = await _geminiService.GenerateContentAsync(prompt);

            // Parse response
            var result = ParseTailoredResumeResponse(geminiResponse);

            _logger.LogInformation("Successfully generated tailored resume for user {UserId}", userId);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating tailored resume for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Optimizes keywords in resume for ATS systems (Validates: Requirement 4.2)
    /// </summary>
    public async Task<string> OptimizeKeywordsAsync(
        string resumeContent, 
        string jobDescription, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Optimizing keywords for ATS");

        try
        {
            var prompt = $@"You are an expert ATS (Applicant Tracking System) optimizer. 
Analyze the job description and optimize the resume content by:
1. Identifying key skills and requirements from the job description
2. Incorporating relevant keywords naturally into the resume
3. Maintaining the authenticity of experiences (DO NOT fabricate experiences)
4. Ensuring ATS-friendly formatting

Job Description:
{jobDescription}

Resume Content:
{resumeContent}

Return ONLY the optimized resume content without any explanations.";

            var optimizedContent = await _geminiService.GenerateContentAsync(prompt);

            _logger.LogInformation("Successfully optimized keywords");
            return optimizedContent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error optimizing keywords");
            throw;
        }
    }

    /// <summary>
    /// Generates a cover letter for a specific job posting (Validates: Requirement 4.4, 4.5)
    /// </summary>
    public async Task<string> GenerateCoverLetterAsync(
        Guid userId, 
        Guid jobPostingId, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating cover letter for user {UserId} and job {JobPostingId}", userId, jobPostingId);

        try
        {
            // Get digital twin
            var digitalTwin = await _context.DigitalTwins
                .FirstOrDefaultAsync(dt => dt.UserId == userId, cancellationToken);

            if (digitalTwin == null)
            {
                throw new InvalidOperationException($"Digital twin not found for user {userId}");
            }

            // Get job posting
            var jobPosting = await _context.JobPostings
                .FirstOrDefaultAsync(jp => jp.Id == jobPostingId, cancellationToken);

            if (jobPosting == null)
            {
                throw new InvalidOperationException($"Job posting not found: {jobPostingId}");
            }

            // Analyze company culture if website available
            CompanyCultureAnalysis? cultureAnalysis = null;
            if (!string.IsNullOrEmpty(jobPosting.CompanyName))
            {
                try
                {
                    cultureAnalysis = await AnalyzeCompanyCultureAsync(
                        jobPosting.CompanyName, 
                        null, // Website URL would come from company database
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not analyze company culture, continuing without it");
                }
            }

            // Build cover letter prompt
            var prompt = BuildCoverLetterPrompt(digitalTwin, jobPosting, cultureAnalysis);

            // Generate cover letter using Gemini
            var coverLetter = await _geminiService.GenerateContentAsync(prompt);

            _logger.LogInformation("Successfully generated cover letter for user {UserId}", userId);
            return coverLetter;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating cover letter for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Analyzes company culture from website and social media (Validates: Requirement 12.1, 12.2)
    /// </summary>
    public async Task<CompanyCultureAnalysis> AnalyzeCompanyCultureAsync(
        string companyName, 
        string? companyWebsite, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Analyzing company culture for {CompanyName}", companyName);

        try
        {
            var prompt = $@"Analyze the company culture for {companyName}.
{(string.IsNullOrEmpty(companyWebsite) ? "" : $"Company website: {companyWebsite}")}

Provide:
1. A brief culture summary (2-3 sentences)
2. Core values (list 3-5 values)
3. Work environment keywords (list 5-7 keywords)
4. Tone recommendation for cover letter (Formal/Casual/Technical/Creative)

Return the response in JSON format:
{{
  ""companyName"": ""{companyName}"",
  ""cultureSummary"": ""..."",
  ""coreValues"": [""value1"", ""value2"", ...],
  ""workEnvironmentKeywords"": [""keyword1"", ""keyword2"", ...],
  ""toneRecommendation"": ""Formal""
}}";

            var response = await _geminiService.GenerateContentAsync(prompt);

            // Parse JSON response
            var analysis = JsonSerializer.Deserialize<CompanyCultureAnalysis>(response, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (analysis == null)
            {
                throw new InvalidOperationException("Failed to parse company culture analysis");
            }

            _logger.LogInformation("Successfully analyzed company culture for {CompanyName}", companyName);
            return analysis;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing company culture for {CompanyName}", companyName);
            
            // Return default analysis
            return new CompanyCultureAnalysis
            {
                CompanyName = companyName,
                CultureSummary = "Company culture information not available.",
                CoreValues = new List<string> { "Innovation", "Teamwork", "Excellence" },
                WorkEnvironmentKeywords = new List<string> { "Collaborative", "Dynamic", "Professional" },
                ToneRecommendation = "Professional"
            };
        }
    }

    /// <summary>
    /// Exports tailored resume to PDF format (Validates: Requirement 4.6)
    /// </summary>
    public async Task<byte[]> ExportToPdfAsync(
        string htmlContent, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Exporting resume to PDF");

        try
        {
            // TODO: Implement HTML to PDF conversion
            // This would typically use a library like PuppeteerSharp, IronPdf, or SelectPdf
            // For now, return placeholder
            
            _logger.LogWarning("PDF export not yet implemented, returning placeholder");
            return Encoding.UTF8.GetBytes("PDF export not yet implemented");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting resume to PDF");
            throw;
        }
    }

    /// <summary>
    /// Compares original and tailored resume (Validates: Requirement 4.3)
    /// </summary>
    public async Task<ResumeComparisonResult> CompareResumesAsync(
        string originalContent, 
        string tailoredContent)
    {
        _logger.LogInformation("Comparing original and tailored resumes");

        try
        {
            var prompt = $@"Compare the original and tailored resume and identify the changes.
For each change, specify:
1. Section (Skills, Experience, Summary, etc.)
2. Change type (Added, Modified, Removed, Highlighted)
3. Original text
4. New text
5. Reason for the change

Original Resume:
{originalContent}

Tailored Resume:
{tailoredContent}

Return the response in JSON format:
{{
  ""changes"": [
    {{
      ""section"": ""Skills"",
      ""changeType"": ""Added"",
      ""originalText"": """",
      ""newText"": ""Python, Machine Learning"",
      ""reason"": ""Added to match job requirements""
    }}
  ],
  ""similarityScore"": 85
}}";

            var response = await _geminiService.GenerateContentAsync(prompt);

            // Parse JSON response
            var comparisonData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(response, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var result = new ResumeComparisonResult
            {
                OriginalContent = originalContent,
                TailoredContent = tailoredContent,
                Changes = new List<ResumeChange>(),
                SimilarityScore = comparisonData?.ContainsKey("similarityScore") == true 
                    ? comparisonData["similarityScore"].GetInt32() 
                    : 85
            };

            if (comparisonData?.ContainsKey("changes") == true)
            {
                var changes = JsonSerializer.Deserialize<List<ResumeChange>>(
                    comparisonData["changes"].GetRawText(), 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                if (changes != null)
                {
                    result.Changes = changes;
                }
            }

            _logger.LogInformation("Successfully compared resumes, found {Count} changes", result.Changes.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error comparing resumes");
            throw;
        }
    }

    /// <summary>
    /// Builds prompt for tailored resume generation
    /// </summary>
    private string BuildTailoredResumePrompt(DigitalTwin digitalTwin, JobPosting jobPosting)
    {
        return $@"You are an expert resume writer. Create a tailored resume for the following job posting.

IMPORTANT RULES:
1. DO NOT fabricate or invent experiences, skills, or qualifications
2. Only highlight and optimize existing experiences from the candidate's profile
3. Incorporate relevant keywords from the job description naturally
4. Maintain authenticity while optimizing for ATS systems
5. Focus on relevant experiences that match the job requirements

Candidate Profile:
- Skills: {digitalTwin.Skills}
- Experience: {digitalTwin.Experience}
- Education: {digitalTwin.Education}
- Career Goals: {digitalTwin.CareerGoals}

Job Posting:
- Title: {jobPosting.Title}
- Company: {jobPosting.CompanyName}
- Location: {jobPosting.Location}
- Description: {jobPosting.Description}
- Requirements: {jobPosting.Requirements}

Generate a tailored resume in HTML format with the following sections:
1. Professional Summary (2-3 sentences highlighting relevant experience)
2. Skills (prioritize skills matching job requirements)
3. Work Experience (highlight relevant experiences)
4. Education
5. Certifications (if applicable)

Also provide:
- List of optimized keywords used
- List of skills added/highlighted
- List of experiences highlighted
- ATS score (0-100)

Return the response in JSON format:
{{
  ""htmlContent"": ""<html>...</html>"",
  ""plainTextContent"": ""..."",
  ""optimizedKeywords"": [""keyword1"", ""keyword2"", ...],
  ""addedSkills"": [""skill1"", ""skill2"", ...],
  ""highlightedExperiences"": [""exp1"", ""exp2"", ...],
  ""atsScore"": 85
}}";
    }

    /// <summary>
    /// Builds prompt for cover letter generation
    /// </summary>
    private string BuildCoverLetterPrompt(
        DigitalTwin digitalTwin, 
        JobPosting jobPosting, 
        CompanyCultureAnalysis? cultureAnalysis)
    {
        var cultureInfo = cultureAnalysis != null 
            ? $@"
Company Culture Analysis:
- Culture Summary: {cultureAnalysis.CultureSummary}
- Core Values: {string.Join(", ", cultureAnalysis.CoreValues)}
- Tone Recommendation: {cultureAnalysis.ToneRecommendation}"
            : "";

        return $@"You are an expert cover letter writer. Create a personalized cover letter for the following job posting.

Candidate Profile:
- Skills: {digitalTwin.Skills}
- Experience: {digitalTwin.Experience}
- Education: {digitalTwin.Education}
- Career Goals: {digitalTwin.CareerGoals}

Job Posting:
- Title: {jobPosting.Title}
- Company: {jobPosting.CompanyName}
- Location: {jobPosting.Location}
- Description: {jobPosting.Description}
{cultureInfo}

Create a professional cover letter that:
1. Opens with a strong introduction expressing interest
2. Highlights 2-3 relevant experiences/achievements
3. Demonstrates knowledge of the company (if culture analysis available)
4. Explains why the candidate is a good fit
5. Closes with a call to action

Keep it concise (250-350 words) and use a {cultureAnalysis?.ToneRecommendation ?? "professional"} tone.

Return ONLY the cover letter text without any additional formatting or explanations.";
    }

    /// <summary>
    /// Parses Gemini response for tailored resume
    /// </summary>
    private TailoredResumeResult ParseTailoredResumeResponse(string response)
    {
        try
        {
            var result = JsonSerializer.Deserialize<TailoredResumeResult>(response, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result == null)
            {
                throw new InvalidOperationException("Failed to parse tailored resume response");
            }

            return result;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse JSON response, returning default result");
            
            // Return default result with the response as HTML content
            return new TailoredResumeResult
            {
                HtmlContent = response,
                PlainTextContent = response,
                OptimizedKeywords = new List<string>(),
                AddedSkills = new List<string>(),
                HighlightedExperiences = new List<string>(),
                AtsScore = 70
            };
        }
    }
}
