using DistroCv.Core.Entities;
using DistroCv.Core.Interfaces;
using DistroCv.Infrastructure.Data;
using DistroCv.Infrastructure.Gmail;
using EmailAttachment = DistroCv.Infrastructure.Gmail.EmailAttachment;
using DistroCv.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DistroCv.Api.Tests.Services;

/// <summary>
/// Unit tests for ApplicationDistributionService
/// Task 23.6: Test email and LinkedIn distribution
/// </summary>
public class ApplicationDistributionServiceTests
{
    private readonly Mock<IGeminiService> _geminiServiceMock;
    private readonly Mock<IGmailService> _gmailServiceMock;
    private readonly Mock<ILogger<ApplicationDistributionService>> _loggerMock;
    private readonly DbContextOptions<DistroCvDbContext> _dbContextOptions;

    public ApplicationDistributionServiceTests()
    {
        _geminiServiceMock = new Mock<IGeminiService>();
        _gmailServiceMock = new Mock<IGmailService>();
        _loggerMock = new Mock<ILogger<ApplicationDistributionService>>();
        _dbContextOptions = new DbContextOptionsBuilder<DistroCvDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    private DistroCvDbContext CreateDbContext() => new DistroCvDbContext(_dbContextOptions);

    private ApplicationDistributionService CreateService(DistroCvDbContext context)
    {
        return new ApplicationDistributionService(
            context,
            _geminiServiceMock.Object,
            _gmailServiceMock.Object,
            _loggerMock.Object);
    }

    #region Test Data Setup

    private (User user, DigitalTwin twin, JobPosting job, JobMatch match, Application application) CreateTestData(DistroCvDbContext context)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "applicant@example.com",
            FullName = "John Doe",
            CreatedAt = DateTime.UtcNow
        };

        var twin = new DigitalTwin
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Skills = "[\"C#\", \".NET\"]",
            Experience = "[{\"title\": \"Developer\", \"years\": 3}]",
            CareerGoals = "Become a senior developer"
        };

        var job = new JobPosting
        {
            Id = Guid.NewGuid(),
            Title = "Software Developer",
            CompanyName = "Tech Corp",
            Description = "Looking for .NET developer",
            Location = "Istanbul",
            SourceUrl = "https://linkedin.com/jobs/view/123",
            SourcePlatform = "LinkedIn",
            IsActive = true
        };

        var match = new JobMatch
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            JobPostingId = job.Id,
            MatchScore = 85,
            Status = "Approved"
        };

        var application = new Application
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            JobMatchId = match.Id,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        user.DigitalTwin = twin;
        match.JobPosting = job;
        application.JobMatch = match;
        application.User = user;

        context.Users.Add(user);
        context.DigitalTwins.Add(twin);
        context.JobPostings.Add(job);
        context.JobMatches.Add(match);
        context.Applications.Add(application);

        return (user, twin, job, match, application);
    }

    #endregion

    #region SendViaEmailAsync Tests

    [Fact]
    public async Task SendViaEmailAsync_ShouldSendEmail_WhenApplicationExists()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        var (user, twin, job, match, application) = CreateTestData(context);
        await context.SaveChangesAsync();

        var emailSubject = "Application for Software Developer";
        var emailBody = "Dear Hiring Manager...";

        _geminiServiceMock.Setup(s => s.GenerateContentAsync(It.IsAny<string>()))
            .ReturnsAsync($"Subject: {emailSubject}\n\n{emailBody}");

        _gmailServiceMock.Setup(s => s.SendEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<List<EmailAttachment>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("msg_12345");

        // Act
        var result = await service.SendViaEmailAsync(application.Id);

        // Assert
        Assert.True(result);
        _gmailServiceMock.Verify(s => s.SendEmailAsync(
            user.Email,
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<List<EmailAttachment>?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendViaEmailAsync_ShouldUpdateStatus_WhenSuccessful()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        var (user, twin, job, match, application) = CreateTestData(context);
        await context.SaveChangesAsync();

        _geminiServiceMock.Setup(s => s.GenerateContentAsync(It.IsAny<string>()))
            .ReturnsAsync("Subject: Test\n\nBody content");

        _gmailServiceMock.Setup(s => s.SendEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<List<EmailAttachment>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("msg_12345");

        // Act
        await service.SendViaEmailAsync(application.Id);

        // Assert - Check status was updated
        var updatedApp = await context.Applications.FindAsync(application.Id);
        Assert.Equal("Sent", updatedApp?.Status);
        Assert.NotNull(updatedApp?.SentAt);
    }

    [Fact]
    public async Task SendViaEmailAsync_ShouldThrow_WhenApplicationNotFound()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SendViaEmailAsync(nonExistentId));
    }

    [Fact]
    public async Task SendViaEmailAsync_ShouldLogError_WhenEmailFails()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        var (user, twin, job, match, application) = CreateTestData(context);
        await context.SaveChangesAsync();

        _geminiServiceMock.Setup(s => s.GenerateContentAsync(It.IsAny<string>()))
            .ReturnsAsync("Subject: Test\n\nBody");

        _gmailServiceMock.Setup(s => s.SendEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<List<EmailAttachment>?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("SMTP Error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() =>
            service.SendViaEmailAsync(application.Id));

        // Verify error was logged
        var logs = await context.ApplicationLogs
            .Where(l => l.ApplicationId == application.Id && l.ActionType == "Error")
            .ToListAsync();
        Assert.NotEmpty(logs);
    }

    #endregion

    #region GeneratePersonalizedEmailAsync Tests

    [Fact]
    public async Task GeneratePersonalizedEmailAsync_ShouldReturnEmailContent()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        var (user, twin, job, match, application) = CreateTestData(context);
        await context.SaveChangesAsync();

        var expectedSubject = "Application for Software Developer Position";
        var expectedBody = "Dear Hiring Manager,\n\nI am excited to apply...";

        _geminiServiceMock.Setup(s => s.GenerateContentAsync(It.IsAny<string>()))
            .ReturnsAsync($"Subject: {expectedSubject}\n\n{expectedBody}");

        // Act
        var result = await service.GeneratePersonalizedEmailAsync(application.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedSubject, result.Subject);
        Assert.Contains("Hiring Manager", result.Body);
    }

    [Fact]
    public async Task GeneratePersonalizedEmailAsync_ShouldUseDefaultSubject_WhenNotInResponse()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        var (user, twin, job, match, application) = CreateTestData(context);
        await context.SaveChangesAsync();

        // Response without "Subject:" prefix
        _geminiServiceMock.Setup(s => s.GenerateContentAsync(It.IsAny<string>()))
            .ReturnsAsync("Just the email body without subject line");

        // Act
        var result = await service.GeneratePersonalizedEmailAsync(application.Id);

        // Assert
        Assert.Contains(job.Title, result.Subject); // Should use default with job title
    }

    [Fact]
    public async Task GeneratePersonalizedEmailAsync_ShouldIncludeRelevantInfo()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        var (user, twin, job, match, application) = CreateTestData(context);
        await context.SaveChangesAsync();

        string capturedPrompt = "";
        _geminiServiceMock.Setup(s => s.GenerateContentAsync(It.IsAny<string>()))
            .Callback<string>(prompt => capturedPrompt = prompt)
            .ReturnsAsync("Subject: Test\n\nBody");

        // Act
        await service.GeneratePersonalizedEmailAsync(application.Id);

        // Assert - Verify prompt includes relevant info
        Assert.Contains(job.Title, capturedPrompt);
        Assert.Contains(job.CompanyName, capturedPrompt);
        Assert.Contains(twin.Skills, capturedPrompt);
    }

    #endregion

    #region UpdateApplicationStatusAsync Tests

    [Fact]
    public async Task UpdateApplicationStatusAsync_ShouldUpdateStatus()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        var application = new Application
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            JobMatchId = Guid.NewGuid(),
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };
        context.Applications.Add(application);
        await context.SaveChangesAsync();

        // Act
        await service.UpdateApplicationStatusAsync(application.Id, "Sent", "Test notes");

        // Assert
        var updated = await context.Applications.FindAsync(application.Id);
        Assert.Equal("Sent", updated?.Status);
        Assert.NotNull(updated?.SentAt);
    }

    [Fact]
    public async Task UpdateApplicationStatusAsync_ShouldCreateLog()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        var application = new Application
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            JobMatchId = Guid.NewGuid(),
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };
        context.Applications.Add(application);
        await context.SaveChangesAsync();

        // Act
        await service.UpdateApplicationStatusAsync(application.Id, "Sent", "Email sent successfully");

        // Assert
        var logs = await context.ApplicationLogs
            .Where(l => l.ApplicationId == application.Id)
            .ToListAsync();
        Assert.NotEmpty(logs);
        Assert.Contains(logs, l => l.Details.Contains("Sent"));
    }

    [Fact]
    public async Task UpdateApplicationStatusAsync_ShouldThrow_WhenApplicationNotFound()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateApplicationStatusAsync(nonExistentId, "Sent"));
    }

    #endregion

    #region SendViaLinkedInAsync Tests (Limited - Requires Browser)

    [Fact]
    public async Task SendViaLinkedInAsync_ShouldThrow_WhenApplicationNotFound()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SendViaLinkedInAsync(nonExistentId));
    }

    // Note: Full LinkedIn automation tests would require mocking Playwright
    // or running integration tests with a real browser

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_ShouldNotThrow()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        // Act & Assert - Should not throw
        service.Dispose();
    }

    #endregion
}

