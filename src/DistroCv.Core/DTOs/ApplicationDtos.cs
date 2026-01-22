using System.ComponentModel.DataAnnotations;

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
    [Required(ErrorMessage = "Job match ID is required")]
    Guid JobMatchId,
    
    [Required(ErrorMessage = "Distribution method is required")]
    [RegularExpression(@"^(Email|LinkedIn)$", ErrorMessage = "Distribution method must be 'Email' or 'LinkedIn'")]
    string DistributionMethod,
    
    [StringLength(2000, ErrorMessage = "Custom message cannot exceed 2000 characters")]
    string? CustomMessage
);

public record UpdateApplicationDto(
    [StringLength(50000, ErrorMessage = "Tailored resume content cannot exceed 50000 characters")]
    string? TailoredResumeContent,
    
    [StringLength(5000, ErrorMessage = "Cover letter cannot exceed 5000 characters")]
    string? CoverLetter,
    
    [StringLength(2000, ErrorMessage = "Custom message cannot exceed 2000 characters")]
    string? CustomMessage
);

public record SendApplicationDto(
    [Required(ErrorMessage = "Send confirmation is required")]
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
