using Pgvector;

namespace DistroCv.Core.Entities;

/// <summary>
/// Represents a digital twin of a candidate's resume, skills, and preferences
/// </summary>
public class DigitalTwin
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string? OriginalResumeUrl { get; set; } // S3 URL
    public string? ParsedResumeJson { get; set; } // Structured JSON
    public Vector? EmbeddingVector { get; set; } // pgvector
    public string? Skills { get; set; } // JSON array
    public string? Experience { get; set; } // JSON array
    public string? Education { get; set; } // JSON array
    public string? CareerGoals { get; set; }
    public string? Preferences { get; set; } // JSON: sectors, locations, salary range
    
    // Task 20.2: Sector & Geographic Filtering fields
    public string? PreferredSectors { get; set; } // JSON array of Sector enum values
    public string? PreferredCities { get; set; } // JSON array of TurkeyCity enum values
    public decimal? MinSalary { get; set; }
    public decimal? MaxSalary { get; set; }
    public bool IsRemotePreferred { get; set; } = false;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User User { get; set; } = null!;
}
