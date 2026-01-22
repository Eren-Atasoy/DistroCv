using DistroCv.Core.Entities;
using DistroCv.Core.Interfaces;
using DistroCv.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DistroCv.Infrastructure.Services;

/// <summary>
/// Service for managing user feedback and learning system
/// </summary>
public class FeedbackService : IFeedbackService
{
    private readonly DistroCvDbContext _context;
    private readonly IGeminiService _geminiService;
    private readonly ILogger<FeedbackService> _logger;
    
    private const int LEARNING_MODEL_THRESHOLD = 10;

    public FeedbackService(
        DistroCvDbContext context,
        IGeminiService geminiService,
        ILogger<FeedbackService> logger)
    {
        _context = context;
        _geminiService = geminiService;
        _logger = logger;
    }

    /// <summary>
    /// Submits user feedback for a job match (Validates: Requirement 16.1, 16.2, 16.3)
    /// </summary>
    public async Task SubmitFeedbackAsync(
        Guid userId,
        Guid jobMatchId,
        string feedbackType,
        string? reason = null,
        string? additionalNotes = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Submitting feedback for user {UserId}, job match {JobMatchId}", userId, jobMatchId);

        try
        {
            // Create feedback entry
            var feedback = new UserFeedback
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                JobMatchId = jobMatchId,
                FeedbackType = feedbackType,
                Reason = reason,
                AdditionalNotes = additionalNotes,
                CreatedAt = DateTime.UtcNow
            };

            _context.UserFeedbacks.Add(feedback);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Feedback submitted successfully for user {UserId}", userId);

            // Check if learning model should be activated
            var shouldActivate = await ShouldActivateLearningModelAsync(userId, cancellationToken);
            
            if (shouldActivate)
            {
                _logger.LogInformation("Learning model threshold reached for user {UserId}, analyzing feedback", userId);
                await AnalyzeFeedbackAndUpdateWeightsAsync(userId, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting feedback for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Gets feedback count for a user
    /// </summary>
    public async Task<int> GetFeedbackCountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserFeedbacks
            .Where(f => f.UserId == userId)
            .CountAsync(cancellationToken);
    }

    /// <summary>
    /// Checks if learning model should be activated (10+ feedbacks) (Validates: Requirement 16.5)
    /// </summary>
    public async Task<bool> ShouldActivateLearningModelAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var feedbackCount = await GetFeedbackCountAsync(userId, cancellationToken);
        return feedbackCount >= LEARNING_MODEL_THRESHOLD;
    }

    /// <summary>
    /// Analyzes feedback and updates Digital Twin weights (Validates: Requirement 16.4)
    /// </summary>
    public async Task AnalyzeFeedbackAndUpdateWeightsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Analyzing feedback and updating weights for user {UserId}", userId);

        try
        {
            // Get user's digital twin
            var digitalTwin = await _context.DigitalTwins
                .FirstOrDefaultAsync(dt => dt.UserId == userId, cancellationToken);

            if (digitalTwin == null)
            {
                _logger.LogWarning("Digital twin not found for user {UserId}", userId);
                return;
            }

            // Get all user feedback
            var feedbacks = await _context.UserFeedbacks
                .Include(f => f.JobMatch)
                    .ThenInclude(jm => jm.JobPosting)
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync(cancellationToken);

            if (feedbacks.Count < LEARNING_MODEL_THRESHOLD)
            {
                _logger.LogInformation("Not enough feedback to activate learning model for user {UserId}", userId);
                return;
            }

            // Build prompt for Gemini to analyze feedback patterns
            var prompt = BuildFeedbackAnalysisPrompt(digitalTwin, feedbacks);

            // Get weight adjustments from Gemini
            var geminiResponse = await _geminiService.GenerateContentAsync(prompt);

            // Parse and apply weight adjustments
            await ApplyWeightAdjustments(digitalTwin, geminiResponse, cancellationToken);

            _logger.LogInformation("Successfully updated weights for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing feedback for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Gets feedback analytics for a user
    /// </summary>
    public async Task<FeedbackAnalytics> GetFeedbackAnalyticsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting feedback analytics for user {UserId}", userId);

        try
        {
            var feedbacks = await _context.UserFeedbacks
                .Where(f => f.UserId == userId)
                .ToListAsync(cancellationToken);

            var rejectedCount = feedbacks.Count(f => f.FeedbackType == "Rejected");
            var approvedCount = feedbacks.Count(f => f.FeedbackType == "Approved");

            var rejectReasons = feedbacks
                .Where(f => f.FeedbackType == "Rejected" && !string.IsNullOrEmpty(f.Reason))
                .GroupBy(f => f.Reason!)
                .ToDictionary(g => g.Key, g => g.Count());

            var topReasons = rejectReasons
                .OrderByDescending(r => r.Value)
                .Take(5)
                .Select(r => r.Key)
                .ToList();

            var isLearningActive = await ShouldActivateLearningModelAsync(userId, cancellationToken);

            var analytics = new FeedbackAnalytics
            {
                TotalFeedbacks = feedbacks.Count,
                RejectedCount = rejectedCount,
                ApprovedCount = approvedCount,
                RejectReasons = rejectReasons,
                IsLearningModelActive = isLearningActive,
                LastFeedbackDate = feedbacks.OrderByDescending(f => f.CreatedAt).FirstOrDefault()?.CreatedAt,
                TopRejectReasons = topReasons
            };

            _logger.LogInformation("Feedback analytics retrieved for user {UserId}: {Total} total, {Rejected} rejected, {Approved} approved",
                userId, analytics.TotalFeedbacks, analytics.RejectedCount, analytics.ApprovedCount);

            return analytics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting feedback analytics for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Gets all feedback for a user
    /// </summary>
    public async Task<List<UserFeedback>> GetUserFeedbackAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserFeedbacks
            .Include(f => f.JobMatch)
                .ThenInclude(jm => jm.JobPosting)
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Builds prompt for Gemini to analyze feedback patterns
    /// </summary>
    private string BuildFeedbackAnalysisPrompt(DigitalTwin digitalTwin, List<UserFeedback> feedbacks)
    {
        var rejectedFeedbacks = feedbacks.Where(f => f.FeedbackType == "Rejected").ToList();
        var approvedFeedbacks = feedbacks.Where(f => f.FeedbackType == "Approved").ToList();

        var rejectReasonsSummary = rejectedFeedbacks
            .Where(f => !string.IsNullOrEmpty(f.Reason))
            .GroupBy(f => f.Reason)
            .Select(g => $"{g.Key}: {g.Count()} times")
            .ToList();

        return $@"You are an AI learning system analyzing user feedback to improve job matching.

Current Digital Twin Profile:
- Skills: {digitalTwin.Skills}
- Experience: {digitalTwin.Experience}
- Career Goals: {digitalTwin.CareerGoals}
- Preferences: {digitalTwin.Preferences}

Feedback Summary:
- Total Feedbacks: {feedbacks.Count}
- Rejected: {rejectedFeedbacks.Count}
- Approved: {approvedFeedbacks.Count}

Reject Reasons:
{string.Join("\n", rejectReasonsSummary)}

Based on this feedback, suggest weight adjustments for the Digital Twin to improve future matches.
Consider:
1. Which factors are most important to the user (salary, location, technology, company culture)
2. What patterns emerge from rejected jobs
3. What characteristics approved jobs have in common

Return your analysis in JSON format:
{{
  ""salaryWeight"": 0.0-1.0,
  ""locationWeight"": 0.0-1.0,
  ""technologyWeight"": 0.0-1.0,
  ""companyCultureWeight"": 0.0-1.0,
  ""insights"": [""insight1"", ""insight2"", ...],
  ""recommendations"": [""recommendation1"", ""recommendation2"", ...]
}}

Higher weights (closer to 1.0) mean more important to the user.";
    }

    /// <summary>
    /// Applies weight adjustments to Digital Twin
    /// </summary>
    private async Task ApplyWeightAdjustments(
        DigitalTwin digitalTwin,
        string geminiResponse,
        CancellationToken cancellationToken)
    {
        try
        {
            // Parse Gemini response
            var adjustments = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, System.Text.Json.JsonElement>>(
                geminiResponse,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (adjustments == null)
            {
                _logger.LogWarning("Failed to parse weight adjustments");
                return;
            }

            // Update preferences with new weights
            var currentPreferences = string.IsNullOrEmpty(digitalTwin.Preferences)
                ? new Dictionary<string, object>()
                : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(digitalTwin.Preferences) ?? new Dictionary<string, object>();

            if (adjustments.ContainsKey("salaryWeight"))
                currentPreferences["salaryWeight"] = adjustments["salaryWeight"].GetDouble();
            
            if (adjustments.ContainsKey("locationWeight"))
                currentPreferences["locationWeight"] = adjustments["locationWeight"].GetDouble();
            
            if (adjustments.ContainsKey("technologyWeight"))
                currentPreferences["technologyWeight"] = adjustments["technologyWeight"].GetDouble();
            
            if (adjustments.ContainsKey("companyCultureWeight"))
                currentPreferences["companyCultureWeight"] = adjustments["companyCultureWeight"].GetDouble();

            // Save updated preferences
            digitalTwin.Preferences = System.Text.Json.JsonSerializer.Serialize(currentPreferences);
            digitalTwin.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Weight adjustments applied successfully for user {UserId}", digitalTwin.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying weight adjustments");
        }
    }
}
