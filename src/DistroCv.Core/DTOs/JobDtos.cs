namespace DistroCv.Core.DTOs;

public record JobPostingDto(
    Guid Id,
    string Title,
    string Description,
    string CompanyName,
    string? Location,
    string? Sector,
    string? SalaryRange,
    string SourcePlatform,
    string? SourceUrl,
    DateTime ScrapedAt,
    bool IsActive
);

public record JobMatchDto(
    Guid Id,
    Guid JobPostingId,
    string JobTitle,
    string CompanyName,
    string? Location,
    string? SalaryRange,
    decimal MatchScore,
    string? MatchReasoning,
    string? SkillGaps,
    string Status,
    DateTime CalculatedAt
);

public record JobFeedbackDto(
    string Reason,
    string? AdditionalNotes = null
);

public record JobSearchFilterDto(
    string? Sector,
    string? Location,
    decimal? MinMatchScore,
    int Skip = 0,
    int Take = 20
);
