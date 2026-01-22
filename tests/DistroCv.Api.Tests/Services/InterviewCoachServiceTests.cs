using DistroCv.Core.Entities;
using DistroCv.Core.Interfaces;
using DistroCv.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DistroCv.Api.Tests.Services;

/// <summary>
/// Unit tests for InterviewCoachService
/// Task 23.7: Test question generation, answer analysis
/// </summary>
public class InterviewCoachServiceTests
{
    private readonly Mock<IInterviewPreparationRepository> _repositoryMock;
    private readonly Mock<IGeminiService> _geminiServiceMock;
    private readonly Mock<ILogger<InterviewCoachService>> _loggerMock;
    private readonly InterviewCoachService _service;

    public InterviewCoachServiceTests()
    {
        _repositoryMock = new Mock<IInterviewPreparationRepository>();
        _geminiServiceMock = new Mock<IGeminiService>();
        _loggerMock = new Mock<ILogger<InterviewCoachService>>();

        _service = new InterviewCoachService(
            _repositoryMock.Object,
            _geminiServiceMock.Object,
            _loggerMock.Object);
    }

    #region Test Data Setup

    private JobPosting CreateTestJobPosting()
    {
        return new JobPosting
        {
            Id = Guid.NewGuid(),
            Title = "Senior .NET Developer",
            Description = "Looking for experienced .NET developer with microservices experience",
            CompanyName = "Tech Corp",
            Requirements = "[\"C#\", \".NET Core\", \"Microservices\", \"Azure\"]",
            SourcePlatform = "LinkedIn",
            IsActive = true
        };
    }

    private VerifiedCompany CreateTestCompany()
    {
        return new VerifiedCompany
        {
            Id = Guid.NewGuid(),
            Name = "Tech Corp",
            CompanyCulture = "Innovative, agile, team-oriented",
            IsVerified = true
        };
    }

    #endregion

    #region GenerateQuestionsAsync Tests

    [Fact]
    public async Task GenerateQuestionsAsync_ShouldReturn10Questions()
    {
        // Arrange
        var jobPosting = CreateTestJobPosting();
        var company = CreateTestCompany();

        var questionsJson = @"[
            ""Tell me about your experience with .NET Core"",
            ""How do you approach microservices architecture?"",
            ""Describe a challenging project you worked on"",
            ""What experience do you have with Azure?"",
            ""How do you handle tight deadlines?"",
            ""Describe your experience with agile methodologies"",
            ""What's your approach to code reviews?"",
            ""How do you stay updated with new technologies?"",
            ""Tell me about a time you resolved a team conflict"",
            ""Where do you see yourself in 5 years?""
        ]";

        _geminiServiceMock.Setup(x => x.GenerateContentAsync(It.IsAny<string>()))
            .ReturnsAsync(questionsJson);

        // Act
        var questions = await _service.GenerateQuestionsAsync(jobPosting, company);

        // Assert
        Assert.NotNull(questions);
        Assert.Equal(10, questions.Count);
    }

    [Fact]
    public async Task GenerateQuestionsAsync_ShouldCallGeminiWithJobDetails()
    {
        // Arrange
        var jobPosting = CreateTestJobPosting();
        var company = CreateTestCompany();

        string capturedPrompt = "";
        _geminiServiceMock.Setup(x => x.GenerateContentAsync(It.IsAny<string>()))
            .Callback<string>(prompt => capturedPrompt = prompt)
            .ReturnsAsync("[\"Question 1\", \"Question 2\"]");

        // Act
        await _service.GenerateQuestionsAsync(jobPosting, company);

        // Assert - Prompt should include job title and requirements
        Assert.Contains("Senior .NET Developer", capturedPrompt);
        Assert.Contains(jobPosting.Description, capturedPrompt);
    }

    [Fact]
    public async Task GenerateQuestionsAsync_ShouldIncludeCompanyInfo()
    {
        // Arrange
        var jobPosting = CreateTestJobPosting();
        var company = CreateTestCompany();

        string capturedPrompt = "";
        _geminiServiceMock.Setup(x => x.GenerateContentAsync(It.IsAny<string>()))
            .Callback<string>(prompt => capturedPrompt = prompt)
            .ReturnsAsync("[\"Question 1\"]");

        // Act
        await _service.GenerateQuestionsAsync(jobPosting, company);

        // Assert - Prompt should include company info
        Assert.Contains("Tech Corp", capturedPrompt);
    }

    [Fact]
    public async Task GenerateQuestionsAsync_WithoutCompany_ShouldStillGenerate()
    {
        // Arrange
        var jobPosting = CreateTestJobPosting();

        _geminiServiceMock.Setup(x => x.GenerateContentAsync(It.IsAny<string>()))
            .ReturnsAsync("[\"Generic question 1\", \"Generic question 2\"]");

        // Act
        var questions = await _service.GenerateQuestionsAsync(jobPosting, null);

        // Assert
        Assert.NotNull(questions);
        Assert.True(questions.Count > 0);
    }

    #endregion

    #region AnalyzeAnswerAsync Tests (STAR Technique)

    [Fact]
    public async Task AnalyzeAnswerAsync_ShouldProvideSTARFeedback()
    {
        // Arrange
        var question = "Tell me about a challenging project";
        var answer = "I led a team migration project from monolith to microservices...";

        var starFeedback = @"
            **STAR Analysis**
            
            **Situation**: Good context provided
            **Task**: Clear task description
            **Action**: Actions were well explained
            **Result**: Quantifiable results mentioned
            
            **Overall Score**: 85/100
            **Suggestions**: Could include more metrics
        ";

        _geminiServiceMock.Setup(x => x.GenerateContentAsync(It.IsAny<string>()))
            .ReturnsAsync(starFeedback);

        // Act
        var feedback = await _service.AnalyzeAnswerAsync(question, answer);

        // Assert
        Assert.NotNull(feedback);
        Assert.Contains("STAR", feedback);
    }

    [Fact]
    public async Task AnalyzeAnswerAsync_ShouldCallGeminiWithQuestionAndAnswer()
    {
        // Arrange
        var question = "Tell me about yourself";
        var answer = "I am a developer with 5 years of experience";

        string capturedPrompt = "";
        _geminiServiceMock.Setup(x => x.GenerateContentAsync(It.IsAny<string>()))
            .Callback<string>(prompt => capturedPrompt = prompt)
            .ReturnsAsync("Feedback text");

        // Act
        await _service.AnalyzeAnswerAsync(question, answer);

        // Assert
        Assert.Contains(question, capturedPrompt);
        Assert.Contains(answer, capturedPrompt);
        Assert.Contains("STAR", capturedPrompt);
    }

    #endregion

    #region GenerateImprovementSuggestionsAsync Tests

    [Fact]
    public async Task GenerateImprovementSuggestionsAsync_ShouldProvideActionableSuggestions()
    {
        // Arrange
        var answers = new List<string>
        {
            "I fixed a bug",
            "I worked on a team project",
            "I improved performance"
        };

        var suggestions = @"[
            ""Practice using the STAR method for all behavioral questions"",
            ""Prepare specific metrics and numbers to quantify your achievements"",
            ""Develop 2-3 detailed project stories that showcase different skills""
        ]";

        _geminiServiceMock.Setup(x => x.GenerateContentAsync(It.IsAny<string>()))
            .ReturnsAsync(suggestions);

        // Act
        var result = await _service.GenerateImprovementSuggestionsAsync(answers);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Count > 0);
    }

    [Fact]
    public async Task GenerateImprovementSuggestionsAsync_WithEmptyAnswers_ShouldStillReturn()
    {
        // Arrange
        var answers = new List<string>();

        _geminiServiceMock.Setup(x => x.GenerateContentAsync(It.IsAny<string>()))
            .ReturnsAsync("[\"Practice answering common interview questions\"]");

        // Act
        var result = await _service.GenerateImprovementSuggestionsAsync(answers);

        // Assert
        Assert.NotNull(result);
    }

    #endregion

    #region GetPreparationAsync Tests

    [Fact]
    public async Task GetPreparationAsync_ExistingPrep_ShouldReturnData()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var prep = new InterviewPreparation
        {
            Id = Guid.NewGuid(),
            ApplicationId = applicationId,
            Questions = "[\"Q1\", \"Q2\"]",
            UserAnswers = "[\"A1\", \"A2\"]",
            Feedback = "[\"F1\", \"F2\"]"
        };

        _repositoryMock.Setup(r => r.GetByApplicationIdAsync(applicationId))
            .ReturnsAsync(prep);

        // Act
        var result = await _service.GetPreparationAsync(applicationId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(prep.Id, result.Id);
    }

    [Fact]
    public async Task GetPreparationAsync_NonExistent_ShouldReturnNull()
    {
        // Arrange
        var nonExistentAppId = Guid.NewGuid();
        
        _repositoryMock.Setup(r => r.GetByApplicationIdAsync(nonExistentAppId))
            .ReturnsAsync((InterviewPreparation?)null);

        // Act
        var result = await _service.GetPreparationAsync(nonExistentAppId);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region CreatePreparationAsync Tests

    [Fact]
    public async Task CreatePreparationAsync_ShouldCreateNewPreparation()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var createdPrep = new InterviewPreparation
        {
            Id = Guid.NewGuid(),
            ApplicationId = applicationId,
            CreatedAt = DateTime.UtcNow
        };

        _repositoryMock.Setup(r => r.CreateAsync(It.IsAny<InterviewPreparation>()))
            .ReturnsAsync(createdPrep);

        // Act
        var result = await _service.CreatePreparationAsync(applicationId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(applicationId, result.ApplicationId);
        _repositoryMock.Verify(r => r.CreateAsync(It.Is<InterviewPreparation>(p => p.ApplicationId == applicationId)), Times.Once);
    }

    #endregion
}
