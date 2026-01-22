namespace DistroCv.Core.Entities;

/// <summary>
/// Represents a LinkedIn profile optimization analysis
/// </summary>
public class LinkedInProfileOptimization
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    
    // Original profile data
    public string LinkedInUrl { get; set; } = string.Empty;
    public string? OriginalHeadline { get; set; }
    public string? OriginalAbout { get; set; }
    public string? OriginalExperience { get; set; } // JSON array
    public string? OriginalSkills { get; set; } // JSON array
    public string? OriginalEducation { get; set; } // JSON array
    
    // Optimized suggestions
    public string? OptimizedHeadline { get; set; }
    public string? OptimizedAbout { get; set; }
    public string? OptimizedExperience { get; set; } // JSON array
    public string? OptimizedSkills { get; set; } // JSON array
    public string? KeywordSuggestions { get; set; } // JSON array
    
    // Analysis results
    public int ProfileScore { get; set; } // 0-100
    public string? ScoreBreakdown { get; set; } // JSON: { headline: 15, about: 25, experience: 30, skills: 20, education: 10 }
    public string? ImprovementAreas { get; set; } // JSON array of improvement suggestions
    public string? ATSKeywords { get; set; } // JSON array of ATS-friendly keywords
    public string? SEOAnalysis { get; set; } // JSON: { searchability, keyword_density, completeness }
    
    // Target job info for optimization
    public string? TargetJobTitles { get; set; } // JSON array
    public string? TargetIndustries { get; set; } // JSON array
    
    // Status
    public string Status { get; set; } = "Pending"; // "Pending", "Analyzing", "Completed", "Failed"
    public string? ErrorMessage { get; set; }
    
    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? AnalyzedAt { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation
    public User User { get; set; } = null!;
}

