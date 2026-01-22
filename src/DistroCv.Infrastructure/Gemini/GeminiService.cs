using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DistroCv.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DistroCv.Infrastructure.Gemini;

/// <summary>
/// Service for interacting with Google Gemini API
/// </summary>
public class GeminiService : IGeminiService
{
    private readonly HttpClient _httpClient;
    private readonly GeminiConfiguration _config;
    private readonly ILogger<GeminiService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public GeminiService(
        HttpClient httpClient,
        IOptions<GeminiConfiguration> config,
        ILogger<GeminiService> logger)
    {
        _httpClient = httpClient;
        _config = config.Value;
        _logger = logger;
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = true
        };

        // Configure HttpClient
        _httpClient.BaseAddress = new Uri(_config.BaseUrl);
    }

    /// <summary>
    /// Analyzes parsed resume data and extracts structured information
    /// </summary>
    public async Task<ResumeAnalysisResult> AnalyzeResumeAsync(string parsedResumeJson)
    {
        try
        {
            _logger.LogInformation("Starting resume analysis with Gemini");

            var prompt = BuildResumeAnalysisPrompt(parsedResumeJson);
            var response = await CallGeminiAsync(prompt);

            _logger.LogDebug("Gemini response: {Response}", response);

            // Parse the response to extract structured data
            var analysisResult = ParseResumeAnalysisResponse(response);

            _logger.LogInformation("Resume analysis completed successfully. Found {SkillCount} skills, {ExpCount} experiences, {EduCount} education entries",
                analysisResult.Skills.Count, analysisResult.Experience.Count, analysisResult.Education.Count);

            return analysisResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing resume with Gemini");
            throw;
        }
    }

    /// <summary>
    /// Generates embedding vector for text using Gemini
    /// </summary>
    public async Task<float[]> GenerateEmbeddingAsync(string text)
    {
        try
        {
            _logger.LogInformation("Generating embedding vector with Gemini");

            // Use Gemini's embedding model
            var url = $"/models/embedding-001:embedContent?key={_config.ApiKey}";

            var request = new
            {
                model = "models/embedding-001",
                content = new
                {
                    parts = new[]
                    {
                        new { text = text }
                    }
                }
            };

            var response = await _httpClient.PostAsJsonAsync(url, request, _jsonOptions);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<EmbeddingResponse>(_jsonOptions);
            
            if (result?.Embedding?.Values == null || result.Embedding.Values.Length == 0)
            {
                throw new InvalidOperationException("Gemini returned empty embedding");
            }

            _logger.LogInformation("Generated embedding vector with {Dimensions} dimensions", 
                result.Embedding.Values.Length);

            return result.Embedding.Values;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating embedding with Gemini");
            throw;
        }
    }

    /// <summary>
    /// Calculates match score between digital twin and job posting
    /// </summary>
    public async Task<MatchResult> CalculateMatchScoreAsync(string digitalTwinData, string jobPostingData)
    {
        try
        {
            _logger.LogInformation("Calculating match score with Gemini");

            var prompt = BuildMatchScorePrompt(digitalTwinData, jobPostingData);
            var response = await CallGeminiAsync(prompt);

            _logger.LogDebug("Gemini match response: {Response}", response);

            var matchResult = ParseMatchScoreResponse(response);

            _logger.LogInformation("Match score calculated: {Score}", matchResult.MatchScore);

            return matchResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating match score with Gemini");
            throw;
        }
    }

    /// <summary>
    /// Calls Gemini API with a prompt
    /// </summary>
    private async Task<string> CallGeminiAsync(string prompt)
    {
        var url = $"/models/{_config.Model}:generateContent?key={_config.ApiKey}";

        var request = new GeminiRequest
        {
            Contents = new[]
            {
                new Content
                {
                    Parts = new[]
                    {
                        new Part { Text = prompt }
                    }
                }
            },
            GenerationConfig = new GenerationConfig
            {
                Temperature = _config.Temperature,
                MaxOutputTokens = _config.MaxTokens
            }
        };

        var response = await _httpClient.PostAsJsonAsync(url, request, _jsonOptions);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("Gemini API error: {StatusCode} - {Error}", 
                response.StatusCode, errorContent);
            throw new HttpRequestException($"Gemini API error: {response.StatusCode}");
        }

        var result = await response.Content.ReadFromJsonAsync<GeminiResponse>(_jsonOptions);
        
        if (result?.Candidates == null || result.Candidates.Length == 0)
        {
            throw new InvalidOperationException("Gemini returned no candidates");
        }

        var text = result.Candidates[0].Content?.Parts?[0]?.Text;
        
        if (string.IsNullOrEmpty(text))
        {
            throw new InvalidOperationException("Gemini returned empty response");
        }

        return text;
    }

    /// <summary>
    /// Builds prompt for resume analysis
    /// </summary>
    private string BuildResumeAnalysisPrompt(string parsedResumeJson)
    {
        return $@"You are an expert resume analyzer. Analyze the following parsed resume data and extract structured information.

Parsed Resume Data:
{parsedResumeJson}

Please provide a comprehensive analysis in the following JSON format:
{{
  ""skills"": [""skill1"", ""skill2"", ...],
  ""experience"": [
    {{
      ""jobTitle"": ""Title"",
      ""company"": ""Company Name"",
      ""duration"": ""Start - End"",
      ""description"": ""Brief description"",
      ""achievements"": [""achievement1"", ""achievement2""]
    }}
  ],
  ""education"": [
    {{
      ""degree"": ""Degree Name"",
      ""institution"": ""Institution Name"",
      ""duration"": ""Start - End"",
      ""fieldOfStudy"": ""Field"",
      ""gpa"": ""GPA if available""
    }}
  ],
  ""careerGoals"": ""Inferred career goals and aspirations based on the resume"",
  ""contactInfo"": {{
    ""email"": ""email if found"",
    ""phone"": ""phone if found"",
    ""linkedin"": ""linkedin if found"",
    ""github"": ""github if found"",
    ""location"": ""location if found""
  }}
}}

Important:
1. Extract ALL skills mentioned (technical, soft skills, tools, technologies, languages)
2. For experience, capture job titles, companies, durations, and key achievements
3. For education, include degrees, institutions, fields of study, and GPAs if available
4. Infer career goals from the resume content, summary, and experience trajectory
5. Extract contact information if present
6. Return ONLY valid JSON, no additional text or markdown formatting
7. If information is not available, use empty strings or empty arrays";
    }

    /// <summary>
    /// Builds prompt for match score calculation
    /// </summary>
    private string BuildMatchScorePrompt(string digitalTwinData, string jobPostingData)
    {
        return $@"You are an expert job matching AI. Calculate how well a candidate matches a job posting.

Candidate Profile (Digital Twin):
{digitalTwinData}

Job Posting:
{jobPostingData}

Analyze the match and provide a response in the following JSON format:
{{
  ""matchScore"": 85,
  ""reasoning"": ""Detailed explanation of why this score was given, highlighting strengths and gaps"",
  ""skillGaps"": [""skill1"", ""skill2"", ...]
}}

Scoring Guidelines:
- 90-100: Excellent match, candidate exceeds requirements
- 80-89: Strong match, candidate meets most requirements
- 70-79: Good match, candidate meets core requirements with some gaps
- 60-69: Fair match, candidate has relevant experience but significant gaps
- Below 60: Poor match, candidate lacks key requirements

Important:
1. Consider skills, experience level, education, and career trajectory
2. Identify specific skill gaps that prevent a perfect match
3. Be realistic and objective in scoring
4. Provide actionable reasoning that explains the score
5. Return ONLY valid JSON, no additional text or markdown formatting";
    }

    /// <summary>
    /// Parses Gemini response for resume analysis
    /// </summary>
    private ResumeAnalysisResult ParseResumeAnalysisResponse(string response)
    {
        try
        {
            // Remove markdown code blocks if present
            var cleanedResponse = response.Trim();
            if (cleanedResponse.StartsWith("```json"))
            {
                cleanedResponse = cleanedResponse.Substring(7);
            }
            if (cleanedResponse.StartsWith("```"))
            {
                cleanedResponse = cleanedResponse.Substring(3);
            }
            if (cleanedResponse.EndsWith("```"))
            {
                cleanedResponse = cleanedResponse.Substring(0, cleanedResponse.Length - 3);
            }
            cleanedResponse = cleanedResponse.Trim();

            var jsonDoc = JsonDocument.Parse(cleanedResponse);
            var root = jsonDoc.RootElement;

            var result = new ResumeAnalysisResult();

            // Extract skills
            if (root.TryGetProperty("skills", out var skillsElement))
            {
                result.Skills = skillsElement.EnumerateArray()
                    .Select(s => s.GetString() ?? string.Empty)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList();
            }

            // Extract experience
            if (root.TryGetProperty("experience", out var expElement))
            {
                result.Experience = expElement.EnumerateArray()
                    .Select(e => new ExperienceEntry
                    {
                        JobTitle = e.TryGetProperty("jobTitle", out var jt) ? jt.GetString() ?? "" : "",
                        Company = e.TryGetProperty("company", out var c) ? c.GetString() ?? "" : "",
                        Duration = e.TryGetProperty("duration", out var d) ? d.GetString() ?? "" : "",
                        Description = e.TryGetProperty("description", out var desc) ? desc.GetString() ?? "" : "",
                        Achievements = e.TryGetProperty("achievements", out var ach) 
                            ? ach.EnumerateArray().Select(a => a.GetString() ?? "").Where(a => !string.IsNullOrWhiteSpace(a)).ToList()
                            : new List<string>()
                    })
                    .ToList();
            }

            // Extract education
            if (root.TryGetProperty("education", out var eduElement))
            {
                result.Education = eduElement.EnumerateArray()
                    .Select(e => new EducationEntry
                    {
                        Degree = e.TryGetProperty("degree", out var deg) ? deg.GetString() ?? "" : "",
                        Institution = e.TryGetProperty("institution", out var inst) ? inst.GetString() ?? "" : "",
                        Duration = e.TryGetProperty("duration", out var dur) ? dur.GetString() ?? "" : "",
                        FieldOfStudy = e.TryGetProperty("fieldOfStudy", out var field) ? field.GetString() ?? "" : "",
                        GPA = e.TryGetProperty("gpa", out var gpa) ? gpa.GetString() ?? "" : ""
                    })
                    .ToList();
            }

            // Extract career goals
            if (root.TryGetProperty("careerGoals", out var goalsElement))
            {
                result.CareerGoals = goalsElement.GetString() ?? string.Empty;
            }

            // Extract contact info
            if (root.TryGetProperty("contactInfo", out var contactElement))
            {
                result.ContactInfo = new ContactInfo
                {
                    Email = contactElement.TryGetProperty("email", out var email) ? email.GetString() : null,
                    Phone = contactElement.TryGetProperty("phone", out var phone) ? phone.GetString() : null,
                    LinkedIn = contactElement.TryGetProperty("linkedin", out var linkedin) ? linkedin.GetString() : null,
                    GitHub = contactElement.TryGetProperty("github", out var github) ? github.GetString() : null,
                    Location = contactElement.TryGetProperty("location", out var location) ? location.GetString() : null
                };
            }

            return result;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error parsing Gemini resume analysis response: {Response}", response);
            throw new InvalidOperationException("Failed to parse Gemini response", ex);
        }
    }

    /// <summary>
    /// Parses Gemini response for match score
    /// </summary>
    private MatchResult ParseMatchScoreResponse(string response)
    {
        try
        {
            // Remove markdown code blocks if present
            var cleanedResponse = response.Trim();
            if (cleanedResponse.StartsWith("```json"))
            {
                cleanedResponse = cleanedResponse.Substring(7);
            }
            if (cleanedResponse.StartsWith("```"))
            {
                cleanedResponse = cleanedResponse.Substring(3);
            }
            if (cleanedResponse.EndsWith("```"))
            {
                cleanedResponse = cleanedResponse.Substring(0, cleanedResponse.Length - 3);
            }
            cleanedResponse = cleanedResponse.Trim();

            var jsonDoc = JsonDocument.Parse(cleanedResponse);
            var root = jsonDoc.RootElement;

            var result = new MatchResult();

            // Extract match score
            if (root.TryGetProperty("matchScore", out var scoreElement))
            {
                if (scoreElement.ValueKind == JsonValueKind.Number)
                {
                    result.MatchScore = scoreElement.GetDecimal();
                }
                else if (scoreElement.ValueKind == JsonValueKind.String)
                {
                    if (decimal.TryParse(scoreElement.GetString(), out var score))
                    {
                        result.MatchScore = score;
                    }
                }
            }

            // Extract reasoning
            if (root.TryGetProperty("reasoning", out var reasoningElement))
            {
                result.Reasoning = reasoningElement.GetString() ?? string.Empty;
            }

            // Extract skill gaps
            if (root.TryGetProperty("skillGaps", out var gapsElement))
            {
                result.SkillGaps = gapsElement.EnumerateArray()
                    .Select(s => s.GetString() ?? string.Empty)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList();
            }

            return result;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error parsing Gemini match score response: {Response}", response);
            throw new InvalidOperationException("Failed to parse Gemini response", ex);
        }
    }

    #region DTOs for Gemini API

    private class GeminiRequest
    {
        [JsonPropertyName("contents")]
        public Content[] Contents { get; set; } = Array.Empty<Content>();

        [JsonPropertyName("generationConfig")]
        public GenerationConfig? GenerationConfig { get; set; }
    }

    private class Content
    {
        [JsonPropertyName("parts")]
        public Part[] Parts { get; set; } = Array.Empty<Part>();
    }

    private class Part
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
    }

    private class GenerationConfig
    {
        [JsonPropertyName("temperature")]
        public double Temperature { get; set; }

        [JsonPropertyName("maxOutputTokens")]
        public int MaxOutputTokens { get; set; }
    }

    private class GeminiResponse
    {
        [JsonPropertyName("candidates")]
        public Candidate[]? Candidates { get; set; }
    }

    private class Candidate
    {
        [JsonPropertyName("content")]
        public Content? Content { get; set; }
    }

    private class EmbeddingResponse
    {
        [JsonPropertyName("embedding")]
        public EmbeddingData? Embedding { get; set; }
    }

    private class EmbeddingData
    {
        [JsonPropertyName("values")]
        public float[]? Values { get; set; }
    }

    #endregion
}
