namespace DistroCv.Core.Entities;

/// <summary>
/// Represents a skill gap analysis for a user
/// Tracks skills the user needs to develop to reach their career goals
/// </summary>
public class SkillGapAnalysis
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? JobMatchId { get; set; } // Optional link to specific job match
    
    // Skill gap details
    public string SkillName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // "Technical", "Certification", "Experience", "SoftSkill"
    public string SubCategory { get; set; } = string.Empty; // e.g., "Programming", "Cloud", "Database"
    public int ImportanceLevel { get; set; } // 1-5, 5 being most important
    public string? Description { get; set; }
    
    // Learning resources
    public string? RecommendedCourses { get; set; } // JSON array of course recommendations
    public string? RecommendedProjects { get; set; } // JSON array of project suggestions
    public string? RecommendedCertifications { get; set; } // JSON array of certifications
    public int EstimatedLearningHours { get; set; }
    
    // Progress tracking
    public string Status { get; set; } = "NotStarted"; // "NotStarted", "InProgress", "Completed"
    public int ProgressPercentage { get; set; } = 0;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Notes { get; set; }
    
    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation
    public User User { get; set; } = null!;
    public JobMatch? JobMatch { get; set; }
}

