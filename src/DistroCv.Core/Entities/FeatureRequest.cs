using System;
using System.Collections.Generic;

namespace DistroCv.Core.Entities;

/// <summary>
/// Represents a feature request submitted by users or beta testers
/// </summary>
public class FeatureRequest
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public Guid? BetaTesterId { get; set; }
    
    // Feature details
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? UseCase { get; set; } // Why do you need this?
    public string? ExpectedBehavior { get; set; }
    public string? AlternativeSolutions { get; set; }
    
    // Classification
    public FeatureCategory Category { get; set; } = FeatureCategory.Other;
    public FeaturePriority Priority { get; set; } = FeaturePriority.Medium;
    public FeatureStatus Status { get; set; } = FeatureStatus.Submitted;
    public FeatureComplexity Complexity { get; set; } = FeatureComplexity.Medium;
    
    // Impact assessment
    public int VoteCount { get; set; }
    public int CommentCount { get; set; }
    public decimal? EstimatedEffort { get; set; } // Story points or hours
    public decimal? BusinessValue { get; set; } // 1-10 scale
    
    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? PlannedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    
    // Implementation
    public string? AssignedTo { get; set; }
    public string? TargetVersion { get; set; }
    public string? RelatedFeatures { get; set; }
    
    // Admin notes
    public string? InternalNotes { get; set; }
    public string? RejectionReason { get; set; }
    
    // Navigation
    public virtual User? User { get; set; }
    public virtual BetaTester? BetaTester { get; set; }
    public virtual ICollection<FeatureRequestComment> Comments { get; set; } = new List<FeatureRequestComment>();
    public virtual ICollection<FeatureVote> Votes { get; set; } = new List<FeatureVote>();
}

public class FeatureRequestComment
{
    public Guid Id { get; set; }
    public Guid FeatureRequestId { get; set; }
    public Guid? UserId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsOfficial { get; set; } // Response from team
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public virtual FeatureRequest? FeatureRequest { get; set; }
}

public class FeatureVote
{
    public Guid Id { get; set; }
    public Guid FeatureRequestId { get; set; }
    public Guid UserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public virtual FeatureRequest? FeatureRequest { get; set; }
}

public enum FeatureCategory
{
    ResumeManagement = 0,
    JobMatching = 1,
    ApplicationProcess = 2,
    InterviewPreparation = 3,
    Analytics = 4,
    Notifications = 5,
    Integration = 6,
    Localization = 7,
    Accessibility = 8,
    Performance = 9,
    Security = 10,
    MobileApp = 11,
    Automation = 12,
    AIFeatures = 13,
    Other = 99
}

public enum FeaturePriority
{
    Critical = 0,  // Must have
    High = 1,      // Should have
    Medium = 2,    // Could have
    Low = 3        // Won't have (this release)
}

public enum FeatureStatus
{
    Submitted = 0,
    UnderReview = 1,
    Planned = 2,
    InProgress = 3,
    Testing = 4,
    Completed = 5,
    Rejected = 6,
    Deferred = 7
}

public enum FeatureComplexity
{
    Trivial = 0,   // < 1 day
    Low = 1,       // 1-2 days
    Medium = 2,    // 3-5 days
    High = 3,      // 1-2 weeks
    Complex = 4    // > 2 weeks
}

