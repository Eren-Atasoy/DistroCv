using DistroCv.Core.Entities;

namespace DistroCv.Core.Interfaces;

/// <summary>
/// Service for managing user feedback and learning system
/// </summary>
public interface IFeedbackService
{
    /// <summary>
    /// Submits user feedback for a job match (Validates: Requirement 16.1, 16.2, 16.3)
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="jobMatchId">Job match ID</param>
    /// <param name="feedbackType">Type of feedback (Rejected, Approved)</param>
    /// <param name="reason">Reason for feedback</param>
    /// <param name="additionalNotes">Additional notes</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SubmitFeedbackAsync(
        Guid userId,
        Guid jobMatchId,
        string feedbackType,
        string? reason = null,
        string? additionalNotes = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets feedback count for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Total feedback count</returns>
    Task<int> GetFeedbackCountAsync(Guid userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if learning model should be activated (10+ feedbacks) (Validates: Requirement 16.5)
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if learning model should be active</returns>
    Task<bool> ShouldActivateLearningModelAsync(Guid userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Analyzes feedback and updates Digital Twin weights (Validates: Requirement 16.4)
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task AnalyzeFeedbackAndUpdateWeightsAsync(Guid userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets feedback analytics for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Feedback analytics</returns>
    Task<FeedbackAnalytics> GetFeedbackAnalyticsAsync(Guid userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all feedback for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of user feedback</returns>
    Task<List<UserFeedback>> GetUserFeedbackAsync(Guid userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Feedback analytics for a user
/// </summary>
public class FeedbackAnalytics
{
    public int TotalFeedbacks { get; set; }
    public int RejectedCount { get; set; }
    public int ApprovedCount { get; set; }
    public Dictionary<string, int> RejectReasons { get; set; } = new();
    public bool IsLearningModelActive { get; set; }
    public DateTime? LastFeedbackDate { get; set; }
    public List<string> TopRejectReasons { get; set; } = new();
}
