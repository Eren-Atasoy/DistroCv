namespace DistroCv.Core.DTOs;

public record DigitalTwinDto(
    Guid Id,
    Guid UserId,
    string? OriginalResumeUrl,
    string? Skills,
    string? Experience,
    string? Education,
    string? CareerGoals,
    string? Preferences,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record UpdatePreferencesDto(
    string? Sectors,
    string? Locations,
    string? SalaryRange,
    string? CareerGoals
);

public record ResumeUploadResponseDto(
    Guid DigitalTwinId,
    string Message,
    string? ParsedData
);
