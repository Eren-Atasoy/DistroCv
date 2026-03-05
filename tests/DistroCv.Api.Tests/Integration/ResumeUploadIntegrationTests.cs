using DistroCv.Core.Entities;
using DistroCv.Core.Interfaces;
using DistroCv.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Xunit;

namespace DistroCv.Api.Tests.Integration;

/// <summary>
/// Integration tests for resume upload and parsing flow (Task 24.2)
/// Tests the end-to-end flow from file upload to digital twin creation
/// </summary>
public class ResumeUploadIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public ResumeUploadIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        SetupMocks();
    }

    private void SetupMocks()
    {
        // Setup Gemini service mock for resume parsing
        _factory.GeminiServiceMock
            .Setup(x => x.GenerateContentAsync(It.IsAny<string>()))
            .ReturnsAsync(@"{
                ""name"": ""John Doe"",
                ""email"": ""john@example.com"",
                ""phone"": ""+90 555 123 4567"",
                ""skills"": [""C#"", "".NET Core"", ""Azure"", ""SQL Server""],
                ""experience"": [{""title"": ""Software Developer"", ""company"": ""Tech Corp"", ""years"": 5}],
                ""education"": [{""degree"": ""BSc Computer Science"", ""school"": ""MIT""}]
            }");

        // Setup embedding generation mock
        _factory.GeminiServiceMock
            .Setup(x => x.GenerateEmbeddingAsync(It.IsAny<string>()))
            .ReturnsAsync(new float[] { 0.1f, 0.2f, 0.3f, 0.4f, 0.5f });

        // Setup S3 mock for file upload
        _factory.S3ServiceMock
            .Setup(x => x.UploadFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("s3://bucket/resumes/test-resume.pdf");
    }

    [Fact]
    public async Task ProfileService_ShouldCreateDigitalTwin_WhenResumeUploaded()
    {
        // Arrange
        var user = await _factory.CreateTestUserAsync();

        using var scope = _factory.Services.CreateScope();
        var profileService = scope.ServiceProvider.GetRequiredService<IProfileService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<DistroCvDbContext>();

        // Create a simple text resume for testing
        var resumeContent = @"
John Doe
Software Developer
Email: john@example.com
Phone: +90 555 123 4567

SKILLS
- C#, .NET Core, Azure
- SQL Server, Entity Framework
- React, TypeScript

EXPERIENCE
Software Developer at Tech Corp (2019-2024)
- Developed backend services using .NET Core
- Implemented Azure cloud solutions

EDUCATION
BSc Computer Science - MIT (2015-2019)
";
        var resumeBytes = Encoding.UTF8.GetBytes(resumeContent);
        using var resumeStream = new MemoryStream(resumeBytes);

        // Act
        var digitalTwin = await profileService.CreateDigitalTwinAsync(user.Id, resumeStream, "resume.txt");

        // Assert
        Assert.NotNull(digitalTwin);
        Assert.Equal(user.Id, digitalTwin.UserId);
        Assert.NotNull(digitalTwin.Skills);
        Assert.NotNull(digitalTwin.Experience);
        Assert.NotNull(digitalTwin.Education);

        // Verify digital twin was saved to database
        var savedTwin = await dbContext.DigitalTwins.FirstOrDefaultAsync(dt => dt.UserId == user.Id);
        Assert.NotNull(savedTwin);
    }

    [Fact]
    public async Task ProfileService_ShouldUpdateDigitalTwin_WhenNewResumeUploaded()
    {
        // Arrange
        var user = await _factory.CreateTestUserAsync("update@example.com", "Update User");
        var existingTwin = await _factory.CreateDigitalTwinAsync(user.Id);

        using var scope = _factory.Services.CreateScope();
        var profileService = scope.ServiceProvider.GetRequiredService<IProfileService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<DistroCvDbContext>();

        var newResumeContent = "Updated resume content with new skills";
        var resumeBytes = Encoding.UTF8.GetBytes(newResumeContent);
        using var resumeStream = new MemoryStream(resumeBytes);

        // Act
        var updatedTwin = await profileService.CreateDigitalTwinAsync(user.Id, resumeStream, "updated-resume.txt");

        // Assert
        Assert.NotNull(updatedTwin);
        Assert.Equal(user.Id, updatedTwin.UserId);

        // Verify only one digital twin exists for the user
        var twinsCount = await dbContext.DigitalTwins.CountAsync(dt => dt.UserId == user.Id);
        Assert.Equal(1, twinsCount);
    }

    [Fact]
    public async Task ProfileService_ShouldGenerateEmbedding_WhenResumeProcessed()
    {
        // Arrange
        var user = await _factory.CreateTestUserAsync("embed@example.com", "Embed User");

        using var scope = _factory.Services.CreateScope();
        var profileService = scope.ServiceProvider.GetRequiredService<IProfileService>();

        var resumeContent = "Software Developer with C# and .NET experience";
        var resumeBytes = Encoding.UTF8.GetBytes(resumeContent);
        using var resumeStream = new MemoryStream(resumeBytes);

        // Act
        var digitalTwin = await profileService.CreateDigitalTwinAsync(user.Id, resumeStream, "resume.txt");

        // Assert
        Assert.NotNull(digitalTwin);
        
        // Verify embedding was generated
        _factory.GeminiServiceMock.Verify(
            x => x.GenerateEmbeddingAsync(It.IsAny<string>()), 
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task ProfileService_ShouldUploadToS3_WhenResumeProcessed()
    {
        // Arrange
        var user = await _factory.CreateTestUserAsync("s3@example.com", "S3 User");

        using var scope = _factory.Services.CreateScope();
        var profileService = scope.ServiceProvider.GetRequiredService<IProfileService>();

        var resumeContent = "Resume content for S3 upload test";
        var resumeBytes = Encoding.UTF8.GetBytes(resumeContent);
        using var resumeStream = new MemoryStream(resumeBytes);

        // Act
        var digitalTwin = await profileService.CreateDigitalTwinAsync(user.Id, resumeStream, "resume.txt");

        // Assert
        Assert.NotNull(digitalTwin);
        Assert.NotNull(digitalTwin.OriginalResumeUrl);

        // Verify S3 upload was called
        _factory.S3ServiceMock.Verify(
            x => x.UploadFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()), 
            Times.Once);
    }

    [Fact]
    public async Task ProfileService_GetDigitalTwin_ShouldReturnExistingTwin()
    {
        // Arrange
        var user = await _factory.CreateTestUserAsync("get@example.com", "Get User");
        var existingTwin = await _factory.CreateDigitalTwinAsync(user.Id);

        using var scope = _factory.Services.CreateScope();
        var profileService = scope.ServiceProvider.GetRequiredService<IProfileService>();

        // Act
        var retrievedTwin = await profileService.GetDigitalTwinAsync(user.Id);

        // Assert
        Assert.NotNull(retrievedTwin);
        Assert.Equal(existingTwin.Id, retrievedTwin.Id);
        Assert.Equal(user.Id, retrievedTwin.UserId);
    }

    [Fact]
    public async Task ProfileService_GetDigitalTwin_ShouldReturnNull_WhenNotExists()
    {
        // Arrange
        var nonExistentUserId = Guid.NewGuid();

        using var scope = _factory.Services.CreateScope();
        var profileService = scope.ServiceProvider.GetRequiredService<IProfileService>();

        // Act
        var result = await profileService.GetDigitalTwinAsync(nonExistentUserId);

        // Assert
        Assert.Null(result);
    }
}

