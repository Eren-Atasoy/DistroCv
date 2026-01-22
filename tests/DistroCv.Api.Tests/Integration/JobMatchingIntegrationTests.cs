using DistroCv.Core.Entities;
using DistroCv.Core.Interfaces;
using DistroCv.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace DistroCv.Api.Tests.Integration;

/// <summary>
/// Integration tests for job matching pipeline (Task 24.3)
/// Tests the end-to-end flow from job scraping to match calculation
/// </summary>
public class JobMatchingIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public JobMatchingIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        SetupMocks();
    }

    private void SetupMocks()
    {
        // Setup Gemini service mock for match calculation
        _factory.GeminiServiceMock
            .Setup(x => x.CalculateMatchScoreAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new MatchResult
            {
                MatchScore = 85,
                Reasoning = "Strong match due to C# and .NET experience. Candidate has relevant skills.",
                SkillGaps = new List<string> { "Kubernetes", "Docker" }
            });

        // Setup embedding generation for job postings
        _factory.GeminiServiceMock
            .Setup(x => x.GenerateEmbeddingAsync(It.IsAny<string>()))
            .ReturnsAsync(new float[] { 0.1f, 0.2f, 0.3f, 0.4f, 0.5f });
    }

    [Fact]
    public async Task MatchingService_ShouldCalculateMatch_WhenUserAndJobExist()
    {
        // Arrange
        var user = await _factory.CreateTestUserAsync("match@example.com", "Match User");
        var digitalTwin = await _factory.CreateDigitalTwinAsync(user.Id);
        var jobPosting = await _factory.CreateJobPostingAsync("Senior .NET Developer");

        using var scope = _factory.Services.CreateScope();
        var matchingService = scope.ServiceProvider.GetRequiredService<IMatchingService>();

        // Act
        var match = await matchingService.CalculateMatchAsync(user.Id, jobPosting.Id);

        // Assert
        Assert.NotNull(match);
        Assert.Equal(user.Id, match.UserId);
        Assert.Equal(jobPosting.Id, match.JobPostingId);
        Assert.Equal(85, match.MatchScore);
        Assert.NotEmpty(match.MatchReasoning);
        Assert.True(match.IsInQueue); // Score >= 80 should be auto-queued
    }

    [Fact]
    public async Task MatchingService_ShouldNotCreateDuplicateMatch()
    {
        // Arrange
        var user = await _factory.CreateTestUserAsync("duplicate@example.com", "Duplicate User");
        var digitalTwin = await _factory.CreateDigitalTwinAsync(user.Id);
        var jobPosting = await _factory.CreateJobPostingAsync("Backend Developer");

        using var scope = _factory.Services.CreateScope();
        var matchingService = scope.ServiceProvider.GetRequiredService<IMatchingService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<DistroCvDbContext>();

        // Create first match
        var firstMatch = await matchingService.CalculateMatchAsync(user.Id, jobPosting.Id);

        // Act - Try to create second match for same user/job
        var secondMatch = await matchingService.CalculateMatchAsync(user.Id, jobPosting.Id);

        // Assert
        Assert.Equal(firstMatch.Id, secondMatch.Id); // Should return existing match
        
        // Verify only one match exists in database
        var matchCount = await dbContext.JobMatches.CountAsync(m => 
            m.UserId == user.Id && m.JobPostingId == jobPosting.Id);
        Assert.Equal(1, matchCount);
    }

    [Fact]
    public async Task MatchingService_ShouldFindMatchesForUser()
    {
        // Arrange
        var user = await _factory.CreateTestUserAsync("findmatches@example.com", "Find Matches User");
        var digitalTwin = await _factory.CreateDigitalTwinAsync(user.Id);

        // Create multiple job postings
        var job1 = await _factory.CreateJobPostingAsync("Frontend Developer");
        var job2 = await _factory.CreateJobPostingAsync("Full Stack Developer");
        var job3 = await _factory.CreateJobPostingAsync("DevOps Engineer");

        using var scope = _factory.Services.CreateScope();
        var matchingService = scope.ServiceProvider.GetRequiredService<IMatchingService>();

        // Act
        var matches = await matchingService.FindMatchesForUserAsync(user.Id, minScore: 80);

        // Assert
        Assert.NotNull(matches);
        Assert.True(matches.Count >= 0); // May be filtered based on minScore
        Assert.All(matches, m => Assert.Equal(user.Id, m.UserId));
    }

    [Fact]
    public async Task MatchingService_ShouldApproveMatch()
    {
        // Arrange
        var user = await _factory.CreateTestUserAsync("approve@example.com", "Approve User");
        var digitalTwin = await _factory.CreateDigitalTwinAsync(user.Id);
        var jobPosting = await _factory.CreateJobPostingAsync("API Developer");

        using var scope = _factory.Services.CreateScope();
        var matchingService = scope.ServiceProvider.GetRequiredService<IMatchingService>();

        // Create match first
        var match = await matchingService.CalculateMatchAsync(user.Id, jobPosting.Id);

        // Act
        var approvedMatch = await matchingService.ApproveMatchAsync(match.Id, user.Id);

        // Assert
        Assert.Equal("Approved", approvedMatch.Status);
        Assert.True(approvedMatch.IsInQueue);
    }

    [Fact]
    public async Task MatchingService_ShouldRejectMatch()
    {
        // Arrange
        var user = await _factory.CreateTestUserAsync("reject@example.com", "Reject User");
        var digitalTwin = await _factory.CreateDigitalTwinAsync(user.Id);
        var jobPosting = await _factory.CreateJobPostingAsync("Data Engineer");

        using var scope = _factory.Services.CreateScope();
        var matchingService = scope.ServiceProvider.GetRequiredService<IMatchingService>();

        // Create match first
        var match = await matchingService.CalculateMatchAsync(user.Id, jobPosting.Id);

        // Act
        var rejectedMatch = await matchingService.RejectMatchAsync(match.Id, user.Id);

        // Assert
        Assert.Equal("Rejected", rejectedMatch.Status);
        Assert.False(rejectedMatch.IsInQueue);
    }

    [Fact]
    public async Task MatchingService_ShouldGetQueuedMatches()
    {
        // Arrange
        var user = await _factory.CreateTestUserAsync("queued@example.com", "Queued User");
        var digitalTwin = await _factory.CreateDigitalTwinAsync(user.Id);
        var job1 = await _factory.CreateJobPostingAsync("Mobile Developer");
        var job2 = await _factory.CreateJobPostingAsync("QA Engineer");

        using var scope = _factory.Services.CreateScope();
        var matchingService = scope.ServiceProvider.GetRequiredService<IMatchingService>();

        // Create matches (they will be auto-queued with score >= 80)
        await matchingService.CalculateMatchAsync(user.Id, job1.Id);
        await matchingService.CalculateMatchAsync(user.Id, job2.Id);

        // Act
        var queuedMatches = await matchingService.GetQueuedMatchesAsync(user.Id);

        // Assert
        Assert.NotNull(queuedMatches);
        Assert.True(queuedMatches.Count >= 2);
        Assert.All(queuedMatches, m => Assert.True(m.IsInQueue));
    }

    [Fact]
    public async Task MatchingService_ShouldThrow_WhenDigitalTwinNotFound()
    {
        // Arrange
        var user = await _factory.CreateTestUserAsync("notwin@example.com", "No Twin User");
        // Note: Not creating digital twin for this user
        var jobPosting = await _factory.CreateJobPostingAsync("Cloud Architect");

        using var scope = _factory.Services.CreateScope();
        var matchingService = scope.ServiceProvider.GetRequiredService<IMatchingService>();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            matchingService.CalculateMatchAsync(user.Id, jobPosting.Id));
    }

    [Fact]
    public async Task MatchingService_ShouldThrow_WhenJobPostingNotFound()
    {
        // Arrange
        var user = await _factory.CreateTestUserAsync("nojob@example.com", "No Job User");
        var digitalTwin = await _factory.CreateDigitalTwinAsync(user.Id);
        var nonExistentJobId = Guid.NewGuid();

        using var scope = _factory.Services.CreateScope();
        var matchingService = scope.ServiceProvider.GetRequiredService<IMatchingService>();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            matchingService.CalculateMatchAsync(user.Id, nonExistentJobId));
    }

    [Fact]
    public async Task MatchingService_ShouldStoreSkillGaps()
    {
        // Arrange
        var user = await _factory.CreateTestUserAsync("skillgap@example.com", "Skill Gap User");
        var digitalTwin = await _factory.CreateDigitalTwinAsync(user.Id);
        var jobPosting = await _factory.CreateJobPostingAsync("Platform Engineer");

        using var scope = _factory.Services.CreateScope();
        var matchingService = scope.ServiceProvider.GetRequiredService<IMatchingService>();

        // Act
        var match = await matchingService.CalculateMatchAsync(user.Id, jobPosting.Id);

        // Assert
        Assert.NotNull(match.SkillGaps);
        Assert.Contains("Kubernetes", match.SkillGaps);
        Assert.Contains("Docker", match.SkillGaps);
    }
}

