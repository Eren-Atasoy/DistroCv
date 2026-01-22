namespace DistroCv.Core.Entities;

/// <summary>
/// Represents a verified company with HR contact information
/// </summary>
public class VerifiedCompany
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Website { get; set; }
    public string? TaxNumber { get; set; }
    public string? HREmail { get; set; }
    public string? HRPhone { get; set; }
    public string? CompanyCulture { get; set; } // Gemini analysis
    public string? RecentNews { get; set; } // JSON array
    public bool IsVerified { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<JobPosting> JobPostings { get; set; } = new List<JobPosting>();
}
