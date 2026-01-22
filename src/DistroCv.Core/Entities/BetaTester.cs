using System;
using System.Collections.Generic;

namespace DistroCv.Core.Entities;

/// <summary>
/// Represents a beta tester in the system
/// </summary>
public class BetaTester
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; } // Linked to User if registered
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    
    // Demographics for diverse testing
    public string? Industry { get; set; }
    public string? JobTitle { get; set; }
    public int? YearsOfExperience { get; set; }
    public string? Location { get; set; } // City/Country
    public string? TechProficiency { get; set; } // Beginner, Intermediate, Advanced
    
    // Beta test tracking
    public BetaTesterStatus Status { get; set; } = BetaTesterStatus.Pending;
    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ApprovedAt { get; set; }
    public DateTime? LastActiveAt { get; set; }
    public string? InviteCode { get; set; }
    
    // Engagement metrics
    public int BugReportsSubmitted { get; set; }
    public int FeedbackSubmitted { get; set; }
    public int SurveysCompleted { get; set; }
    public int FeatureRequestsSubmitted { get; set; }
    public int TotalSessionsCount { get; set; }
    public TimeSpan TotalTimeSpent { get; set; }
    
    // Communication preferences
    public bool ReceiveUpdates { get; set; } = true;
    public bool ReceiveSurveys { get; set; } = true;
    public string? PreferredLanguage { get; set; } = "tr";
    
    // Notes
    public string? Notes { get; set; }
    public string? RejectionReason { get; set; }
    
    // Navigation
    public virtual User? User { get; set; }
    public virtual ICollection<BugReport> BugReports { get; set; } = new List<BugReport>();
    public virtual ICollection<FeatureRequest> FeatureRequests { get; set; } = new List<FeatureRequest>();
    public virtual ICollection<SurveyResponse> SurveyResponses { get; set; } = new List<SurveyResponse>();
}

public enum BetaTesterStatus
{
    Pending = 0,      // Applied, waiting for review
    Approved = 1,     // Accepted to beta program
    Active = 2,       // Currently participating
    Inactive = 3,     // Not active for 14+ days
    Completed = 4,    // Completed beta testing period
    Rejected = 5      // Application rejected
}

