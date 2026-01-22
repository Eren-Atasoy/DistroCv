using DistroCv.Core.Entities;
using DistroCv.Infrastructure.AWS;
using DistroCv.Infrastructure.Data;
using DistroCv.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DistroCv.Api.Tests.Services;

public class ProfileServiceTests
{
    private readonly Mock<IS3Service> _mockS3Service;
    private readonly Mock<ILogger<ProfileService>> _mockLogger;
    private readonly DistroCvDbContext _context;
    private readonly ProfileService _profileService;

    public ProfileServiceTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<DistroCvDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new DistroCvDbContext(options);

        _mockS3Service = new Mock<IS3Service>();
        _mockLogger = new Mock<ILogger<ProfileService>>();

        _profileService = new ProfileService(_context, _mockS3Service.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task CreateDigitalTwinAsync_WithValidUser_CreatesDigitalTwin()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            FullName = "Test User",
            CognitoUserId = "cognito-123",
            PreferredLanguage = "en",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var fileName = "resume.txt";
        var fileContent = "Test resume content";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(fileContent));

        _mockS3Service
            .Setup(s => s.UploadFileAsync(It.IsAny<Stream>(), fileName, "text/plain"))
            .ReturnsAsync("test-s3-key/resume.txt");

        // Act
        var result = await _profileService.CreateDigitalTwinAsync(userId, stream, fileName);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.UserId);
        Assert.NotNull(result.OriginalResumeUrl);
        Assert.Contains("s3://", result.OriginalResumeUrl);
        Assert.NotNull(result.ParsedResumeJson);
        Assert.NotNull(result.EmbeddingVector);

        // Verify S3 upload was called
        _mockS3Service.Verify(s => s.UploadFileAsync(
            It.IsAny<Stream>(), 
            fileName, 
            "text/plain"), Times.Once);

        // Verify digital twin was saved to database
        var savedTwin = await _context.DigitalTwins.FirstOrDefaultAsync(dt => dt.UserId == userId);
        Assert.NotNull(savedTwin);
        Assert.Equal(result.Id, savedTwin.Id);
    }

    [Fact]
    public async Task CreateDigitalTwinAsync_WithNonExistentUser_ThrowsArgumentException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var fileName = "resume.txt";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("Test content"));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _profileService.CreateDigitalTwinAsync(userId, stream, fileName));
    }

    [Fact]
    public async Task CreateDigitalTwinAsync_WithExistingDigitalTwin_UpdatesExisting()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            FullName = "Test User",
            CognitoUserId = "cognito-123",
            PreferredLanguage = "en",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
        _context.Users.Add(user);

        var existingTwin = new DigitalTwin
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            OriginalResumeUrl = "s3://old-key/old-resume.txt",
            ParsedResumeJson = "{}",
            EmbeddingVector = new Pgvector.Vector(new float[768]),
            Skills = "[]",
            Experience = "[]",
            Education = "[]",
            CareerGoals = "",
            Preferences = "{}",
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };
        _context.DigitalTwins.Add(existingTwin);
        await _context.SaveChangesAsync();

        var fileName = "new-resume.txt";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("New resume content"));

        _mockS3Service
            .Setup(s => s.UploadFileAsync(It.IsAny<Stream>(), fileName, "text/plain"))
            .ReturnsAsync("test-s3-key/new-resume.txt");

        // Act
        var result = await _profileService.CreateDigitalTwinAsync(userId, stream, fileName);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(existingTwin.Id, result.Id); // Should be same ID (updated, not new)
        Assert.Equal(userId, result.UserId);
        Assert.Contains("new-resume.txt", result.OriginalResumeUrl);

        // Verify only one digital twin exists for the user
        var twins = await _context.DigitalTwins.Where(dt => dt.UserId == userId).ToListAsync();
        Assert.Single(twins);
    }

    [Fact]
    public async Task GetDigitalTwinAsync_WithExistingTwin_ReturnsTwin()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var digitalTwin = new DigitalTwin
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            OriginalResumeUrl = "s3://test-key/resume.txt",
            ParsedResumeJson = "{}",
            EmbeddingVector = new Pgvector.Vector(new float[768]),
            Skills = "[]",
            Experience = "[]",
            Education = "[]",
            CareerGoals = "",
            Preferences = "{}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.DigitalTwins.Add(digitalTwin);
        await _context.SaveChangesAsync();

        // Act
        var result = await _profileService.GetDigitalTwinAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(digitalTwin.Id, result.Id);
        Assert.Equal(userId, result.UserId);
    }

    [Fact]
    public async Task GetDigitalTwinAsync_WithNonExistentTwin_ReturnsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var result = await _profileService.GetDigitalTwinAsync(userId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateDigitalTwinAsync_WithExistingTwin_UpdatesPreferences()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var digitalTwin = new DigitalTwin
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            OriginalResumeUrl = "s3://test-key/resume.txt",
            ParsedResumeJson = "{}",
            EmbeddingVector = new Pgvector.Vector(new float[768]),
            Skills = "[]",
            Experience = "[]",
            Education = "[]",
            CareerGoals = "",
            Preferences = "{}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.DigitalTwins.Add(digitalTwin);
        await _context.SaveChangesAsync();

        var newPreferences = "{\"sectors\":[\"Technology\"],\"locations\":[\"Istanbul\"]}";

        // Act
        var result = await _profileService.UpdateDigitalTwinAsync(userId, newPreferences);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(newPreferences, result.Preferences);
        Assert.True(result.UpdatedAt > digitalTwin.UpdatedAt);
    }

    [Fact]
    public async Task UpdateDigitalTwinAsync_WithNonExistentTwin_ThrowsInvalidOperationException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var preferences = "{}";

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _profileService.UpdateDigitalTwinAsync(userId, preferences));
    }

    [Theory]
    [InlineData(".pdf", "application/pdf")]
    [InlineData(".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
    [InlineData(".txt", "text/plain")]
    public async Task CreateDigitalTwinAsync_WithDifferentFileTypes_UsesCorrectContentType(
        string extension, string expectedContentType)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            FullName = "Test User",
            CognitoUserId = "cognito-123",
            PreferredLanguage = "en",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var fileName = $"resume{extension}";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("Test content"));

        _mockS3Service
            .Setup(s => s.UploadFileAsync(It.IsAny<Stream>(), fileName, expectedContentType))
            .ReturnsAsync($"test-s3-key/{fileName}");

        // Act
        var result = await _profileService.CreateDigitalTwinAsync(userId, stream, fileName);

        // Assert
        Assert.NotNull(result);
        _mockS3Service.Verify(s => s.UploadFileAsync(
            It.IsAny<Stream>(), 
            fileName, 
            expectedContentType), Times.Once);
    }

    [Fact]
    public async Task ParseResumeAsync_WithTxtFile_ReturnsJsonContent()
    {
        // Arrange
        var content = "John Doe\nSoftware Engineer\nExperience: 5 years";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
        var fileName = "resume.txt";

        // Act
        var result = await _profileService.ParseResumeAsync(stream, fileName);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("txt", result);
        Assert.Contains(content, result);
    }

    [Fact]
    public async Task ParseResumeAsync_WithUnsupportedFileType_ThrowsNotSupportedException()
    {
        // Arrange
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("Test content"));
        var fileName = "resume.xyz";

        // Act & Assert
        await Assert.ThrowsAsync<NotSupportedException>(
            () => _profileService.ParseResumeAsync(stream, fileName));
    }

    [Fact]
    public void GenerateEmbeddingAsync_WithText_ReturnsVector()
    {
        // Arrange
        var text = "This is a test resume content";

        // Act
        var result = _profileService.GenerateEmbeddingAsync(text).Result;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(768, result.ToArray().Length); // Standard embedding dimension
    }
}
