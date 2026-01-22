using System.Text.Json;
using DistroCv.Core.DTOs;
using DistroCv.Core.Entities;
using DistroCv.Core.Interfaces;
using DistroCv.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DistroCv.Infrastructure.Services;

/// <summary>
/// Service for analyzing skill gaps and providing learning recommendations
/// </summary>
public class SkillGapService : ISkillGapService
{
    private readonly ISkillGapRepository _skillGapRepository;
    private readonly IGeminiService _geminiService;
    private readonly DistroCvDbContext _dbContext;
    private readonly ILogger<SkillGapService> _logger;

    public SkillGapService(
        ISkillGapRepository skillGapRepository,
        IGeminiService geminiService,
        DistroCvDbContext dbContext,
        ILogger<SkillGapService> logger)
    {
        _skillGapRepository = skillGapRepository;
        _geminiService = geminiService;
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Task 17.1: Analyze skill gaps between user profile and job posting
    /// </summary>
    public async Task<SkillGapAnalysisResultDto> AnalyzeSkillGapsAsync(
        Guid userId, 
        Guid jobMatchId, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Analyzing skill gaps for user {UserId} and job match {JobMatchId}", userId, jobMatchId);

        // Get user's digital twin
        var digitalTwin = await _dbContext.DigitalTwins
            .FirstOrDefaultAsync(dt => dt.UserId == userId, cancellationToken);

        if (digitalTwin == null)
        {
            throw new InvalidOperationException($"Digital twin not found for user {userId}");
        }

        // Get job match with job posting
        var jobMatch = await _dbContext.JobMatches
            .Include(jm => jm.JobPosting)
            .FirstOrDefaultAsync(jm => jm.Id == jobMatchId, cancellationToken);

        if (jobMatch == null)
        {
            throw new InvalidOperationException($"Job match not found: {jobMatchId}");
        }

        // Analyze using Gemini
        var analysisResult = await AnalyzeWithGeminiAsync(
            digitalTwin, 
            jobMatch.JobPosting, 
            cancellationToken);

        // Save skill gaps to database
        await SaveSkillGapsAsync(userId, jobMatchId, analysisResult, cancellationToken);

        _logger.LogInformation("Skill gap analysis completed. Found {Count} gaps", analysisResult.TotalGaps);

        return analysisResult;
    }

    /// <summary>
    /// Analyze skill gaps for user's career goals (without specific job)
    /// </summary>
    public async Task<SkillGapAnalysisResultDto> AnalyzeCareerGapsAsync(
        Guid userId, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Analyzing career gaps for user {UserId}", userId);

        var digitalTwin = await _dbContext.DigitalTwins
            .FirstOrDefaultAsync(dt => dt.UserId == userId, cancellationToken);

        if (digitalTwin == null)
        {
            throw new InvalidOperationException($"Digital twin not found for user {userId}");
        }

        var prompt = BuildCareerGapAnalysisPrompt(digitalTwin);
        var response = await _geminiService.GenerateContentAsync(prompt);
        var analysisResult = ParseSkillGapResponse(response);

        await SaveSkillGapsAsync(userId, null, analysisResult, cancellationToken);

        return analysisResult;
    }

    private async Task<SkillGapAnalysisResultDto> AnalyzeWithGeminiAsync(
        DigitalTwin digitalTwin, 
        JobPosting jobPosting,
        CancellationToken cancellationToken)
    {
        var prompt = BuildSkillGapAnalysisPrompt(digitalTwin, jobPosting);
        var response = await _geminiService.GenerateContentAsync(prompt);
        return ParseSkillGapResponse(response);
    }

    /// <summary>
    /// Task 17.2: Build prompt with categorization logic
    /// </summary>
    private string BuildSkillGapAnalysisPrompt(DigitalTwin digitalTwin, JobPosting jobPosting)
    {
        return $@"You are an expert career advisor. Analyze the skill gaps between the candidate's profile and the job requirements.

Candidate Profile:
- Skills: {digitalTwin.Skills ?? "Not specified"}
- Experience: {digitalTwin.Experience ?? "Not specified"}
- Education: {digitalTwin.Education ?? "Not specified"}
- Career Goals: {digitalTwin.CareerGoals ?? "Not specified"}

Job Posting:
- Title: {jobPosting.Title}
- Company: {jobPosting.CompanyName}
- Description: {jobPosting.Description}
- Requirements: {jobPosting.Requirements ?? "Not specified"}

Analyze and categorize the skill gaps into these categories:
1. Technical Skills (programming languages, frameworks, tools, databases, cloud platforms)
2. Certifications (professional certifications required or preferred)
3. Experience Gaps (years of experience, domain expertise, leadership)
4. Soft Skills (communication, teamwork, problem-solving)

For each gap, provide:
- Skill name
- Category and sub-category
- Importance level (1-5, 5 being most critical)
- Description
- Estimated learning hours
- Course recommendations (from Coursera, Udemy, LinkedIn Learning)
- Project suggestions for portfolio
- Certification recommendations if applicable

Return response in this exact JSON format:
{{
  ""technicalSkills"": [
    {{
      ""skillName"": ""Skill Name"",
      ""category"": ""Technical"",
      ""subCategory"": ""Programming/Cloud/Database/DevOps/etc"",
      ""importanceLevel"": 5,
      ""description"": ""Why this skill is needed"",
      ""estimatedLearningHours"": 40,
      ""courses"": [
        {{
          ""title"": ""Course Title"",
          ""provider"": ""Coursera/Udemy/LinkedIn Learning"",
          ""url"": ""https://..."",
          ""level"": ""Beginner/Intermediate/Advanced"",
          ""estimatedHours"": 20,
          ""price"": 49.99,
          ""rating"": 4.7,
          ""description"": ""Course description""
        }}
      ],
      ""projects"": [
        {{
          ""title"": ""Project Title"",
          ""description"": ""What to build"",
          ""difficulty"": ""Beginner/Intermediate/Advanced"",
          ""technologies"": [""tech1"", ""tech2""],
          ""estimatedHours"": 15,
          ""learningOutcomes"": ""What you'll learn""
        }}
      ],
      ""certifications"": []
    }}
  ],
  ""certifications"": [
    {{
      ""skillName"": ""AWS Certified Solutions Architect"",
      ""category"": ""Certification"",
      ""subCategory"": ""Cloud"",
      ""importanceLevel"": 4,
      ""description"": ""Required for cloud architect roles"",
      ""estimatedLearningHours"": 60,
      ""courses"": [],
      ""projects"": [],
      ""certifications"": [
        {{
          ""name"": ""AWS Solutions Architect Associate"",
          ""provider"": ""AWS"",
          ""url"": ""https://aws.amazon.com/certification/"",
          ""level"": ""Associate"",
          ""cost"": 150,
          ""validityYears"": 3,
          ""description"": ""Validates cloud architecture expertise"",
          ""prerequisites"": [""Basic AWS knowledge""]
        }}
      ]
    }}
  ],
  ""experienceGaps"": [],
  ""softSkills"": [],
  ""summary"": ""Overall assessment of gaps and readiness"",
  ""overallReadinessScore"": 75,
  ""priorityRecommendations"": [""Top priority action 1"", ""Top priority action 2""]
}}

Important:
1. Be realistic about importance levels
2. Provide actionable course URLs from real platforms
3. Suggest practical projects that demonstrate the skill
4. Consider Turkish market preferences
5. Return ONLY valid JSON, no additional text or markdown";
    }

    private string BuildCareerGapAnalysisPrompt(DigitalTwin digitalTwin)
    {
        return $@"You are an expert career advisor. Analyze the skill gaps based on the candidate's career goals.

Candidate Profile:
- Skills: {digitalTwin.Skills ?? "Not specified"}
- Experience: {digitalTwin.Experience ?? "Not specified"}
- Education: {digitalTwin.Education ?? "Not specified"}
- Career Goals: {digitalTwin.CareerGoals ?? "Not specified"}
- Preferences: {digitalTwin.Preferences ?? "Not specified"}

Analyze what skills the candidate needs to develop to reach their career goals.
Categorize into: Technical Skills, Certifications, Experience Gaps, Soft Skills.

Return the same JSON format as skill gap analysis.
Return ONLY valid JSON.";
    }

    private SkillGapAnalysisResultDto ParseSkillGapResponse(string response)
    {
        try
        {
            var cleanedResponse = CleanJsonResponse(response);
            var jsonDoc = JsonDocument.Parse(cleanedResponse);
            var root = jsonDoc.RootElement;

            var technicalSkills = ParseSkillGapList(root, "technicalSkills");
            var certifications = ParseSkillGapList(root, "certifications");
            var experienceGaps = ParseSkillGapList(root, "experienceGaps");
            var softSkills = ParseSkillGapList(root, "softSkills");

            var allGaps = technicalSkills.Concat(certifications).Concat(experienceGaps).Concat(softSkills).ToList();
            var completedGaps = allGaps.Count(g => g.Status == "Completed");
            var inProgressGaps = allGaps.Count(g => g.Status == "InProgress");

            return new SkillGapAnalysisResultDto(
                TechnicalSkills: technicalSkills,
                Certifications: certifications,
                ExperienceGaps: experienceGaps,
                SoftSkills: softSkills,
                TotalGaps: allGaps.Count,
                CompletedGaps: completedGaps,
                InProgressGaps: inProgressGaps,
                OverallReadinessScore: root.TryGetProperty("overallReadinessScore", out var score) ? score.GetDouble() : 50,
                Summary: root.TryGetProperty("summary", out var summary) ? summary.GetString() ?? "" : "",
                PriorityRecommendations: root.TryGetProperty("priorityRecommendations", out var recs) 
                    ? recs.EnumerateArray().Select(r => r.GetString() ?? "").ToList() 
                    : new List<string>()
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing skill gap response");
            return new SkillGapAnalysisResultDto(
                TechnicalSkills: new List<SkillGapDto>(),
                Certifications: new List<SkillGapDto>(),
                ExperienceGaps: new List<SkillGapDto>(),
                SoftSkills: new List<SkillGapDto>(),
                TotalGaps: 0,
                CompletedGaps: 0,
                InProgressGaps: 0,
                OverallReadinessScore: 0,
                Summary: "Analysis failed",
                PriorityRecommendations: new List<string>()
            );
        }
    }

    private List<SkillGapDto> ParseSkillGapList(JsonElement root, string propertyName)
    {
        var result = new List<SkillGapDto>();

        if (!root.TryGetProperty(propertyName, out var array))
            return result;

        foreach (var item in array.EnumerateArray())
        {
            result.Add(new SkillGapDto(
                Id: Guid.NewGuid(),
                SkillName: item.TryGetProperty("skillName", out var name) ? name.GetString() ?? "" : "",
                Category: item.TryGetProperty("category", out var cat) ? cat.GetString() ?? "" : "",
                SubCategory: item.TryGetProperty("subCategory", out var subCat) ? subCat.GetString() ?? "" : "",
                ImportanceLevel: item.TryGetProperty("importanceLevel", out var imp) ? imp.GetInt32() : 3,
                Description: item.TryGetProperty("description", out var desc) ? desc.GetString() : null,
                RecommendedCourses: ParseCourses(item),
                RecommendedProjects: ParseProjects(item),
                RecommendedCertifications: ParseCertifications(item),
                EstimatedLearningHours: item.TryGetProperty("estimatedLearningHours", out var hours) ? hours.GetInt32() : 0,
                Status: "NotStarted",
                ProgressPercentage: 0,
                StartedAt: null,
                CompletedAt: null,
                CreatedAt: DateTime.UtcNow
            ));
        }

        return result;
    }

    private List<CourseRecommendationDto> ParseCourses(JsonElement item)
    {
        var courses = new List<CourseRecommendationDto>();
        if (!item.TryGetProperty("courses", out var coursesArray))
            return courses;

        foreach (var course in coursesArray.EnumerateArray())
        {
            courses.Add(new CourseRecommendationDto(
                Title: course.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "",
                Provider: course.TryGetProperty("provider", out var p) ? p.GetString() ?? "" : "",
                Url: course.TryGetProperty("url", out var u) ? u.GetString() ?? "" : "",
                Level: course.TryGetProperty("level", out var l) ? l.GetString() ?? "" : "",
                EstimatedHours: course.TryGetProperty("estimatedHours", out var h) ? h.GetInt32() : 0,
                Price: course.TryGetProperty("price", out var pr) && pr.ValueKind == JsonValueKind.Number ? (decimal?)pr.GetDecimal() : null,
                Rating: course.TryGetProperty("rating", out var r) && r.ValueKind == JsonValueKind.Number ? (double?)r.GetDouble() : null,
                Description: course.TryGetProperty("description", out var d) ? d.GetString() : null
            ));
        }

        return courses;
    }

    private List<ProjectSuggestionDto> ParseProjects(JsonElement item)
    {
        var projects = new List<ProjectSuggestionDto>();
        if (!item.TryGetProperty("projects", out var projectsArray))
            return projects;

        foreach (var project in projectsArray.EnumerateArray())
        {
            projects.Add(new ProjectSuggestionDto(
                Title: project.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "",
                Description: project.TryGetProperty("description", out var d) ? d.GetString() ?? "" : "",
                Difficulty: project.TryGetProperty("difficulty", out var diff) ? diff.GetString() ?? "" : "",
                Technologies: project.TryGetProperty("technologies", out var tech) 
                    ? tech.EnumerateArray().Select(x => x.GetString() ?? "").ToList() 
                    : new List<string>(),
                EstimatedHours: project.TryGetProperty("estimatedHours", out var h) ? h.GetInt32() : 0,
                GitHubTemplate: project.TryGetProperty("githubTemplate", out var g) ? g.GetString() : null,
                LearningOutcomes: project.TryGetProperty("learningOutcomes", out var l) ? l.GetString() : null
            ));
        }

        return projects;
    }

    private List<CertificationRecommendationDto> ParseCertifications(JsonElement item)
    {
        var certs = new List<CertificationRecommendationDto>();
        if (!item.TryGetProperty("certifications", out var certsArray))
            return certs;

        foreach (var cert in certsArray.EnumerateArray())
        {
            certs.Add(new CertificationRecommendationDto(
                Name: cert.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "",
                Provider: cert.TryGetProperty("provider", out var p) ? p.GetString() ?? "" : "",
                Url: cert.TryGetProperty("url", out var u) ? u.GetString() ?? "" : "",
                Level: cert.TryGetProperty("level", out var l) ? l.GetString() ?? "" : "",
                Cost: cert.TryGetProperty("cost", out var c) && c.ValueKind == JsonValueKind.Number ? (decimal?)c.GetDecimal() : null,
                ValidityYears: cert.TryGetProperty("validityYears", out var v) && v.ValueKind == JsonValueKind.Number ? (int?)v.GetInt32() : null,
                Description: cert.TryGetProperty("description", out var d) ? d.GetString() : null,
                Prerequisites: cert.TryGetProperty("prerequisites", out var pr) 
                    ? pr.EnumerateArray().Select(x => x.GetString() ?? "").ToList() 
                    : new List<string>()
            ));
        }

        return certs;
    }

    private string CleanJsonResponse(string response)
    {
        var cleaned = response.Trim();
        if (cleaned.StartsWith("```json"))
            cleaned = cleaned.Substring(7);
        if (cleaned.StartsWith("```"))
            cleaned = cleaned.Substring(3);
        if (cleaned.EndsWith("```"))
            cleaned = cleaned.Substring(0, cleaned.Length - 3);
        return cleaned.Trim();
    }

    private async Task SaveSkillGapsAsync(
        Guid userId, 
        Guid? jobMatchId, 
        SkillGapAnalysisResultDto result, 
        CancellationToken cancellationToken)
    {
        var allGaps = result.TechnicalSkills
            .Concat(result.Certifications)
            .Concat(result.ExperienceGaps)
            .Concat(result.SoftSkills);

        var entities = allGaps.Select(gap => new SkillGapAnalysis
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            JobMatchId = jobMatchId,
            SkillName = gap.SkillName,
            Category = gap.Category,
            SubCategory = gap.SubCategory,
            ImportanceLevel = gap.ImportanceLevel,
            Description = gap.Description,
            RecommendedCourses = JsonSerializer.Serialize(gap.RecommendedCourses),
            RecommendedProjects = JsonSerializer.Serialize(gap.RecommendedProjects),
            RecommendedCertifications = JsonSerializer.Serialize(gap.RecommendedCertifications),
            EstimatedLearningHours = gap.EstimatedLearningHours,
            Status = "NotStarted",
            ProgressPercentage = 0
        }).ToList();

        // Don't save duplicates
        var existingSkills = await _skillGapRepository.GetByUserIdAsync(userId, cancellationToken);
        var existingSkillNames = existingSkills.Select(s => s.SkillName.ToLower()).ToHashSet();
        
        var newEntities = entities.Where(e => !existingSkillNames.Contains(e.SkillName.ToLower())).ToList();

        if (newEntities.Any())
        {
            await _skillGapRepository.CreateRangeAsync(newEntities, cancellationToken);
        }
    }

    public async Task<List<SkillGapDto>> GetUserSkillGapsAsync(
        Guid userId, 
        SkillGapFilterDto? filter = null, 
        CancellationToken cancellationToken = default)
    {
        var gaps = await _skillGapRepository.GetFilteredAsync(
            userId,
            filter?.Category,
            filter?.Status,
            filter?.MinImportance,
            filter?.JobMatchId,
            filter?.Skip ?? 0,
            filter?.Take ?? 20,
            cancellationToken);

        return gaps.Select(MapToDto).ToList();
    }

    public async Task<SkillGapDto?> GetSkillGapByIdAsync(
        Guid skillGapId, 
        CancellationToken cancellationToken = default)
    {
        var gap = await _skillGapRepository.GetByIdAsync(skillGapId, cancellationToken);
        return gap != null ? MapToDto(gap) : null;
    }

    /// <summary>
    /// Task 17.3: Generate course recommendations
    /// </summary>
    public async Task<List<CourseRecommendationDto>> GetCourseRecommendationsAsync(
        string skillName, 
        string category,
        CancellationToken cancellationToken = default)
    {
        var prompt = $@"Suggest 5 online courses for learning ""{skillName}"" in the ""{category}"" category.

Return courses from Coursera, Udemy, LinkedIn Learning, Udacity, or similar platforms.

Return in this JSON format:
{{
  ""courses"": [
    {{
      ""title"": ""Course Title"",
      ""provider"": ""Platform Name"",
      ""url"": ""https://...(use realistic URL structure)"",
      ""level"": ""Beginner/Intermediate/Advanced"",
      ""estimatedHours"": 20,
      ""price"": 49.99,
      ""rating"": 4.7,
      ""description"": ""What you'll learn""
    }}
  ]
}}

Return ONLY valid JSON.";

        var response = await _geminiService.GenerateContentAsync(prompt);
        
        try
        {
            var cleaned = CleanJsonResponse(response);
            var json = JsonDocument.Parse(cleaned);
            if (json.RootElement.TryGetProperty("courses", out var courses))
            {
                return courses.EnumerateArray().Select(c => new CourseRecommendationDto(
                    Title: c.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "",
                    Provider: c.TryGetProperty("provider", out var p) ? p.GetString() ?? "" : "",
                    Url: c.TryGetProperty("url", out var u) ? u.GetString() ?? "" : "",
                    Level: c.TryGetProperty("level", out var l) ? l.GetString() ?? "" : "",
                    EstimatedHours: c.TryGetProperty("estimatedHours", out var h) ? h.GetInt32() : 0,
                    Price: c.TryGetProperty("price", out var pr) && pr.ValueKind == JsonValueKind.Number ? (decimal?)pr.GetDecimal() : null,
                    Rating: c.TryGetProperty("rating", out var r) && r.ValueKind == JsonValueKind.Number ? (double?)r.GetDouble() : null,
                    Description: c.TryGetProperty("description", out var d) ? d.GetString() : null
                )).ToList();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing course recommendations");
        }

        return new List<CourseRecommendationDto>();
    }

    /// <summary>
    /// Task 17.4: Generate project suggestions
    /// </summary>
    public async Task<List<ProjectSuggestionDto>> GetProjectSuggestionsAsync(
        string skillName, 
        string category,
        CancellationToken cancellationToken = default)
    {
        var prompt = $@"Suggest 3 portfolio projects for demonstrating ""{skillName}"" skill in the ""{category}"" category.

Projects should be practical and impressive for job applications.

Return in this JSON format:
{{
  ""projects"": [
    {{
      ""title"": ""Project Title"",
      ""description"": ""What to build and why it's valuable"",
      ""difficulty"": ""Beginner/Intermediate/Advanced"",
      ""technologies"": [""tech1"", ""tech2""],
      ""estimatedHours"": 15,
      ""githubTemplate"": ""https://github.com/..."" (optional),
      ""learningOutcomes"": ""Skills demonstrated""
    }}
  ]
}}

Return ONLY valid JSON.";

        var response = await _geminiService.GenerateContentAsync(prompt);
        
        try
        {
            var cleaned = CleanJsonResponse(response);
            var json = JsonDocument.Parse(cleaned);
            if (json.RootElement.TryGetProperty("projects", out var projects))
            {
                return projects.EnumerateArray().Select(p => new ProjectSuggestionDto(
                    Title: p.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "",
                    Description: p.TryGetProperty("description", out var d) ? d.GetString() ?? "" : "",
                    Difficulty: p.TryGetProperty("difficulty", out var diff) ? diff.GetString() ?? "" : "",
                    Technologies: p.TryGetProperty("technologies", out var tech) 
                        ? tech.EnumerateArray().Select(x => x.GetString() ?? "").ToList() 
                        : new List<string>(),
                    EstimatedHours: p.TryGetProperty("estimatedHours", out var h) ? h.GetInt32() : 0,
                    GitHubTemplate: p.TryGetProperty("githubTemplate", out var g) ? g.GetString() : null,
                    LearningOutcomes: p.TryGetProperty("learningOutcomes", out var l) ? l.GetString() : null
                )).ToList();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing project suggestions");
        }

        return new List<ProjectSuggestionDto>();
    }

    public async Task<List<CertificationRecommendationDto>> GetCertificationRecommendationsAsync(
        string skillName, 
        string category,
        CancellationToken cancellationToken = default)
    {
        var prompt = $@"Suggest relevant professional certifications for ""{skillName}"" in the ""{category}"" category.

Return in this JSON format:
{{
  ""certifications"": [
    {{
      ""name"": ""Certification Name"",
      ""provider"": ""Certifying Body"",
      ""url"": ""https://..."",
      ""level"": ""Associate/Professional/Expert"",
      ""cost"": 150,
      ""validityYears"": 3,
      ""description"": ""What it validates"",
      ""prerequisites"": [""Prerequisite 1""]
    }}
  ]
}}

Return ONLY valid JSON.";

        var response = await _geminiService.GenerateContentAsync(prompt);
        
        try
        {
            var cleaned = CleanJsonResponse(response);
            var json = JsonDocument.Parse(cleaned);
            if (json.RootElement.TryGetProperty("certifications", out var certs))
            {
                return certs.EnumerateArray().Select(c => new CertificationRecommendationDto(
                    Name: c.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "",
                    Provider: c.TryGetProperty("provider", out var p) ? p.GetString() ?? "" : "",
                    Url: c.TryGetProperty("url", out var u) ? u.GetString() ?? "" : "",
                    Level: c.TryGetProperty("level", out var l) ? l.GetString() ?? "" : "",
                    Cost: c.TryGetProperty("cost", out var co) && co.ValueKind == JsonValueKind.Number ? (decimal?)co.GetDecimal() : null,
                    ValidityYears: c.TryGetProperty("validityYears", out var v) && v.ValueKind == JsonValueKind.Number ? (int?)v.GetInt32() : null,
                    Description: c.TryGetProperty("description", out var d) ? d.GetString() : null,
                    Prerequisites: c.TryGetProperty("prerequisites", out var pr) 
                        ? pr.EnumerateArray().Select(x => x.GetString() ?? "").ToList() 
                        : new List<string>()
                )).ToList();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing certification recommendations");
        }

        return new List<CertificationRecommendationDto>();
    }

    /// <summary>
    /// Task 17.5: Update progress tracking
    /// </summary>
    public async Task<SkillGapDto> UpdateProgressAsync(
        Guid skillGapId, 
        Guid userId,
        UpdateSkillGapProgressDto dto, 
        CancellationToken cancellationToken = default)
    {
        var skillGap = await _skillGapRepository.GetByIdAsync(skillGapId, cancellationToken);
        
        if (skillGap == null)
            throw new InvalidOperationException($"Skill gap not found: {skillGapId}");

        if (skillGap.UserId != userId)
            throw new UnauthorizedAccessException("User does not own this skill gap");

        if (!string.IsNullOrEmpty(dto.Status))
        {
            skillGap.Status = dto.Status;
            
            if (dto.Status == "InProgress" && !skillGap.StartedAt.HasValue)
                skillGap.StartedAt = DateTime.UtcNow;
            
            if (dto.Status == "Completed")
                skillGap.CompletedAt = DateTime.UtcNow;
        }

        if (dto.ProgressPercentage.HasValue)
        {
            skillGap.ProgressPercentage = Math.Clamp(dto.ProgressPercentage.Value, 0, 100);
            
            if (skillGap.ProgressPercentage == 100 && skillGap.Status != "Completed")
            {
                skillGap.Status = "Completed";
                skillGap.CompletedAt = DateTime.UtcNow;
            }
        }

        if (!string.IsNullOrEmpty(dto.Notes))
            skillGap.Notes = dto.Notes;

        await _skillGapRepository.UpdateAsync(skillGap, cancellationToken);

        return MapToDto(skillGap);
    }

    public async Task<SkillGapDto> MarkAsCompletedAsync(
        Guid skillGapId, 
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await UpdateProgressAsync(skillGapId, userId, new UpdateSkillGapProgressDto(
            Status: "Completed",
            ProgressPercentage: 100,
            Notes: null
        ), cancellationToken);
    }

    public async Task<SkillDevelopmentProgressDto> GetDevelopmentProgressAsync(
        Guid userId, 
        CancellationToken cancellationToken = default)
    {
        var statusCounts = await _skillGapRepository.GetStatusCountsAsync(userId, cancellationToken);
        var categoryCounts = await _skillGapRepository.GetCategoryCountsAsync(userId, cancellationToken);
        var recentlyCompleted = await _skillGapRepository.GetRecentlyCompletedAsync(userId, 5, cancellationToken);
        var currentlyLearning = await _skillGapRepository.GetInProgressAsync(userId, 10, cancellationToken);
        var allGaps = await _skillGapRepository.GetByUserIdAsync(userId, cancellationToken);

        var totalGaps = allGaps.Count;
        var completedSkills = statusCounts.GetValueOrDefault("Completed", 0);
        var inProgressSkills = statusCounts.GetValueOrDefault("InProgress", 0);
        var notStartedSkills = statusCounts.GetValueOrDefault("NotStarted", 0);

        var totalHoursEstimated = allGaps.Sum(g => g.EstimatedLearningHours);
        var completedHours = allGaps.Where(g => g.Status == "Completed").Sum(g => g.EstimatedLearningHours);

        var completedByCategory = allGaps
            .Where(g => g.Status == "Completed")
            .GroupBy(g => g.Category)
            .ToDictionary(g => g.Key, g => g.Count());

        return new SkillDevelopmentProgressDto(
            UserId: userId,
            TotalSkillGaps: totalGaps,
            CompletedSkills: completedSkills,
            InProgressSkills: inProgressSkills,
            NotStartedSkills: notStartedSkills,
            OverallProgress: totalGaps > 0 ? (double)completedSkills / totalGaps * 100 : 0,
            TotalLearningHoursEstimated: totalHoursEstimated,
            TotalLearningHoursCompleted: completedHours,
            GapsByCategory: categoryCounts,
            CompletedByCategory: completedByCategory,
            RecentlyCompleted: recentlyCompleted.Select(MapToDto).ToList(),
            CurrentlyLearning: currentlyLearning.Select(MapToDto).ToList()
        );
    }

    public async Task DeleteSkillGapAsync(
        Guid skillGapId, 
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var skillGap = await _skillGapRepository.GetByIdAsync(skillGapId, cancellationToken);
        
        if (skillGap == null)
            throw new InvalidOperationException($"Skill gap not found: {skillGapId}");

        if (skillGap.UserId != userId)
            throw new UnauthorizedAccessException("User does not own this skill gap");

        await _skillGapRepository.DeleteAsync(skillGapId, cancellationToken);
    }

    public async Task<decimal> RecalculateMatchScoreAsync(
        Guid userId, 
        Guid jobMatchId, 
        CancellationToken cancellationToken = default)
    {
        // Get skill gaps for this job match
        var skillGaps = await _skillGapRepository.GetByJobMatchIdAsync(jobMatchId, cancellationToken);
        
        if (!skillGaps.Any())
            return 0;

        // Calculate completion weighted by importance
        var totalWeight = skillGaps.Sum(g => g.ImportanceLevel);
        var completedWeight = skillGaps.Where(g => g.Status == "Completed").Sum(g => g.ImportanceLevel);
        
        var improvementFactor = totalWeight > 0 ? (decimal)completedWeight / totalWeight : 0;

        // Get original match
        var jobMatch = await _dbContext.JobMatches.FindAsync(new object[] { jobMatchId }, cancellationToken);
        
        if (jobMatch == null)
            return 0;

        // Calculate new score (original score + improvement up to 100)
        var potentialImprovement = 100 - jobMatch.MatchScore;
        var actualImprovement = potentialImprovement * improvementFactor;
        var newScore = Math.Min(100, jobMatch.MatchScore + actualImprovement);

        _logger.LogInformation("Recalculated match score for {JobMatchId}: {OldScore} -> {NewScore}", 
            jobMatchId, jobMatch.MatchScore, newScore);

        return newScore;
    }

    private SkillGapDto MapToDto(SkillGapAnalysis entity)
    {
        return new SkillGapDto(
            Id: entity.Id,
            SkillName: entity.SkillName,
            Category: entity.Category,
            SubCategory: entity.SubCategory,
            ImportanceLevel: entity.ImportanceLevel,
            Description: entity.Description,
            RecommendedCourses: ParseJsonList<CourseRecommendationDto>(entity.RecommendedCourses),
            RecommendedProjects: ParseJsonList<ProjectSuggestionDto>(entity.RecommendedProjects),
            RecommendedCertifications: ParseJsonList<CertificationRecommendationDto>(entity.RecommendedCertifications),
            EstimatedLearningHours: entity.EstimatedLearningHours,
            Status: entity.Status,
            ProgressPercentage: entity.ProgressPercentage,
            StartedAt: entity.StartedAt,
            CompletedAt: entity.CompletedAt,
            CreatedAt: entity.CreatedAt
        );
    }

    private List<T> ParseJsonList<T>(string? json)
    {
        if (string.IsNullOrEmpty(json))
            return new List<T>();

        try
        {
            return JsonSerializer.Deserialize<List<T>>(json) ?? new List<T>();
        }
        catch
        {
            return new List<T>();
        }
    }
}

