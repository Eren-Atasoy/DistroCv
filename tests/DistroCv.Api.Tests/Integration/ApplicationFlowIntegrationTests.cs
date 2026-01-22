using DistroCv.Core.Entities;
using DistroCv.Core.Interfaces;
using DistroCv.Infrastructure.Data;
using DistroCv.Infrastructure.Gmail;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace DistroCv.Api.Tests.Integration;

/// <summary>
/// Integration tests for application creation and sending flow (Task 24.4)
/// Tests the end-to-end flow from match approval to application submission
/// </summary>
public class ApplicationFlowIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public ApplicationFlowIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        SetupMocks();
    }

    private void SetupMocks()
    {
        // Setup Gemini for match calculation
        _factory.GeminiServiceMock
            .Setup(x => x.CalculateMatchScoreAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new MatchResult
            {
                MatchScore = 85,
                Reasoning = "Good match",
                SkillGaps = new List<string>()
            });

        // Setup Gemini for email generation
        _factory.GeminiServiceMock
            .Setup(x => x.GenerateContentAsync(It.IsAny<string>()))
            .ReturnsAsync(@"Subject: Application for Software Developer Position

Dear Hiring Manager,

I am writing to express my interest in the Software Developer position at Tech Corp.

With my background in C# and .NET development, I believe I would be a strong addition to your team.

Best regards,
Test User");

        // Setup Gmail service mock
        _factory.GmailServiceMock
            .Setup(x => x.SendEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<List<EmailAttachment>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("msg_12345");
    }

    [Fact]
    public async Task ApplicationFlow_ShouldCreateApplication_FromApprovedMatch()
    {
        // Arrange
        var user = await _factory.CreateTestUserAsync("appflow@example.com", "App Flow User");
        var digitalTwin = await _factory.CreateDigitalTwinAsync(user.Id);
        var jobPosting = await _factory.CreateJobPostingAsync("Software Developer");

        using var scope = _factory.Services.CreateScope();
        var matchingService = scope.ServiceProvider.GetRequiredService<IMatchingService>();
        var applicationRepository = scope.ServiceProvider.GetRequiredService<IApplicationRepository>();
        var dbContext = scope.ServiceProvider.GetRequiredService<DistroCvDbContext>();

        // Create and approve match
        var match = await matchingService.CalculateMatchAsync(user.Id, jobPosting.Id);
        var approvedMatch = await matchingService.ApproveMatchAsync(match.Id, user.Id);

        // Act - Create application from approved match
        var application = new Application
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            JobMatchId = approvedMatch.Id,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };
        
        await applicationRepository.CreateAsync(application);

        // Assert
        var savedApplication = await dbContext.Applications
            .FirstOrDefaultAsync(a => a.JobMatchId == approvedMatch.Id);
        
        Assert.NotNull(savedApplication);
        Assert.Equal(user.Id, savedApplication.UserId);
        Assert.Equal("Pending", savedApplication.Status);
    }

    [Fact]
    public async Task ApplicationDistribution_ShouldSendEmail()
    {
        // Arrange
        var user = await _factory.CreateTestUserAsync("email@example.com", "Email User");
        var digitalTwin = await _factory.CreateDigitalTwinAsync(user.Id);
        var jobPosting = await _factory.CreateJobPostingAsync("Backend Developer");

        using var scope = _factory.Services.CreateScope();
        var matchingService = scope.ServiceProvider.GetRequiredService<IMatchingService>();
        var applicationDistributionService = scope.ServiceProvider.GetRequiredService<IApplicationDistributionService>();
        var applicationRepository = scope.ServiceProvider.GetRequiredService<IApplicationRepository>();
        var dbContext = scope.ServiceProvider.GetRequiredService<DistroCvDbContext>();

        // Create match, approve, and create application
        var match = await matchingService.CalculateMatchAsync(user.Id, jobPosting.Id);
        var approvedMatch = await matchingService.ApproveMatchAsync(match.Id, user.Id);

        var application = new Application
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            JobMatchId = approvedMatch.Id,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };
        await applicationRepository.CreateAsync(application);

        // Act
        var result = await applicationDistributionService.SendViaEmailAsync(application.Id);

        // Assert
        Assert.True(result);
        
        // Verify email was sent
        _factory.GmailServiceMock.Verify(
            x => x.SendEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<List<EmailAttachment>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        // Verify application status was updated
        var updatedApplication = await dbContext.Applications
            .FirstOrDefaultAsync(a => a.Id == application.Id);
        Assert.Equal("Sent", updatedApplication?.Status);
        Assert.NotNull(updatedApplication?.SentAt);
    }

    [Fact]
    public async Task ApplicationDistribution_ShouldGeneratePersonalizedEmail()
    {
        // Arrange
        var user = await _factory.CreateTestUserAsync("personalized@example.com", "Personalized User");
        var digitalTwin = await _factory.CreateDigitalTwinAsync(user.Id);
        var jobPosting = await _factory.CreateJobPostingAsync("Full Stack Developer");

        using var scope = _factory.Services.CreateScope();
        var matchingService = scope.ServiceProvider.GetRequiredService<IMatchingService>();
        var applicationDistributionService = scope.ServiceProvider.GetRequiredService<IApplicationDistributionService>();
        var applicationRepository = scope.ServiceProvider.GetRequiredService<IApplicationRepository>();

        // Create match, approve, and create application
        var match = await matchingService.CalculateMatchAsync(user.Id, jobPosting.Id);
        var approvedMatch = await matchingService.ApproveMatchAsync(match.Id, user.Id);

        var application = new Application
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            JobMatchId = approvedMatch.Id,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };
        await applicationRepository.CreateAsync(application);

        // Act
        var emailContent = await applicationDistributionService.GeneratePersonalizedEmailAsync(application.Id);

        // Assert
        Assert.NotNull(emailContent);
        Assert.NotEmpty(emailContent.Subject);
        Assert.NotEmpty(emailContent.Body);
        Assert.Contains("Software Developer", emailContent.Subject);
    }

    [Fact]
    public async Task ApplicationDistribution_ShouldUpdateStatusCorrectly()
    {
        // Arrange
        var user = await _factory.CreateTestUserAsync("status@example.com", "Status User");
        var digitalTwin = await _factory.CreateDigitalTwinAsync(user.Id);
        var jobPosting = await _factory.CreateJobPostingAsync("DevOps Engineer");

        using var scope = _factory.Services.CreateScope();
        var matchingService = scope.ServiceProvider.GetRequiredService<IMatchingService>();
        var applicationDistributionService = scope.ServiceProvider.GetRequiredService<IApplicationDistributionService>();
        var applicationRepository = scope.ServiceProvider.GetRequiredService<IApplicationRepository>();
        var dbContext = scope.ServiceProvider.GetRequiredService<DistroCvDbContext>();

        // Create match and application
        var match = await matchingService.CalculateMatchAsync(user.Id, jobPosting.Id);
        var approvedMatch = await matchingService.ApproveMatchAsync(match.Id, user.Id);

        var application = new Application
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            JobMatchId = approvedMatch.Id,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };
        await applicationRepository.CreateAsync(application);

        // Act
        await applicationDistributionService.UpdateApplicationStatusAsync(
            application.Id, 
            "Sent", 
            "Email sent successfully");

        // Assert
        var updatedApplication = await dbContext.Applications
            .FirstOrDefaultAsync(a => a.Id == application.Id);
        Assert.Equal("Sent", updatedApplication?.Status);

        // Verify log was created
        var log = await dbContext.ApplicationLogs
            .FirstOrDefaultAsync(l => l.ApplicationId == application.Id);
        Assert.NotNull(log);
        Assert.Contains("Sent", log.Details);
    }

    [Fact]
    public async Task ApplicationFlow_ShouldTrackApplicationHistory()
    {
        // Arrange
        var user = await _factory.CreateTestUserAsync("history@example.com", "History User");
        var digitalTwin = await _factory.CreateDigitalTwinAsync(user.Id);
        var jobPosting = await _factory.CreateJobPostingAsync("Platform Engineer");

        using var scope = _factory.Services.CreateScope();
        var matchingService = scope.ServiceProvider.GetRequiredService<IMatchingService>();
        var applicationDistributionService = scope.ServiceProvider.GetRequiredService<IApplicationDistributionService>();
        var applicationRepository = scope.ServiceProvider.GetRequiredService<IApplicationRepository>();
        var dbContext = scope.ServiceProvider.GetRequiredService<DistroCvDbContext>();

        // Create application
        var match = await matchingService.CalculateMatchAsync(user.Id, jobPosting.Id);
        var approvedMatch = await matchingService.ApproveMatchAsync(match.Id, user.Id);

        var application = new Application
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            JobMatchId = approvedMatch.Id,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };
        await applicationRepository.CreateAsync(application);

        // Act - Update status multiple times
        await applicationDistributionService.UpdateApplicationStatusAsync(application.Id, "Processing", "Starting process");
        await applicationDistributionService.UpdateApplicationStatusAsync(application.Id, "Sent", "Email sent");

        // Assert - Check history logs
        var logs = await dbContext.ApplicationLogs
            .Where(l => l.ApplicationId == application.Id)
            .OrderBy(l => l.Timestamp)
            .ToListAsync();

        Assert.Equal(2, logs.Count);
        Assert.Contains(logs, l => l.Details.Contains("Processing"));
        Assert.Contains(logs, l => l.Details.Contains("Sent"));
    }

    [Fact]
    public async Task ApplicationRepository_ShouldGetUserApplications()
    {
        // Arrange
        var user = await _factory.CreateTestUserAsync("getapps@example.com", "Get Apps User");
        var digitalTwin = await _factory.CreateDigitalTwinAsync(user.Id);
        var job1 = await _factory.CreateJobPostingAsync("Job 1");
        var job2 = await _factory.CreateJobPostingAsync("Job 2");

        using var scope = _factory.Services.CreateScope();
        var matchingService = scope.ServiceProvider.GetRequiredService<IMatchingService>();
        var applicationRepository = scope.ServiceProvider.GetRequiredService<IApplicationRepository>();

        // Create two applications
        var match1 = await matchingService.CalculateMatchAsync(user.Id, job1.Id);
        var match2 = await matchingService.CalculateMatchAsync(user.Id, job2.Id);

        var app1 = new Application { Id = Guid.NewGuid(), UserId = user.Id, JobMatchId = match1.Id, Status = "Pending", CreatedAt = DateTime.UtcNow };
        var app2 = new Application { Id = Guid.NewGuid(), UserId = user.Id, JobMatchId = match2.Id, Status = "Sent", CreatedAt = DateTime.UtcNow };
        
        await applicationRepository.CreateAsync(app1);
        await applicationRepository.CreateAsync(app2);

        // Act
        var applications = await applicationRepository.GetByUserIdAsync(user.Id);

        // Assert
        var appList = applications.ToList();
        Assert.Equal(2, appList.Count);
        Assert.Contains(appList, a => a.Status == "Pending");
        Assert.Contains(appList, a => a.Status == "Sent");
    }
}

