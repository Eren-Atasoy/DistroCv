using System;
using System.Collections.Generic;

namespace DistroCv.Core.Entities;

/// <summary>
/// Represents an in-app survey for collecting user feedback
/// </summary>
public class Survey
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public SurveyType Type { get; set; } = SurveyType.General;
    
    // Targeting
    public SurveyTargetAudience TargetAudience { get; set; } = SurveyTargetAudience.All;
    public string? TargetFeature { get; set; } // Show after using specific feature
    public int? MinUserAge { get; set; } // Days since registration
    public int? MinSessionsCompleted { get; set; }
    
    // Schedule
    public bool IsActive { get; set; } = true;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int MaxResponses { get; set; } = 0; // 0 = unlimited
    
    // Display settings
    public SurveyTrigger Trigger { get; set; } = SurveyTrigger.Manual;
    public int DisplayDelaySeconds { get; set; } = 5;
    public bool CanDismiss { get; set; } = true;
    public bool ShowProgressBar { get; set; } = true;
    public int? CooldownDays { get; set; } = 7; // Don't show again for X days
    
    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    // Statistics
    public int TotalResponses { get; set; }
    public int TotalViews { get; set; }
    public decimal CompletionRate { get; set; }
    
    // Navigation
    public virtual ICollection<SurveyQuestion> Questions { get; set; } = new List<SurveyQuestion>();
    public virtual ICollection<SurveyResponse> Responses { get; set; } = new List<SurveyResponse>();
}

public class SurveyQuestion
{
    public Guid Id { get; set; }
    public Guid SurveyId { get; set; }
    public int Order { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string? QuestionTextTr { get; set; } // Turkish translation
    public QuestionType Type { get; set; } = QuestionType.Text;
    public bool IsRequired { get; set; } = true;
    
    // Options for multiple choice questions
    public List<string> Options { get; set; } = new();
    public List<string>? OptionsTr { get; set; }
    
    // Validation
    public int? MinLength { get; set; }
    public int? MaxLength { get; set; }
    public int? MinValue { get; set; } // For rating/number
    public int? MaxValue { get; set; }
    
    // Conditional logic
    public Guid? DependsOnQuestionId { get; set; }
    public string? DependsOnAnswer { get; set; }
    
    public virtual Survey? Survey { get; set; }
}

public class SurveyResponse
{
    public Guid Id { get; set; }
    public Guid SurveyId { get; set; }
    public Guid? UserId { get; set; }
    public Guid? BetaTesterId { get; set; }
    
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public bool IsCompleted { get; set; }
    
    // Context
    public string? Browser { get; set; }
    public string? OperatingSystem { get; set; }
    public string? PageUrl { get; set; }
    
    public virtual Survey? Survey { get; set; }
    public virtual ICollection<SurveyAnswer> Answers { get; set; } = new List<SurveyAnswer>();
}

public class SurveyAnswer
{
    public Guid Id { get; set; }
    public Guid SurveyResponseId { get; set; }
    public Guid SurveyQuestionId { get; set; }
    
    public string? TextAnswer { get; set; }
    public int? NumberAnswer { get; set; }
    public List<string>? SelectedOptions { get; set; }
    public bool? BooleanAnswer { get; set; }
    
    public virtual SurveyResponse? SurveyResponse { get; set; }
    public virtual SurveyQuestion? SurveyQuestion { get; set; }
}

public enum SurveyType
{
    General = 0,
    NPS = 1,           // Net Promoter Score
    CSAT = 2,          // Customer Satisfaction
    CES = 3,           // Customer Effort Score
    FeatureFeedback = 4,
    Onboarding = 5,
    Exit = 6
}

public enum SurveyTargetAudience
{
    All = 0,
    BetaTesters = 1,
    NewUsers = 2,        // < 7 days
    ActiveUsers = 3,     // > 10 sessions
    InactiveUsers = 4    // No activity > 14 days
}

public enum SurveyTrigger
{
    Manual = 0,          // Admin triggers
    OnPageLoad = 1,      // When page loads
    AfterAction = 2,     // After specific action
    OnExit = 3,          // Exit intent
    Scheduled = 4        // At specific time
}

public enum QuestionType
{
    Text = 0,
    TextArea = 1,
    SingleChoice = 2,
    MultipleChoice = 3,
    Rating = 4,          // 1-5 stars
    Scale = 5,           // 1-10
    NPS = 6,             // 0-10 with promoter/detractor
    Boolean = 7,         // Yes/No
    Date = 8,
    Email = 9
}

