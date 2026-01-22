using System;
using System.Collections.Generic;
using DistroCv.Core.Entities;

namespace DistroCv.Core.DTOs;

#region Beta Tester DTOs

public class BetaTesterApplicationDto
{
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Industry { get; set; }
    public string? JobTitle { get; set; }
    public int? YearsOfExperience { get; set; }
    public string? Location { get; set; }
    public string? TechProficiency { get; set; }
    public string? WhyJoinBeta { get; set; }
    public bool ReceiveUpdates { get; set; } = true;
    public bool ReceiveSurveys { get; set; } = true;
    public string? PreferredLanguage { get; set; } = "tr";
}

public class BetaTesterResponseDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Industry { get; set; }
    public string? JobTitle { get; set; }
    public BetaTesterStatus Status { get; set; }
    public DateTime AppliedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? LastActiveAt { get; set; }
    public int BugReportsSubmitted { get; set; }
    public int FeedbackSubmitted { get; set; }
    public int SurveysCompleted { get; set; }
    public int TotalSessionsCount { get; set; }
}

public class BetaTesterStatsDto
{
    public int TotalApplications { get; set; }
    public int PendingApplications { get; set; }
    public int ApprovedTesters { get; set; }
    public int ActiveTesters { get; set; }
    public int InactiveTesters { get; set; }
    public int TotalBugReports { get; set; }
    public int TotalFeatureRequests { get; set; }
    public int TotalSurveyResponses { get; set; }
    public decimal AverageEngagementScore { get; set; }
    public Dictionary<string, int> TestersByIndustry { get; set; } = new();
    public Dictionary<string, int> TestersByLocation { get; set; } = new();
}

#endregion

#region Bug Report DTOs

public class CreateBugReportDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? StepsToReproduce { get; set; }
    public string? ExpectedBehavior { get; set; }
    public string? ActualBehavior { get; set; }
    public BugSeverity Severity { get; set; } = BugSeverity.Medium;
    public BugCategory Category { get; set; } = BugCategory.Other;
    public string? Browser { get; set; }
    public string? OperatingSystem { get; set; }
    public string? DeviceType { get; set; }
    public string? ScreenResolution { get; set; }
    public string? PageUrl { get; set; }
    public string? ConsoleErrors { get; set; }
    public string? ScreenshotUrl { get; set; }
}

public class BugReportResponseDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? StepsToReproduce { get; set; }
    public BugSeverity Severity { get; set; }
    public BugPriority Priority { get; set; }
    public BugCategory Category { get; set; }
    public BugStatus Status { get; set; }
    public string? ReporterName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? Resolution { get; set; }
    public string? AssignedTo { get; set; }
    public int VoteCount { get; set; }
    public int CommentCount { get; set; }
    public string? ScreenshotUrl { get; set; }
}

public class UpdateBugReportDto
{
    public BugPriority? Priority { get; set; }
    public BugStatus? Status { get; set; }
    public string? AssignedTo { get; set; }
    public string? Resolution { get; set; }
    public string? FixVersion { get; set; }
}

public class BugReportStatsDto
{
    public int TotalBugs { get; set; }
    public int OpenBugs { get; set; }
    public int ResolvedBugs { get; set; }
    public int CriticalBugs { get; set; }
    public int P0Bugs { get; set; }
    public int P1Bugs { get; set; }
    public decimal AverageResolutionTimeHours { get; set; }
    public Dictionary<BugCategory, int> BugsByCategory { get; set; } = new();
    public Dictionary<BugStatus, int> BugsByStatus { get; set; } = new();
    public List<BugReportResponseDto> RecentBugs { get; set; } = new();
}

#endregion

#region Feature Request DTOs

public class CreateFeatureRequestDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? UseCase { get; set; }
    public string? ExpectedBehavior { get; set; }
    public FeatureCategory Category { get; set; } = FeatureCategory.Other;
}

public class FeatureRequestResponseDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? UseCase { get; set; }
    public FeatureCategory Category { get; set; }
    public FeaturePriority Priority { get; set; }
    public FeatureStatus Status { get; set; }
    public string? RequesterName { get; set; }
    public DateTime CreatedAt { get; set; }
    public int VoteCount { get; set; }
    public int CommentCount { get; set; }
    public string? TargetVersion { get; set; }
    public bool HasVoted { get; set; }
}

public class UpdateFeatureRequestDto
{
    public FeaturePriority? Priority { get; set; }
    public FeatureStatus? Status { get; set; }
    public FeatureComplexity? Complexity { get; set; }
    public string? AssignedTo { get; set; }
    public string? TargetVersion { get; set; }
    public string? InternalNotes { get; set; }
    public string? RejectionReason { get; set; }
}

public class FeatureRequestStatsDto
{
    public int TotalRequests { get; set; }
    public int SubmittedRequests { get; set; }
    public int PlannedRequests { get; set; }
    public int InProgressRequests { get; set; }
    public int CompletedRequests { get; set; }
    public Dictionary<FeatureCategory, int> RequestsByCategory { get; set; } = new();
    public List<FeatureRequestResponseDto> TopVotedRequests { get; set; } = new();
}

#endregion

#region Survey DTOs

public class CreateSurveyDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public SurveyType Type { get; set; } = SurveyType.General;
    public SurveyTargetAudience TargetAudience { get; set; } = SurveyTargetAudience.All;
    public SurveyTrigger Trigger { get; set; } = SurveyTrigger.Manual;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public List<CreateSurveyQuestionDto> Questions { get; set; } = new();
}

public class CreateSurveyQuestionDto
{
    public int Order { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string? QuestionTextTr { get; set; }
    public QuestionType Type { get; set; } = QuestionType.Text;
    public bool IsRequired { get; set; } = true;
    public List<string>? Options { get; set; }
    public List<string>? OptionsTr { get; set; }
    public int? MinValue { get; set; }
    public int? MaxValue { get; set; }
}

public class SurveyResponseDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public SurveyType Type { get; set; }
    public bool IsActive { get; set; }
    public int TotalResponses { get; set; }
    public decimal CompletionRate { get; set; }
    public List<SurveyQuestionResponseDto> Questions { get; set; } = new();
}

public class SurveyQuestionResponseDto
{
    public Guid Id { get; set; }
    public int Order { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string? QuestionTextTr { get; set; }
    public QuestionType Type { get; set; }
    public bool IsRequired { get; set; }
    public List<string> Options { get; set; } = new();
    public List<string>? OptionsTr { get; set; }
}

public class SubmitSurveyResponseDto
{
    public Guid SurveyId { get; set; }
    public List<SubmitSurveyAnswerDto> Answers { get; set; } = new();
    public string? Browser { get; set; }
    public string? OperatingSystem { get; set; }
    public string? PageUrl { get; set; }
}

public class SubmitSurveyAnswerDto
{
    public Guid QuestionId { get; set; }
    public string? TextAnswer { get; set; }
    public int? NumberAnswer { get; set; }
    public List<string>? SelectedOptions { get; set; }
    public bool? BooleanAnswer { get; set; }
}

public class SurveyResultsDto
{
    public Guid SurveyId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int TotalResponses { get; set; }
    public decimal CompletionRate { get; set; }
    public decimal? NPSScore { get; set; }
    public decimal? AverageRating { get; set; }
    public List<QuestionResultDto> QuestionResults { get; set; } = new();
}

public class QuestionResultDto
{
    public Guid QuestionId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public QuestionType Type { get; set; }
    public int ResponseCount { get; set; }
    public decimal? AverageValue { get; set; }
    public Dictionary<string, int>? OptionCounts { get; set; }
    public List<string>? TextResponses { get; set; } // Sample of text responses
}

#endregion

#region Performance Monitoring DTOs

public class PerformanceMetricsDto
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    // API Performance
    public decimal AverageResponseTimeMs { get; set; }
    public decimal P95ResponseTimeMs { get; set; }
    public decimal P99ResponseTimeMs { get; set; }
    public int TotalRequests { get; set; }
    public int ErrorCount { get; set; }
    public decimal ErrorRate { get; set; }
    
    // Database Performance
    public decimal AverageQueryTimeMs { get; set; }
    public int SlowQueriesCount { get; set; }
    public int ActiveConnections { get; set; }
    
    // Application Metrics
    public int ActiveUsers { get; set; }
    public int TotalSessions { get; set; }
    public decimal MatchCalculationsPerMinute { get; set; }
    public decimal ApplicationsSentPerHour { get; set; }
    
    // Resource Usage
    public decimal CpuUsagePercent { get; set; }
    public decimal MemoryUsageMB { get; set; }
    public decimal DiskUsagePercent { get; set; }
    
    // Feature Usage
    public Dictionary<string, int> FeatureUsageCount { get; set; } = new();
    public Dictionary<string, decimal> EndpointResponseTimes { get; set; } = new();
}

public class UserEngagementMetricsDto
{
    public int DailyActiveUsers { get; set; }
    public int WeeklyActiveUsers { get; set; }
    public int MonthlyActiveUsers { get; set; }
    public decimal AverageSessionDurationMinutes { get; set; }
    public decimal BounceRate { get; set; }
    public Dictionary<string, int> PageViews { get; set; } = new();
    public Dictionary<string, int> FeatureAdoption { get; set; } = new();
    public List<UserJourneyDto> TopUserJourneys { get; set; } = new();
}

public class UserJourneyDto
{
    public string JourneyPath { get; set; } = string.Empty; // e.g., "Landing -> Upload -> Match -> Apply"
    public int UserCount { get; set; }
    public decimal ConversionRate { get; set; }
    public decimal AverageDurationMinutes { get; set; }
}

#endregion

