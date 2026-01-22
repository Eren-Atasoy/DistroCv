namespace DistroCv.Core.DTOs;

/// <summary>
/// DTO for creating a new verified company
/// </summary>
public record CreateVerifiedCompanyDto(
    string Name,
    string? Website,
    string? TaxNumber,
    string? HREmail,
    string? HRPhone,
    string? Sector,
    string? City,
    string? Description
);

/// <summary>
/// DTO for updating a verified company
/// </summary>
public record UpdateVerifiedCompanyDto(
    string? Name,
    string? Website,
    string? TaxNumber,
    string? HREmail,
    string? HRPhone,
    string? Sector,
    string? City,
    string? Description,
    bool? IsVerified
);

/// <summary>
/// DTO for verified company response
/// </summary>
public record VerifiedCompanyDto(
    Guid Id,
    string Name,
    string? Website,
    string? TaxNumber,
    string? HREmail,
    string? HRPhone,
    string? Sector,
    string? City,
    string? Description,
    string? CompanyCulture,
    string? RecentNews,
    bool IsVerified,
    DateTime? VerifiedAt,
    DateTime UpdatedAt
);

/// <summary>
/// DTO for company verification request
/// </summary>
public record VerifyCompanyDto(
    string? TaxNumber,
    string? HREmail,
    string? Website
);

/// <summary>
/// DTO for company culture analysis result
/// </summary>
public record CompanyCultureAnalysisDto(
    string Culture,
    string Values,
    string WorkEnvironment,
    string Benefits,
    string CareerGrowth,
    double OverallScore
);

/// <summary>
/// DTO for company news item
/// </summary>
public record CompanyNewsDto(
    string Title,
    string Summary,
    string Source,
    string? Url,
    DateTime PublishedAt
);

/// <summary>
/// DTO for company search/filter
/// </summary>
public record CompanyFilterDto(
    string? SearchTerm,
    string? Sector,
    string? City,
    bool? IsVerified,
    int Skip = 0,
    int Take = 20
);

