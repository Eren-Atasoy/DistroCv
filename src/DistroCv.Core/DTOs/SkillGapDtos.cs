namespace DistroCv.Core.DTOs;

/// <summary>
/// DTO for skill gap analysis result
/// </summary>
public record SkillGapDto(
    Guid Id,
    string SkillName,
    string Category,
    string SubCategory,
    int ImportanceLevel,
    string? Description,
    List<CourseRecommendationDto> RecommendedCourses,
    List<ProjectSuggestionDto> RecommendedProjects,
    List<CertificationRecommendationDto> RecommendedCertifications,
    int EstimatedLearningHours,
    string Status,
    int ProgressPercentage,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    DateTime CreatedAt
);

/// <summary>
/// DTO for course recommendation
/// </summary>
public record CourseRecommendationDto(
    string Title,
    string Provider, // Coursera, Udemy, LinkedIn Learning, etc.
    string Url,
    string Level, // Beginner, Intermediate, Advanced
    int EstimatedHours,
    decimal? Price,
    double? Rating,
    string? Description
);

/// <summary>
/// DTO for project suggestion
/// </summary>
public record ProjectSuggestionDto(
    string Title,
    string Description,
    string Difficulty, // Beginner, Intermediate, Advanced
    List<string> Technologies,
    int EstimatedHours,
    string? GitHubTemplate,
    string? LearningOutcomes
);

/// <summary>
/// DTO for certification recommendation
/// </summary>
public record CertificationRecommendationDto(
    string Name,
    string Provider,
    string Url,
    string Level, // Associate, Professional, Expert
    decimal? Cost,
    int? ValidityYears,
    string? Description,
    List<string> Prerequisites
);

/// <summary>
/// DTO for creating a skill gap entry
/// </summary>
public record CreateSkillGapDto(
    string SkillName,
    string Category,
    string SubCategory,
    int ImportanceLevel,
    string? Description,
    Guid? JobMatchId
);

/// <summary>
/// DTO for updating skill gap progress
/// </summary>
public record UpdateSkillGapProgressDto(
    string? Status,
    int? ProgressPercentage,
    string? Notes
);

/// <summary>
/// DTO for complete skill gap analysis
/// </summary>
public record SkillGapAnalysisResultDto(
    List<SkillGapDto> TechnicalSkills,
    List<SkillGapDto> Certifications,
    List<SkillGapDto> ExperienceGaps,
    List<SkillGapDto> SoftSkills,
    int TotalGaps,
    int CompletedGaps,
    int InProgressGaps,
    double OverallReadinessScore, // 0-100
    string Summary,
    List<string> PriorityRecommendations
);

/// <summary>
/// DTO for skill gap filter
/// </summary>
public record SkillGapFilterDto(
    string? Category,
    string? Status,
    int? MinImportance,
    Guid? JobMatchId,
    int Skip = 0,
    int Take = 20
);

/// <summary>
/// DTO for skill development progress
/// </summary>
public record SkillDevelopmentProgressDto(
    Guid UserId,
    int TotalSkillGaps,
    int CompletedSkills,
    int InProgressSkills,
    int NotStartedSkills,
    double OverallProgress,
    int TotalLearningHoursEstimated,
    int TotalLearningHoursCompleted,
    Dictionary<string, int> GapsByCategory,
    Dictionary<string, int> CompletedByCategory,
    List<SkillGapDto> RecentlyCompleted,
    List<SkillGapDto> CurrentlyLearning
);

/// <summary>
/// DTO for Gemini skill analysis request
/// </summary>
public record SkillAnalysisRequestDto(
    string UserSkills,
    string UserExperience,
    string UserEducation,
    string JobRequirements,
    string JobTitle,
    string CompanyName
);

