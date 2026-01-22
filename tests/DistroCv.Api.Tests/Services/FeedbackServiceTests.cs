using DistroCv.Core.Entities;
using DistroCv.Core.Interfaces;
using DistroCv.Infrastructure.Data;
using DistroCv.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DistroCv.Api.Tests.Services;

/// <summary>
/// Unit tests for FeedbackService
/// Task 23.10: Test feedback storage, learning threshold
/// </summary>
public class FeedbackServiceTests : IDisposable
{
    private readonly DistroCvDbContext _context;
    private readonly Mock<IGeminiService> _geminiServiceMock;
    private readonly Mock<ILogger<FeedbackService>> _loggerMock;
    private readonly FeedbackService _service;
    private readonly Guid _testUserId;

    public FeedbackServiceTests()
    {
        var options = new DbContextOptionsBuilder<DistroCvDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new DistroCvDbContext(options);
        _geminiServiceMock = new Mock<IGeminiService>();
        _loggerMock = new Mock<ILogger<FeedbackService>>();

        _service = new FeedbackService(
            _context,
            _geminiServiceMock.Object,
            _loggerMock.Object);

        // Create test user with digital twin
        _testUserId = Guid.NewGuid();
        _context.Users.Add(new User 
        { 
            Id = _testUserId, 
            Email = "test@example.com",
            FullName = "Test User"
        });
        _context.DigitalTwins.Add(new DigitalTwin
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            Skills = "[\"C#\", \".NET\"]",
            Preferences = "{}"
        });
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    #region Test Data Setup

    private JobMatch CreateTestJobMatch(Guid? userId = null)
    {
        var user = userId ?? _testUserId;
        var jobPosting = new JobPosting
        {
            Id = Guid.NewGuid(),
            Title = "Test Job",
            Description = "Test Description",
            CompanyName = "Test Company",
            SourcePlatform = "LinkedIn",
            IsActive = true
        };
        _context.JobPostings.Add(jobPosting);

        var jobMatch = new JobMatch
        {
            Id = Guid.NewGuid(),
            UserId = user,
            JobPostingId = jobPosting.Id,
            MatchScore = 85,
            MatchReasoning = "Good match",
            SkillGaps = "[]",
            IsInQueue = true,
            Status = "Pending"
        };
        _context.JobMatches.Add(jobMatch);
        return jobMatch;
    }

    #endregion

    #region SubmitFeedbackAsync Tests

    [Fact]
    public async Task SubmitFeedbackAsync_ValidFeedback_ShouldStoreFeedback()
    {
        // Arrange
        var jobMatch = CreateTestJobMatch();
        await _context.SaveChangesAsync();

        // Act
        await _service.SubmitFeedbackAsync(
            _testUserId,
            jobMatch.Id,
            "Rejected",
            "Low Salary",
            "The salary is below market rate");

        // Assert
        var feedback = await _context.UserFeedbacks
            .FirstOrDefaultAsync(f => f.UserId == _testUserId && f.JobMatchId == jobMatch.Id);

        Assert.NotNull(feedback);
        Assert.Equal("Low Salary", feedback.Reason);
        Assert.Equal("Rejected", feedback.FeedbackType);
        Assert.Equal("The salary is below market rate", feedback.AdditionalNotes);
    }

    [Theory]
    [InlineData("Low Salary")]
    [InlineData("Old Tech")]
    [InlineData("Location")]
    [InlineData("Company Culture")]
    [InlineData("Other")]
    public async Task SubmitFeedbackAsync_DifferentReasons_ShouldStoreCorrectly(string reason)
    {
        // Arrange
        var jobMatch = CreateTestJobMatch();
        await _context.SaveChangesAsync();

        // Act
        await _service.SubmitFeedbackAsync(_testUserId, jobMatch.Id, "Rejected", reason, "Notes");

        // Assert
        var feedback = await _context.UserFeedbacks
            .FirstOrDefaultAsync(f => f.JobMatchId == jobMatch.Id);

        Assert.NotNull(feedback);
        Assert.Equal(reason, feedback.Reason);
    }

    #endregion

    #region GetFeedbackCountAsync Tests

    [Fact]
    public async Task GetFeedbackCountAsync_ShouldReturnCorrectCount()
    {
        // Arrange
        for (int i = 0; i < 5; i++)
        {
            var match = CreateTestJobMatch();
            await _context.SaveChangesAsync();
            await _service.SubmitFeedbackAsync(_testUserId, match.Id, "Rejected", "Low Salary", null);
        }

        // Act
        var count = await _service.GetFeedbackCountAsync(_testUserId);

        // Assert
        Assert.Equal(5, count);
    }

    #endregion

    #region Learning Threshold Tests (Property 10)

    [Fact]
    public async Task ShouldActivateLearningModelAsync_BelowThreshold_ShouldReturnFalse()
    {
        // Arrange - Add 9 feedbacks (below 10 threshold)
        for (int i = 0; i < 9; i++)
        {
            var match = CreateTestJobMatch();
            await _context.SaveChangesAsync();
            await _service.SubmitFeedbackAsync(_testUserId, match.Id, "Rejected", "Low Salary", null);
        }

        // Act
        var isActive = await _service.ShouldActivateLearningModelAsync(_testUserId);

        // Assert - Property 10: Count < 10 => Learning not active
        Assert.False(isActive);
    }

    [Fact]
    public async Task ShouldActivateLearningModelAsync_AtThreshold_ShouldReturnTrue()
    {
        // Arrange - Add exactly 10 feedbacks
        for (int i = 0; i < 10; i++)
        {
            var match = CreateTestJobMatch();
            await _context.SaveChangesAsync();
            await _service.SubmitFeedbackAsync(_testUserId, match.Id, "Rejected", "Low Salary", null);
        }

        // Act
        var isActive = await _service.ShouldActivateLearningModelAsync(_testUserId);

        // Assert - Property 10: Count >= 10 => Learning active
        Assert.True(isActive);
    }

    [Fact]
    public async Task ShouldActivateLearningModelAsync_AboveThreshold_ShouldRemainTrue()
    {
        // Arrange - Add 15 feedbacks
        for (int i = 0; i < 15; i++)
        {
            var match = CreateTestJobMatch();
            await _context.SaveChangesAsync();
            await _service.SubmitFeedbackAsync(_testUserId, match.Id, "Rejected", "Low Salary", null);
        }

        // Act
        var isActive = await _service.ShouldActivateLearningModelAsync(_testUserId);

        // Assert
        Assert.True(isActive);
    }

    #endregion

    #region GetUserFeedbackAsync Tests

    [Fact]
    public async Task GetUserFeedbackAsync_ShouldReturnUserFeedbacks()
    {
        // Arrange
        var match1 = CreateTestJobMatch();
        var match2 = CreateTestJobMatch();
        await _context.SaveChangesAsync();

        await _service.SubmitFeedbackAsync(_testUserId, match1.Id, "Rejected", "Low Salary", null);
        await _service.SubmitFeedbackAsync(_testUserId, match2.Id, "Approved", "Old Tech", null);

        // Act
        var feedbacks = await _service.GetUserFeedbackAsync(_testUserId);

        // Assert
        Assert.Equal(2, feedbacks.Count);
    }

    [Fact]
    public async Task GetUserFeedbackAsync_NoFeedbacks_ShouldReturnEmpty()
    {
        // Act
        var feedbacks = await _service.GetUserFeedbackAsync(_testUserId);

        // Assert
        Assert.Empty(feedbacks);
    }

    #endregion

    #region GetFeedbackAnalyticsAsync Tests

    [Fact]
    public async Task GetFeedbackAnalyticsAsync_ShouldReturnCorrectAnalytics()
    {
        // Arrange
        var reasons = new[] { "Low Salary", "Low Salary", "Old Tech", "Location", "Low Salary" };
        foreach (var reason in reasons)
        {
            var match = CreateTestJobMatch();
            await _context.SaveChangesAsync();
            await _service.SubmitFeedbackAsync(_testUserId, match.Id, "Rejected", reason, null);
        }
        
        // Add some approved feedbacks
        for (int i = 0; i < 3; i++)
        {
            var match = CreateTestJobMatch();
            await _context.SaveChangesAsync();
            await _service.SubmitFeedbackAsync(_testUserId, match.Id, "Approved", null, null);
        }

        // Act
        var analytics = await _service.GetFeedbackAnalyticsAsync(_testUserId);

        // Assert
        Assert.NotNull(analytics);
        Assert.Equal(8, analytics.TotalFeedbacks);
        Assert.Equal(5, analytics.RejectedCount);
        Assert.Equal(3, analytics.ApprovedCount);
        Assert.True(analytics.RejectReasons.ContainsKey("Low Salary"));
        Assert.Equal(3, analytics.RejectReasons["Low Salary"]);
    }

    [Fact]
    public async Task GetFeedbackAnalyticsAsync_NoFeedbacks_ShouldReturnEmptyAnalytics()
    {
        // Act
        var analytics = await _service.GetFeedbackAnalyticsAsync(_testUserId);

        // Assert
        Assert.NotNull(analytics);
        Assert.Equal(0, analytics.TotalFeedbacks);
        Assert.Empty(analytics.RejectReasons);
    }

    #endregion

    #region AnalyzeFeedbackAndUpdateWeightsAsync Tests

    [Fact]
    public async Task AnalyzeFeedbackAndUpdateWeightsAsync_WhenLearningActive_ShouldCallGemini()
    {
        // Arrange - Add 10 feedbacks to activate learning
        for (int i = 0; i < 10; i++)
        {
            var match = CreateTestJobMatch();
            await _context.SaveChangesAsync();
            await _service.SubmitFeedbackAsync(_testUserId, match.Id, "Rejected", "Low Salary", null);
        }

        _geminiServiceMock.Setup(x => x.GenerateContentAsync(It.IsAny<string>()))
            .ReturnsAsync(@"{
                ""salaryWeight"": 0.8,
                ""technologyWeight"": 0.6
            }");

        // Act
        await _service.AnalyzeFeedbackAndUpdateWeightsAsync(_testUserId);

        // Assert
        _geminiServiceMock.Verify(
            x => x.GenerateContentAsync(It.IsAny<string>()), 
            Times.Once);
    }

    [Fact]
    public async Task AnalyzeFeedbackAndUpdateWeightsAsync_WhenLearningNotActive_ShouldNotCallGemini()
    {
        // Arrange - Only 5 feedbacks (below threshold)
        for (int i = 0; i < 5; i++)
        {
            var match = CreateTestJobMatch();
            await _context.SaveChangesAsync();
            await _service.SubmitFeedbackAsync(_testUserId, match.Id, "Rejected", "Low Salary", null);
        }

        // Act
        await _service.AnalyzeFeedbackAndUpdateWeightsAsync(_testUserId);

        // Assert - Gemini should NOT be called
        _geminiServiceMock.Verify(
            x => x.GenerateContentAsync(It.IsAny<string>()), 
            Times.Never);
    }

    #endregion
}
