using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DistroCv.Core.DTOs;
using DistroCv.Core.Entities;

namespace DistroCv.Core.Interfaces;

/// <summary>
/// Service interface for managing beta testing operations
/// </summary>
public interface IBetaTestingService
{
    #region Beta Tester Management
    
    /// <summary>
    /// Submit a new beta tester application
    /// </summary>
    Task<BetaTesterResponseDto> ApplyForBetaAsync(BetaTesterApplicationDto application, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Approve a beta tester application
    /// </summary>
    Task<BetaTesterResponseDto> ApproveBetaTesterAsync(Guid testerId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Reject a beta tester application
    /// </summary>
    Task<BetaTesterResponseDto> RejectBetaTesterAsync(Guid testerId, string reason, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get all beta testers with optional filtering
    /// </summary>
    Task<List<BetaTesterResponseDto>> GetBetaTestersAsync(BetaTesterStatus? status = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get beta tester by ID
    /// </summary>
    Task<BetaTesterResponseDto?> GetBetaTesterAsync(Guid testerId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get beta testing statistics
    /// </summary>
    Task<BetaTesterStatsDto> GetBetaStatsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Update beta tester activity
    /// </summary>
    Task UpdateBetaTesterActivityAsync(Guid userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Link a registered user to their beta tester profile
    /// </summary>
    Task LinkUserToBetaTesterAsync(Guid userId, string email, CancellationToken cancellationToken = default);
    
    #endregion
    
    #region Bug Reports
    
    /// <summary>
    /// Submit a new bug report
    /// </summary>
    Task<BugReportResponseDto> SubmitBugReportAsync(Guid userId, CreateBugReportDto bugReport, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get all bug reports with optional filtering
    /// </summary>
    Task<List<BugReportResponseDto>> GetBugReportsAsync(BugStatus? status = null, BugPriority? priority = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get bug report by ID
    /// </summary>
    Task<BugReportResponseDto?> GetBugReportAsync(Guid bugId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Update bug report (admin)
    /// </summary>
    Task<BugReportResponseDto> UpdateBugReportAsync(Guid bugId, UpdateBugReportDto update, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get bug report statistics
    /// </summary>
    Task<BugReportStatsDto> GetBugStatsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Add comment to bug report
    /// </summary>
    Task AddBugCommentAsync(Guid bugId, Guid userId, string comment, bool isInternal = false, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Vote for a bug report (confirm it's a real issue)
    /// </summary>
    Task VoteBugReportAsync(Guid bugId, Guid userId, CancellationToken cancellationToken = default);
    
    #endregion
    
    #region Feature Requests
    
    /// <summary>
    /// Submit a new feature request
    /// </summary>
    Task<FeatureRequestResponseDto> SubmitFeatureRequestAsync(Guid userId, CreateFeatureRequestDto request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get all feature requests with optional filtering
    /// </summary>
    Task<List<FeatureRequestResponseDto>> GetFeatureRequestsAsync(FeatureStatus? status = null, FeatureCategory? category = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get feature request by ID
    /// </summary>
    Task<FeatureRequestResponseDto?> GetFeatureRequestAsync(Guid requestId, Guid? userId = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Update feature request (admin)
    /// </summary>
    Task<FeatureRequestResponseDto> UpdateFeatureRequestAsync(Guid requestId, UpdateFeatureRequestDto update, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get feature request statistics
    /// </summary>
    Task<FeatureRequestStatsDto> GetFeatureRequestStatsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Vote for a feature request
    /// </summary>
    Task<int> VoteFeatureRequestAsync(Guid requestId, Guid userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Remove vote from a feature request
    /// </summary>
    Task<int> UnvoteFeatureRequestAsync(Guid requestId, Guid userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Add comment to feature request
    /// </summary>
    Task AddFeatureCommentAsync(Guid requestId, Guid userId, string comment, bool isOfficial = false, CancellationToken cancellationToken = default);
    
    #endregion
    
    #region Surveys
    
    /// <summary>
    /// Create a new survey (admin)
    /// </summary>
    Task<SurveyResponseDto> CreateSurveyAsync(CreateSurveyDto survey, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get active surveys for a user
    /// </summary>
    Task<List<SurveyResponseDto>> GetActiveSurveysForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get survey by ID
    /// </summary>
    Task<SurveyResponseDto?> GetSurveyAsync(Guid surveyId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Submit survey response
    /// </summary>
    Task SubmitSurveyResponseAsync(Guid userId, SubmitSurveyResponseDto response, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get survey results (admin)
    /// </summary>
    Task<SurveyResultsDto> GetSurveyResultsAsync(Guid surveyId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Activate/Deactivate survey
    /// </summary>
    Task<SurveyResponseDto> SetSurveyActiveAsync(Guid surveyId, bool isActive, CancellationToken cancellationToken = default);
    
    #endregion
    
    #region Performance Monitoring
    
    /// <summary>
    /// Get current performance metrics
    /// </summary>
    Task<PerformanceMetricsDto> GetPerformanceMetricsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get historical performance metrics
    /// </summary>
    Task<List<PerformanceMetricsDto>> GetHistoricalMetricsAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get user engagement metrics
    /// </summary>
    Task<UserEngagementMetricsDto> GetUserEngagementMetricsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Record a performance metric
    /// </summary>
    Task RecordMetricAsync(string metricName, decimal value, Dictionary<string, string>? dimensions = null, CancellationToken cancellationToken = default);
    
    #endregion
}

