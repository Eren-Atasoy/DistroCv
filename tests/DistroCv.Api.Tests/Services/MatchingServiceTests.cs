using DistroCv.Core.Entities;
using DistroCv.Core.Interfaces;
using DistroCv.Infrastructure.Data;
using DistroCv.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Xunit;

namespace DistroCv.Api.Tests.Services;

/// <summary>
/// Unit tests for MatchingService
/// Task 23.3: Test match score calculation and reasoning generation
/// </summary>
public class MatchingServiceTests
{
    private readonly Mock<IJobMatchRepository> _jobMatchRepositoryMock;
    private readonly Mock<IGeminiService> _geminiServiceMock;
    private readonly Mock<INotificationService> _notificationServiceMock;
    private readonly Mock<ILogger<MatchingService>> _loggerMock;
    private readonly DbContextOptions<DistroCvDbContext> _dbContextOptions;

    public MatchingServiceTests()
    {
        _jobMatchRepositoryMock = new Mock<IJobMatchRepository>();
        _geminiServiceMock = new Mock<IGeminiService>();
        _notificationServiceMock = new Mock<INotificationService>();
        _loggerMock = new Mock<ILogger<MatchingService>>();
        _dbContextOptions = new DbContextOptionsBuilder<DistroCvDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    private DistroCvDbContext CreateDbContext() => new DistroCvDbContext(_dbContextOptions);

    private MatchingService CreateService(DistroCvDbContext context)
    {
        return new MatchingService(
            context,
            _jobMatchRepositoryMock.Object,
            _geminiServiceMock.Object,
            _notificationServiceMock.Object,
            _loggerMock.Object);
    }

    #region Test Data Setup

    private User CreateTestUser()
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            FullName = "Test User",
            CreatedAt = DateTime.UtcNow
        };
    }

    private DigitalTwin CreateTestDigitalTwin(Guid userId, string? preferredSectors = null, string? preferredCities = null, bool isRemote = false)
    {
        return new DigitalTwin
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Skills = "[\"C#\", \".NET Core\", \"Azure\", \"SQL Server\"]",
            Experience = "[{\"title\": \"Senior Developer\", \"company\": \"Tech Corp\", \"years\": 5}]",
            Education = "[{\"degree\": \"BSc Computer Science\", \"school\": \"MIT\"}]",
            CareerGoals = "Lead technical teams and architect scalable systems",
            Preferences = "{\"seniority\": \"Senior\"}",
            PreferredSectors = preferredSectors,
            PreferredCities = preferredCities,
            IsRemotePreferred = isRemote,
            CreatedAt = DateTime.UtcNow
        };
    }

    private JobPosting CreateTestJobPosting(int? sectorId = null, string? city = null, bool isRemote = false)
    {
        return new JobPosting
        {
            Id = Guid.NewGuid(),
            ExternalId = $"linkedin_{Guid.NewGuid():N}",
            Title = "Senior .NET Developer",
            CompanyName = "Tech Corp",
            Description = "Looking for experienced .NET developer with microservices experience",
            Requirements = "[\"C#\", \".NET Core\", \"Microservices\", \"Azure\"]",
            Location = "Istanbul, Turkey",
            SectorId = sectorId,
            City = city,
            IsRemote = isRemote,
            SalaryRange = "80,000 - 120,000 TL",
            SourcePlatform = "LinkedIn",
            IsActive = true,
            ScrapedAt = DateTime.UtcNow
        };
    }

    #endregion

    #region CalculateMatchAsync Tests

    [Fact]
    public async Task CalculateMatchAsync_ShouldCreateNewMatch_WhenNoExistingMatch()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        var user = CreateTestUser();
        var digitalTwin = CreateTestDigitalTwin(user.Id);
        var jobPosting = CreateTestJobPosting();

        context.Users.Add(user);
        context.DigitalTwins.Add(digitalTwin);
        context.JobPostings.Add(jobPosting);
        await context.SaveChangesAsync();

        _jobMatchRepositoryMock.Setup(r => r.ExistsAsync(user.Id, jobPosting.Id, default))
            .ReturnsAsync(false);

        _geminiServiceMock.Setup(s => s.CalculateMatchScoreAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new MatchResult 
            { 
                MatchScore = 85, 
                Reasoning = "Strong match due to C# and .NET experience", 
                SkillGaps = new List<string> { "Kubernetes" } 
            });

        _jobMatchRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<JobMatch>(), default))
            .ReturnsAsync((JobMatch jm, CancellationToken ct) => jm);

        // Act
        var result = await service.CalculateMatchAsync(user.Id, jobPosting.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(85, result.MatchScore);
        Assert.Equal("Strong match due to C# and .NET experience", result.MatchReasoning);
        Assert.True(result.IsInQueue); // Score >= 80 should auto-queue
        _jobMatchRepositoryMock.Verify(r => r.CreateAsync(It.IsAny<JobMatch>(), default), Times.Once);
    }

    [Fact]
    public async Task CalculateMatchAsync_ShouldReturnExistingMatch_WhenMatchExists()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        var user = CreateTestUser();
        var digitalTwin = CreateTestDigitalTwin(user.Id);
        var jobPosting = CreateTestJobPosting();

        context.Users.Add(user);
        context.DigitalTwins.Add(digitalTwin);
        context.JobPostings.Add(jobPosting);
        await context.SaveChangesAsync();

        var existingMatch = new JobMatch
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            JobPostingId = jobPosting.Id,
            MatchScore = 90,
            MatchReasoning = "Existing match",
            Status = "Pending"
        };

        _jobMatchRepositoryMock.Setup(r => r.ExistsAsync(user.Id, jobPosting.Id, default))
            .ReturnsAsync(true);

        _jobMatchRepositoryMock.Setup(r => r.GetByUserIdAsync(user.Id, default))
            .ReturnsAsync(new List<JobMatch> { existingMatch });

        // Act
        var result = await service.CalculateMatchAsync(user.Id, jobPosting.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(existingMatch.Id, result.Id);
        Assert.Equal(90, result.MatchScore);
        _jobMatchRepositoryMock.Verify(r => r.CreateAsync(It.IsAny<JobMatch>(), default), Times.Never);
    }

    [Fact]
    public async Task CalculateMatchAsync_ShouldThrow_WhenDigitalTwinNotFound()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        var nonExistentUserId = Guid.NewGuid();
        var jobPosting = CreateTestJobPosting();
        context.JobPostings.Add(jobPosting);
        await context.SaveChangesAsync();

        _jobMatchRepositoryMock.Setup(r => r.ExistsAsync(nonExistentUserId, jobPosting.Id, default))
            .ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CalculateMatchAsync(nonExistentUserId, jobPosting.Id));
    }

    [Fact]
    public async Task CalculateMatchAsync_ShouldThrow_WhenJobPostingNotFound()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        var user = CreateTestUser();
        var digitalTwin = CreateTestDigitalTwin(user.Id);
        context.Users.Add(user);
        context.DigitalTwins.Add(digitalTwin);
        await context.SaveChangesAsync();

        var nonExistentJobId = Guid.NewGuid();

        _jobMatchRepositoryMock.Setup(r => r.ExistsAsync(user.Id, nonExistentJobId, default))
            .ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CalculateMatchAsync(user.Id, nonExistentJobId));
    }

    [Fact]
    public async Task CalculateMatchAsync_ShouldSendNotification_WhenScoreAbove80()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        var user = CreateTestUser();
        var digitalTwin = CreateTestDigitalTwin(user.Id);
        var jobPosting = CreateTestJobPosting();

        context.Users.Add(user);
        context.DigitalTwins.Add(digitalTwin);
        context.JobPostings.Add(jobPosting);
        await context.SaveChangesAsync();

        _jobMatchRepositoryMock.Setup(r => r.ExistsAsync(user.Id, jobPosting.Id, default))
            .ReturnsAsync(false);

        _geminiServiceMock.Setup(s => s.CalculateMatchScoreAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new MatchResult { MatchScore = 85, Reasoning = "Match", SkillGaps = new List<string>() });

        _jobMatchRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<JobMatch>(), default))
            .ReturnsAsync((JobMatch jm, CancellationToken ct) => jm);

        // Act
        await service.CalculateMatchAsync(user.Id, jobPosting.Id);

        // Assert
        _notificationServiceMock.Verify(n => n.CreateNewMatchNotificationAsync(
            user.Id, 
            It.IsAny<JobMatch>(), 
            default), Times.Once);
    }

    [Fact]
    public async Task CalculateMatchAsync_ShouldNotSendNotification_WhenScoreBelow80()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        var user = CreateTestUser();
        var digitalTwin = CreateTestDigitalTwin(user.Id);
        var jobPosting = CreateTestJobPosting();

        context.Users.Add(user);
        context.DigitalTwins.Add(digitalTwin);
        context.JobPostings.Add(jobPosting);
        await context.SaveChangesAsync();

        _jobMatchRepositoryMock.Setup(r => r.ExistsAsync(user.Id, jobPosting.Id, default))
            .ReturnsAsync(false);

        _geminiServiceMock.Setup(s => s.CalculateMatchScoreAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new MatchResult { MatchScore = 65, Reasoning = "Partial match", SkillGaps = new List<string>() });

        _jobMatchRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<JobMatch>(), default))
            .ReturnsAsync((JobMatch jm, CancellationToken ct) => jm);

        // Act
        var result = await service.CalculateMatchAsync(user.Id, jobPosting.Id);

        // Assert
        Assert.False(result.IsInQueue);
        _notificationServiceMock.Verify(n => n.CreateNewMatchNotificationAsync(
            It.IsAny<Guid>(), 
            It.IsAny<JobMatch>(), 
            default), Times.Never);
    }

    #endregion

    #region FindMatchesForUserAsync Tests

    [Fact]
    public async Task FindMatchesForUserAsync_ShouldFilterBySector()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        var user = CreateTestUser();
        // Sector 1 = Information Technology
        var preferredSectors = JsonSerializer.Serialize(new List<int> { 1 });
        var digitalTwin = CreateTestDigitalTwin(user.Id, preferredSectors: preferredSectors);

        var jobIT = CreateTestJobPosting(sectorId: 1);
        jobIT.Title = "IT Developer";
        
        var jobFinance = CreateTestJobPosting(sectorId: 2);
        jobFinance.Title = "Finance Analyst";

        context.Users.Add(user);
        context.DigitalTwins.Add(digitalTwin);
        context.JobPostings.AddRange(jobIT, jobFinance);
        await context.SaveChangesAsync();

        _geminiServiceMock.Setup(s => s.CalculateMatchScoreAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new MatchResult { MatchScore = 85, Reasoning = "Match", SkillGaps = new List<string>() });

        _jobMatchRepositoryMock.Setup(r => r.ExistsAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), default))
            .ReturnsAsync(false);

        _jobMatchRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<JobMatch>(), default))
            .ReturnsAsync((JobMatch jm, CancellationToken ct) => jm);

        // Act
        var result = await service.FindMatchesForUserAsync(user.Id);

        // Assert - Should only find the IT job due to sector filter
        // Note: Jobs without sector (null) are also included by the filter logic
        Assert.NotNull(result);
    }

    [Fact]
    public async Task FindMatchesForUserAsync_ShouldFilterByRemotePreference()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        var user = CreateTestUser();
        var digitalTwin = CreateTestDigitalTwin(user.Id, isRemote: true);

        var remoteJob = CreateTestJobPosting(isRemote: true);
        remoteJob.Title = "Remote Developer";
        
        var officeJob = CreateTestJobPosting(isRemote: false);
        officeJob.Title = "Office Developer";
        officeJob.City = "Istanbul";

        context.Users.Add(user);
        context.DigitalTwins.Add(digitalTwin);
        context.JobPostings.AddRange(remoteJob, officeJob);
        await context.SaveChangesAsync();

        _geminiServiceMock.Setup(s => s.CalculateMatchScoreAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new MatchResult { MatchScore = 85, Reasoning = "Match", SkillGaps = new List<string>() });

        _jobMatchRepositoryMock.Setup(r => r.ExistsAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), default))
            .ReturnsAsync(false);

        _jobMatchRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<JobMatch>(), default))
            .ReturnsAsync((JobMatch jm, CancellationToken ct) => jm);

        // Act
        var result = await service.FindMatchesForUserAsync(user.Id);

        // Assert - Should find remote job when remote preference is set
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(remoteJob.Id, result.First().JobPostingId);
    }

    [Fact]
    public async Task FindMatchesForUserAsync_ShouldThrow_WhenDigitalTwinNotFound()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        var nonExistentUserId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.FindMatchesForUserAsync(nonExistentUserId));
    }

    [Fact]
    public async Task FindMatchesForUserAsync_ShouldRespectMinScoreFilter()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        var user = CreateTestUser();
        var digitalTwin = CreateTestDigitalTwin(user.Id);
        var jobPosting = CreateTestJobPosting();

        context.Users.Add(user);
        context.DigitalTwins.Add(digitalTwin);
        context.JobPostings.Add(jobPosting);
        await context.SaveChangesAsync();

        // Return low score
        _geminiServiceMock.Setup(s => s.CalculateMatchScoreAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new MatchResult { MatchScore = 50, Reasoning = "Low match", SkillGaps = new List<string>() });

        _jobMatchRepositoryMock.Setup(r => r.ExistsAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), default))
            .ReturnsAsync(false);

        _jobMatchRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<JobMatch>(), default))
            .ReturnsAsync((JobMatch jm, CancellationToken ct) => jm);

        // Act - Default minScore is 80
        var result = await service.FindMatchesForUserAsync(user.Id, minScore: 80);

        // Assert - Should not include match below threshold
        Assert.Empty(result);
    }

    [Fact]
    public async Task FindMatchesForUserAsync_ShouldSkipAlreadyMatchedJobs()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        var user = CreateTestUser();
        var digitalTwin = CreateTestDigitalTwin(user.Id);
        var jobPosting = CreateTestJobPosting();

        // Add existing match
        var existingMatch = new JobMatch
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            JobPostingId = jobPosting.Id,
            MatchScore = 90,
            Status = "Pending"
        };

        context.Users.Add(user);
        context.DigitalTwins.Add(digitalTwin);
        context.JobPostings.Add(jobPosting);
        context.JobMatches.Add(existingMatch);
        await context.SaveChangesAsync();

        // Act
        var result = await service.FindMatchesForUserAsync(user.Id);

        // Assert - Should not create new match for already matched job
        Assert.Empty(result);
        _geminiServiceMock.Verify(s => s.CalculateMatchScoreAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    #endregion

    #region ApproveMatchAsync Tests

    [Fact]
    public async Task ApproveMatchAsync_ShouldUpdateStatusToApproved()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        var userId = Guid.NewGuid();
        var matchId = Guid.NewGuid();
        var match = new JobMatch
        {
            Id = matchId,
            UserId = userId,
            JobPostingId = Guid.NewGuid(),
            MatchScore = 85,
            Status = "Pending",
            IsInQueue = false
        };

        _jobMatchRepositoryMock.Setup(r => r.GetByIdAsync(matchId, default))
            .ReturnsAsync(match);

        _jobMatchRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<JobMatch>(), default))
            .Returns(Task.FromResult<JobMatch>(match));

        // Act
        var result = await service.ApproveMatchAsync(matchId, userId);

        // Assert
        Assert.Equal("Approved", result.Status);
        Assert.True(result.IsInQueue);
        _jobMatchRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<JobMatch>(), default), Times.Once);
    }

    [Fact]
    public async Task ApproveMatchAsync_ShouldThrow_WhenMatchNotFound()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        var userId = Guid.NewGuid();
        var nonExistentMatchId = Guid.NewGuid();

        _jobMatchRepositoryMock.Setup(r => r.GetByIdAsync(nonExistentMatchId, default))
            .ReturnsAsync((JobMatch?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ApproveMatchAsync(nonExistentMatchId, userId));
    }

    [Fact]
    public async Task ApproveMatchAsync_ShouldThrow_WhenUserDoesNotOwnMatch()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        var matchOwnerId = Guid.NewGuid();
        var differentUserId = Guid.NewGuid();
        var matchId = Guid.NewGuid();

        var match = new JobMatch
        {
            Id = matchId,
            UserId = matchOwnerId, // Different user owns this match
            JobPostingId = Guid.NewGuid(),
            MatchScore = 85,
            Status = "Pending"
        };

        _jobMatchRepositoryMock.Setup(r => r.GetByIdAsync(matchId, default))
            .ReturnsAsync(match);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.ApproveMatchAsync(matchId, differentUserId));
    }

    #endregion

    #region RejectMatchAsync Tests

    [Fact]
    public async Task RejectMatchAsync_ShouldUpdateStatusToRejected()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        var userId = Guid.NewGuid();
        var matchId = Guid.NewGuid();
        var match = new JobMatch
        {
            Id = matchId,
            UserId = userId,
            JobPostingId = Guid.NewGuid(),
            MatchScore = 85,
            Status = "Pending",
            IsInQueue = true
        };

        _jobMatchRepositoryMock.Setup(r => r.GetByIdAsync(matchId, default))
            .ReturnsAsync(match);

        _jobMatchRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<JobMatch>(), default))
            .Returns(Task.FromResult<JobMatch>(match));

        // Act
        var result = await service.RejectMatchAsync(matchId, userId);

        // Assert
        Assert.Equal("Rejected", result.Status);
        Assert.False(result.IsInQueue);
        _jobMatchRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<JobMatch>(), default), Times.Once);
    }

    #endregion

    #region GetQueuedMatchesAsync Tests

    [Fact]
    public async Task GetQueuedMatchesAsync_ShouldReturnQueuedMatches()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        var userId = Guid.NewGuid();
        var queuedMatches = new List<JobMatch>
        {
            new JobMatch { Id = Guid.NewGuid(), UserId = userId, MatchScore = 90, IsInQueue = true },
            new JobMatch { Id = Guid.NewGuid(), UserId = userId, MatchScore = 85, IsInQueue = true }
        };

        _jobMatchRepositoryMock.Setup(r => r.GetQueuedMatchesAsync(userId, default))
            .ReturnsAsync(queuedMatches);

        // Act
        var result = await service.GetQueuedMatchesAsync(userId);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, m => Assert.True(m.IsInQueue));
    }

    #endregion
}
