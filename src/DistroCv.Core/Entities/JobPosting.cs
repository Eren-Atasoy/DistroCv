using Pgvector;

namespace DistroCv.Core.Entities;

/// <summary>
/// Represents a scraped job posting from various platforms
/// </summary>
public class JobPosting
{
    public Guid Id { get; set; }
    public string? ExternalId { get; set; } // LinkedIn/Indeed ID
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public Guid? VerifiedCompanyId { get; set; }
    public string? Location { get; set; }
    public string? Sector { get; set; }
    public string? SalaryRange { get; set; }
    public string? Requirements { get; set; } // JSON
    public Vector? EmbeddingVector { get; set; } // pgvector
    public string SourcePlatform { get; set; } = string.Empty; // "LinkedIn", "Indeed"
    public string? SourceUrl { get; set; }
    public DateTime ScrapedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public VerifiedCompany? VerifiedCompany { get; set; }
    public ICollection<JobMatch> Matches { get; set; } = new List<JobMatch>();
    public ICollection<Application> Applications { get; set; } = new List<Application>();
}
