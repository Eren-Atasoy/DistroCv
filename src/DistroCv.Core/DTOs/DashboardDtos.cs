namespace DistroCv.Core.DTOs;

public record DashboardStatsDto(
    int TotalApplications,
    int PendingApplications,
    int SentApplications,
    int ViewedApplications,
    int RespondedApplications,
    int RejectedApplications,
    decimal ResponseRate,
    int InterviewInvitations,
    int MatchingJobs
);

public record DashboardTrendsDto(
    List<TrendDataPoint> WeeklyApplications,
    List<TrendDataPoint> MonthlyApplications,
    List<StatusBreakdown> StatusBreakdown
);

public record TrendDataPoint(
    DateTime Date,
    int Count
);

public record StatusBreakdown(
    string Status,
    int Count,
    decimal Percentage
);
