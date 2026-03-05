using DistroCv.Core.Entities;
using DistroCv.Core.Interfaces;
using DistroCv.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace DistroCv.Api.Tests.Integration;

/// <summary>
/// Integration tests for interview preparation flow (Task 24.5)
/// Tests the end-to-end flow from application to interview coaching
/// </summary>
public class InterviewPreparationIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public InterviewPreparationIntegrationTests(TestWebApplicationFactory factory)
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

        // Setup Gemini for question generation
        _factory.GeminiServiceMock
            .Setup(x => x.GenerateContentAsync(It.Is<string>(s => s.Contains("interview questions"))))
            .ReturnsAsync(@"[
                ""Tell me about your experience with .NET development"",
                ""How do you approach microservices architecture?"",
                ""Describe a challenging project you worked on"",
                ""What experience do you have with Azure?"",
                ""How do you handle tight deadlines?"",
                ""Describe your experience with agile methodologies"",
                ""What's your approach to code reviews?"",
                ""How do you stay updated with new technologies?"",
                ""Tell me about a time you resolved a team conflict"",
                ""Where do you see yourself in 5 years?""
            ]");

        // Setup Gemini for answer analysis (STAR technique)
        _factory.GeminiServiceMock
            .Setup(x => x.GenerateContentAsync(It.Is<string>(s => s.Contains("STAR"))))
            .ReturnsAsync(@"**STAR Analysis**

**Situation**: Good context provided - clearly described the scenario
**Task**: Clear task description - objective was well defined
**Action**: Actions were well explained - specific steps mentioned
**Result**: Quantifiable results mentioned - measurable outcomes provided

**Overall Score**: 85/100

**Suggestions**: 
- Consider adding more specific metrics
- Could elaborate on the technical challenges
- Great use of the STAR method overall");

        // Setup Gemini for improvement suggestions
        _factory.GeminiServiceMock
            .Setup(x => x.GenerateContentAsync(It.Is<string>(s => s.Contains("improvement suggestions"))))
            .ReturnsAsync(@"[
                ""Practice using the STAR method for all behavioral questions"",
                ""Prepare specific metrics and numbers to quantify achievements"",
                ""Develop 2-3 detailed project stories that showcase different skills""
            ]");

        // Default content generation
        _factory.GeminiServiceMock
            .Setup(x => x.GenerateContentAsync(It.Is<string>(s => !s.Contains("interview") && !s.Contains("STAR") && !s.Contains("improvement"))))
            .ReturnsAsync("Generated content");
    }

    [Fact]
    public async Task InterviewCoach_ShouldGenerateQuestions_ForApplication()
    {
        // Arrange
        var user = await _factory.CreateTestUserAsync("interview@example.com", "Interview User");
        var digitalTwin = await _factory.CreateDigitalTwinAsync(user.Id);
        var jobPosting = await _factory.CreateJobPostingAsync("Software Developer");

        // Create a verified company for richer context
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DistroCvDbContext>();

        var company = new VerifiedCompany
        {
            Id = Guid.NewGuid(),
            Name = "Tech Corp",
            Website = "https://techcorp.com",
            CompanyCulture = "Innovative, agile, team-oriented",
            IsVerified = true,
            VerifiedAt = DateTime.UtcNow
        };
        dbContext.VerifiedCompanies.Add(company);
        await dbContext.SaveChangesAsync();

        var interviewCoachService = scope.ServiceProvider.GetRequiredService<IInterviewCoachService>();

        // Act
        var questions = await interviewCoachService.GenerateQuestionsAsync(jobPosting, company);

        // Assert
        Assert.NotNull(questions);
        Assert.Equal(10, questions.Count);
        Assert.Contains(questions, q => q.Contains(".NET"));
    }

    [Fact]
    public async Task InterviewCoach_ShouldAnalyzeAnswer_UsingSTARMethod()
    {
        // Arrange
        var user = await _factory.CreateTestUserAsync("star@example.com", "STAR User");
        var digitalTwin = await _factory.CreateDigitalTwinAsync(user.Id);

        using var scope = _factory.Services.CreateScope();
        var interviewCoachService = scope.ServiceProvider.GetRequiredService<IInterviewCoachService>();

        var question = "Tell me about a challenging project you worked on";
        var answer = @"In my previous role at Tech Corp, we needed to migrate a monolithic application to microservices.
        My task was to lead the backend team and architect the new system.
        I designed the service boundaries, implemented API gateways, and set up CI/CD pipelines.
        As a result, we reduced deployment time by 70% and improved system reliability from 95% to 99.9% uptime.";

        // Act
        var feedback = await interviewCoachService.AnalyzeAnswerAsync(question, answer);

        // Assert
        Assert.NotNull(feedback);
        Assert.Contains("STAR", feedback);
        Assert.Contains("Score", feedback);
    }

    [Fact]
    public async Task InterviewCoach_ShouldGenerateImprovementSuggestions()
    {
        // Arrange
        var user = await _factory.CreateTestUserAsync("improve@example.com", "Improve User");

        using var scope = _factory.Services.CreateScope();
        var interviewCoachService = scope.ServiceProvider.GetRequiredService<IInterviewCoachService>();

        var answers = new List<string>
        {
            "I fixed a bug in the system.",
            "I worked on a team project.",
            "I improved performance by optimizing queries."
        };

        // Act
        var suggestions = await interviewCoachService.GenerateImprovementSuggestionsAsync(answers);

        // Assert
        Assert.NotNull(suggestions);
        Assert.True(suggestions.Count > 0);
        Assert.Contains(suggestions, s => s.Contains("STAR"));
    }

    [Fact]
    public async Task InterviewCoach_ShouldCreateAndStorePreparation()
    {
        // Arrange
        var user = await _factory.CreateTestUserAsync("prep@example.com", "Prep User");
        var digitalTwin = await _factory.CreateDigitalTwinAsync(user.Id);
        var jobPosting = await _factory.CreateJobPostingAsync("Backend Developer");

        using var scope = _factory.Services.CreateScope();
        var matchingService = scope.ServiceProvider.GetRequiredService<IMatchingService>();
        var applicationRepository = scope.ServiceProvider.GetRequiredService<IApplicationRepository>();
        var interviewCoachService = scope.ServiceProvider.GetRequiredService<IInterviewCoachService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<DistroCvDbContext>();

        // Create application flow
        var match = await matchingService.CalculateMatchAsync(user.Id, jobPosting.Id);
        var application = new Application
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            JobMatchId = match.Id,
            Status = "Sent",
            CreatedAt = DateTime.UtcNow
        };
        await applicationRepository.CreateAsync(application);

        // Act
        var preparation = await interviewCoachService.CreatePreparationAsync(application.Id);

        // Assert
        Assert.NotNull(preparation);
        Assert.Equal(application.Id, preparation.ApplicationId);

        // Verify preparation was saved
        var savedPrep = await dbContext.InterviewPreparations
            .FirstOrDefaultAsync(p => p.ApplicationId == application.Id);
        Assert.NotNull(savedPrep);
    }

    [Fact]
    public async Task InterviewCoach_ShouldGetExistingPreparation()
    {
        // Arrange
        var user = await _factory.CreateTestUserAsync("getprep@example.com", "Get Prep User");
        var digitalTwin = await _factory.CreateDigitalTwinAsync(user.Id);
        var jobPosting = await _factory.CreateJobPostingAsync("Frontend Developer");

        using var scope = _factory.Services.CreateScope();
        var matchingService = scope.ServiceProvider.GetRequiredService<IMatchingService>();
        var applicationRepository = scope.ServiceProvider.GetRequiredService<IApplicationRepository>();
        var interviewCoachService = scope.ServiceProvider.GetRequiredService<IInterviewCoachService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<DistroCvDbContext>();

        // Create application and preparation
        var match = await matchingService.CalculateMatchAsync(user.Id, jobPosting.Id);
        var application = new Application
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            JobMatchId = match.Id,
            Status = "Sent",
            CreatedAt = DateTime.UtcNow
        };
        await applicationRepository.CreateAsync(application);

        var existingPrep = new InterviewPreparation
        {
            Id = Guid.NewGuid(),
            ApplicationId = application.Id,
            Questions = "[\"Q1\", \"Q2\"]",
            UserAnswers = "[\"A1\", \"A2\"]",
            Feedback = "[\"F1\", \"F2\"]",
            CreatedAt = DateTime.UtcNow
        };
        dbContext.InterviewPreparations.Add(existingPrep);
        await dbContext.SaveChangesAsync();

        // Act
        var retrieved = await interviewCoachService.GetPreparationAsync(application.Id);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(existingPrep.Id, retrieved.Id);
        Assert.NotNull(retrieved.Questions);
        Assert.NotNull(retrieved.UserAnswers);
    }

    [Fact]
    public async Task InterviewCoach_ShouldGenerateQuestions_WithoutCompany()
    {
        // Arrange
        var user = await _factory.CreateTestUserAsync("nocompany@example.com", "No Company User");
        var jobPosting = await _factory.CreateJobPostingAsync("Data Engineer");
        
        using var scope = _factory.Services.CreateScope();
        var interviewCoachService = scope.ServiceProvider.GetRequiredService<IInterviewCoachService>();

        // Act - Generate questions without verified company
        var questions = await interviewCoachService.GenerateQuestionsAsync(jobPosting, null);

        // Assert
        Assert.NotNull(questions);
        Assert.True(questions.Count > 0);
    }

    [Fact]
    public async Task InterviewPreparation_FullFlow_ShouldWork()
    {
        // Arrange
        var user = await _factory.CreateTestUserAsync("fullflow@example.com", "Full Flow User");
        var digitalTwin = await _factory.CreateDigitalTwinAsync(user.Id);
        var jobPosting = await _factory.CreateJobPostingAsync("Solutions Architect");

        using var scope = _factory.Services.CreateScope();
        var matchingService = scope.ServiceProvider.GetRequiredService<IMatchingService>();
        var applicationRepository = scope.ServiceProvider.GetRequiredService<IApplicationRepository>();
        var interviewCoachService = scope.ServiceProvider.GetRequiredService<IInterviewCoachService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<DistroCvDbContext>();

        // Step 1: Create match and application
        var match = await matchingService.CalculateMatchAsync(user.Id, jobPosting.Id);
        var application = new Application
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            JobMatchId = match.Id,
            Status = "Sent",
            CreatedAt = DateTime.UtcNow
        };
        await applicationRepository.CreateAsync(application);

        // Step 2: Generate interview questions
        var questions = await interviewCoachService.GenerateQuestionsAsync(jobPosting, null);
        Assert.NotNull(questions);
        Assert.True(questions.Count > 0);

        // Step 3: Create preparation
        var preparation = await interviewCoachService.CreatePreparationAsync(application.Id);
        Assert.NotNull(preparation);

        // Step 4: Analyze an answer
        var answer = "I led the migration of our legacy system to cloud infrastructure.";
        var feedback = await interviewCoachService.AnalyzeAnswerAsync(questions[0], answer);
        Assert.NotNull(feedback);
        Assert.Contains("STAR", feedback);

        // Step 5: Get improvement suggestions
        var answers = new List<string> { answer };
        var suggestions = await interviewCoachService.GenerateImprovementSuggestionsAsync(answers);
        Assert.NotNull(suggestions);
        Assert.True(suggestions.Count > 0);

        // Verify preparation is stored
        var savedPrep = await dbContext.InterviewPreparations
            .FirstOrDefaultAsync(p => p.ApplicationId == application.Id);
        Assert.NotNull(savedPrep);
    }

    [Fact]
    public async Task InterviewCoach_ShouldReturnNull_WhenPreparationNotExists()
    {
        // Arrange
        var nonExistentApplicationId = Guid.NewGuid();

        using var scope = _factory.Services.CreateScope();
        var interviewCoachService = scope.ServiceProvider.GetRequiredService<IInterviewCoachService>();

        // Act
        var result = await interviewCoachService.GetPreparationAsync(nonExistentApplicationId);

        // Assert
        Assert.Null(result);
    }
}

