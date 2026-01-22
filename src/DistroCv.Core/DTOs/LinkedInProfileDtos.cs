namespace DistroCv.Core.DTOs;

/// <summary>
/// Request to analyze a LinkedIn profile
/// </summary>
public record LinkedInProfileAnalysisRequest(
    string LinkedInUrl,
    List<string>? TargetJobTitles = null,
    List<string>? TargetIndustries = null
);

/// <summary>
/// Scraped LinkedIn profile data
/// </summary>
public record LinkedInProfileData(
    string ProfileUrl,
    string? Name,
    string? Headline,
    string? About,
    List<LinkedInExperience> Experience,
    List<LinkedInEducation> Education,
    List<string> Skills,
    string? Location,
    int? ConnectionCount,
    string? ProfileImageUrl
);

/// <summary>
/// LinkedIn experience entry
/// </summary>
public record LinkedInExperience(
    string Title,
    string Company,
    string? Duration,
    string? Location,
    string? Description,
    bool IsCurrent
);

/// <summary>
/// LinkedIn education entry
/// </summary>
public record LinkedInEducation(
    string School,
    string? Degree,
    string? FieldOfStudy,
    string? Duration
);

/// <summary>
/// Profile optimization result
/// </summary>
public record LinkedInOptimizationResultDto(
    Guid Id,
    string LinkedInUrl,
    int ProfileScore,
    ProfileScoreBreakdownDto ScoreBreakdown,
    OriginalProfileDto OriginalProfile,
    OptimizedProfileDto OptimizedProfile,
    List<string> ImprovementAreas,
    List<string> ATSKeywords,
    SEOAnalysisDto SEOAnalysis,
    DateTime AnalyzedAt
);

/// <summary>
/// Profile score breakdown by section
/// </summary>
public record ProfileScoreBreakdownDto(
    int HeadlineScore,
    int AboutScore,
    int ExperienceScore,
    int SkillsScore,
    int EducationScore,
    int OverallScore
);

/// <summary>
/// Original profile sections
/// </summary>
public record OriginalProfileDto(
    string? Headline,
    string? About,
    List<LinkedInExperience> Experience,
    List<string> Skills,
    List<LinkedInEducation> Education
);

/// <summary>
/// Optimized profile suggestions
/// </summary>
public record OptimizedProfileDto(
    string? Headline,
    string? About,
    List<OptimizedExperienceDto> Experience,
    List<string> SuggestedSkills
);

/// <summary>
/// Optimized experience entry with improvements
/// </summary>
public record OptimizedExperienceDto(
    string OriginalDescription,
    string OptimizedDescription,
    List<string> AddedKeywords,
    List<string> ImprovementNotes
);

/// <summary>
/// SEO analysis results
/// </summary>
public record SEOAnalysisDto(
    double Searchability,
    double KeywordDensity,
    double ProfileCompleteness,
    List<string> MissingKeywords,
    List<string> StrongKeywords
);

/// <summary>
/// Comparison view data
/// </summary>
public record ProfileComparisonDto(
    string SectionName,
    string OriginalContent,
    string OptimizedContent,
    List<string> Changes,
    int ImprovementScore
);

/// <summary>
/// Profile optimization history entry
/// </summary>
public record ProfileOptimizationHistoryDto(
    Guid Id,
    string LinkedInUrl,
    int ProfileScore,
    string Status,
    DateTime CreatedAt,
    DateTime? AnalyzedAt
);

