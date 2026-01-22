namespace DistroCv.Core.DTOs;

public record ApplicationDto(
    Guid Id,
    Guid JobPostingId,
    JobPostingDto? JobPosting,
    string? TailoredResumeUrl,
    string? CoverLetter,
    string? CustomMessage,
    string DistributionMethod,
    string Status,
    DateTime CreatedAt,
    DateTime? SentAt,
    DateTime? ViewedAt,
    DateTime? RespondedAt
);

public record CreateApplicationDto(
    Guid JobMatchId,
    string DistributionMethod,
    string? CustomMessage
);

public record UpdateApplicationDto(
    string? TailoredResumeContent,
    string? CoverLetter,
    string? CustomMessage
);

public record SendApplicationDto(
    bool ConfirmSend
);

public record ApplicationLogDto(
    Guid Id,
    string ActionType,
    string? TargetElement,
    string? Details,
    string? ScreenshotUrl,
    DateTime Timestamp
);
