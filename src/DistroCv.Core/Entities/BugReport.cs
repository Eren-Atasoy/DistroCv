using System;
using System.Collections.Generic;

namespace DistroCv.Core.Entities;

/// <summary>
/// Represents a bug report submitted by users or beta testers
/// </summary>
public class BugReport
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public Guid? BetaTesterId { get; set; }
    
    // Bug details
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? StepsToReproduce { get; set; }
    public string? ExpectedBehavior { get; set; }
    public string? ActualBehavior { get; set; }
    
    // Classification
    public BugSeverity Severity { get; set; } = BugSeverity.Medium;
    public BugPriority Priority { get; set; } = BugPriority.P2;
    public BugCategory Category { get; set; } = BugCategory.Other;
    public BugStatus Status { get; set; } = BugStatus.New;
    
    // Technical context
    public string? Browser { get; set; }
    public string? OperatingSystem { get; set; }
    public string? DeviceType { get; set; } // Desktop, Mobile, Tablet
    public string? ScreenResolution { get; set; }
    public string? AppVersion { get; set; }
    public string? PageUrl { get; set; }
    public string? ConsoleErrors { get; set; }
    public string? NetworkLogs { get; set; }
    
    // Attachments
    public string? ScreenshotUrl { get; set; }
    public string? VideoUrl { get; set; }
    public List<string> AttachmentUrls { get; set; } = new();
    
    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    
    // Resolution
    public string? Resolution { get; set; }
    public string? AssignedTo { get; set; }
    public string? FixVersion { get; set; }
    
    // Tracking
    public int DuplicateOfId { get; set; }
    public int VoteCount { get; set; }
    public bool IsVerified { get; set; }
    
    // Navigation
    public virtual User? User { get; set; }
    public virtual BetaTester? BetaTester { get; set; }
    public virtual ICollection<BugReportComment> Comments { get; set; } = new List<BugReportComment>();
}

public class BugReportComment
{
    public Guid Id { get; set; }
    public Guid BugReportId { get; set; }
    public Guid? UserId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsInternal { get; set; } // Only visible to team
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public virtual BugReport? BugReport { get; set; }
}

public enum BugSeverity
{
    Critical = 0,  // System crash, data loss
    High = 1,      // Major feature broken
    Medium = 2,    // Feature works but with issues
    Low = 3,       // Minor cosmetic issues
    Trivial = 4    // Very minor issues
}

public enum BugPriority
{
    P0 = 0,  // Fix immediately (blocking)
    P1 = 1,  // Fix within 24 hours
    P2 = 2,  // Fix within 1 week
    P3 = 3,  // Fix in next release
    P4 = 4   // Nice to have
}

public enum BugCategory
{
    Authentication = 0,
    ResumeUpload = 1,
    JobMatching = 2,
    ApplicationSending = 3,
    InterviewPrep = 4,
    Dashboard = 5,
    ProfileManagement = 6,
    Notifications = 7,
    Performance = 8,
    UI_UX = 9,
    Localization = 10,
    Integration = 11,
    Security = 12,
    Other = 99
}

public enum BugStatus
{
    New = 0,
    Confirmed = 1,
    InProgress = 2,
    Testing = 3,
    Resolved = 4,
    Closed = 5,
    WontFix = 6,
    Duplicate = 7,
    CannotReproduce = 8
}

