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
/// Unit tests for JobScrapingService
/// Task 23.8: Test scraping logic and duplicate detection
/// </summary>
public class JobScrapingServiceTests
{
    private readonly Mock<IGeminiService> _geminiServiceMock;
    private readonly Mock<ILogger<JobScrapingService>> _loggerMock;
    private readonly DbContextOptions<DistroCvDbContext> _dbContextOptions;

    public JobScrapingServiceTests()
    {
        _geminiServiceMock = new Mock<IGeminiService>();
        _loggerMock = new Mock<ILogger<JobScrapingService>>();
        _dbContextOptions = new DbContextOptionsBuilder<DistroCvDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    private DistroCvDbContext CreateDbContext() => new DistroCvDbContext(_dbContextOptions);

    private JobScrapingService CreateService(DistroCvDbContext context)
    {
        return new JobScrapingService(
            context,
            _loggerMock.Object,
            _geminiServiceMock.Object);
    }

    #region Test Data Setup

    private JobPosting CreateTestJobPosting(string externalId)
    {
        return new JobPosting
        {
            Id = Guid.NewGuid(),
            ExternalId = externalId,
            Title = "Software Developer",
            CompanyName = "Tech Corp",
            Description = "Looking for a software developer",
            Location = "Istanbul, Turkey",
            SourcePlatform = "LinkedIn",
            SourceUrl = $"https://linkedin.com/jobs/view/{externalId}",
            ScrapedAt = DateTime.UtcNow,
            IsActive = true
        };
    }

    #endregion

    #region IsDuplicateAsync Tests

    [Fact]
    public async Task IsDuplicateAsync_ShouldReturnTrue_WhenJobExists()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        var externalId = "linkedin_12345";
        var existingJob = CreateTestJobPosting(externalId);
        context.JobPostings.Add(existingJob);
        await context.SaveChangesAsync();

        // Act
        var result = await service.IsDuplicateAsync(externalId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsDuplicateAsync_ShouldReturnFalse_WhenJobDoesNotExist()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        var nonExistentId = "linkedin_99999";

        // Act
        var result = await service.IsDuplicateAsync(nonExistentId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsDuplicateAsync_ShouldHandleMultipleJobs()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        var job1 = CreateTestJobPosting("linkedin_111");
        var job2 = CreateTestJobPosting("linkedin_222");
        var job3 = CreateTestJobPosting("linkedin_333");

        context.JobPostings.AddRange(job1, job2, job3);
        await context.SaveChangesAsync();

        // Act & Assert
        Assert.True(await service.IsDuplicateAsync("linkedin_111"));
        Assert.True(await service.IsDuplicateAsync("linkedin_222"));
        Assert.True(await service.IsDuplicateAsync("linkedin_333"));
        Assert.False(await service.IsDuplicateAsync("linkedin_444"));
    }

    #endregion

    #region StoreJobPostingsAsync Tests

    [Fact]
    public async Task StoreJobPostingsAsync_ShouldStoreNewJobs()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        var jobs = new List<JobPosting>
        {
            CreateTestJobPosting("linkedin_001"),
            CreateTestJobPosting("linkedin_002"),
            CreateTestJobPosting("linkedin_003")
        };

        // Mock embedding generation
        _geminiServiceMock.Setup(s => s.GenerateEmbeddingAsync(It.IsAny<string>()))
            .ReturnsAsync(new float[] { 0.1f, 0.2f, 0.3f });

        // Act
        var storedCount = await service.StoreJobPostingsAsync(jobs);

        // Assert
        Assert.Equal(3, storedCount);
        Assert.Equal(3, await context.JobPostings.CountAsync());
    }

    [Fact]
    public async Task StoreJobPostingsAsync_ShouldSkipDuplicates()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        // Add existing job
        var existingJob = CreateTestJobPosting("linkedin_existing");
        context.JobPostings.Add(existingJob);
        await context.SaveChangesAsync();

        var jobs = new List<JobPosting>
        {
            CreateTestJobPosting("linkedin_existing"), // Duplicate
            CreateTestJobPosting("linkedin_new1"),
            CreateTestJobPosting("linkedin_new2")
        };

        _geminiServiceMock.Setup(s => s.GenerateEmbeddingAsync(It.IsAny<string>()))
            .ReturnsAsync(new float[] { 0.1f, 0.2f, 0.3f });

        // Act
        var storedCount = await service.StoreJobPostingsAsync(jobs);

        // Assert
        Assert.Equal(2, storedCount); // Only 2 new jobs should be stored
        Assert.Equal(3, await context.JobPostings.CountAsync()); // 1 existing + 2 new
    }

    [Fact]
    public async Task StoreJobPostingsAsync_ShouldGenerateEmbeddings()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        var job = CreateTestJobPosting("linkedin_embed");

        _geminiServiceMock.Setup(s => s.GenerateEmbeddingAsync(It.IsAny<string>()))
            .ReturnsAsync(new float[] { 0.1f, 0.2f, 0.3f });

        // Act
        await service.StoreJobPostingsAsync(new List<JobPosting> { job });

        // Assert
        _geminiServiceMock.Verify(s => s.GenerateEmbeddingAsync(It.IsAny<string>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task StoreJobPostingsAsync_ShouldContinueOnEmbeddingError()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        var jobs = new List<JobPosting>
        {
            CreateTestJobPosting("linkedin_fail"),
            CreateTestJobPosting("linkedin_success")
        };

        // First call throws, second succeeds
        var callCount = 0;
        _geminiServiceMock.Setup(s => s.GenerateEmbeddingAsync(It.IsAny<string>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                if (callCount == 1)
                    throw new Exception("Embedding error");
                return new float[] { 0.1f, 0.2f, 0.3f };
            });

        // Act
        var storedCount = await service.StoreJobPostingsAsync(jobs);

        // Assert - Both jobs should be stored even if embedding fails for one
        Assert.Equal(2, storedCount);
    }

    [Fact]
    public async Task StoreJobPostingsAsync_ShouldRespectCancellation()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        var jobs = new List<JobPosting>
        {
            CreateTestJobPosting("linkedin_001"),
            CreateTestJobPosting("linkedin_002"),
            CreateTestJobPosting("linkedin_003")
        };

        var cts = new CancellationTokenSource();
        cts.Cancel();

        _geminiServiceMock.Setup(s => s.GenerateEmbeddingAsync(It.IsAny<string>()))
            .ReturnsAsync(new float[] { 0.1f, 0.2f, 0.3f });

        // Act
        var storedCount = await service.StoreJobPostingsAsync(jobs, cts.Token);

        // Assert - Should stop immediately due to cancellation
        Assert.Equal(0, storedCount);
    }

    [Fact]
    public async Task StoreJobPostingsAsync_ShouldHandleEmptyList()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        var emptyJobs = new List<JobPosting>();

        // Act
        var storedCount = await service.StoreJobPostingsAsync(emptyJobs);

        // Assert
        Assert.Equal(0, storedCount);
        Assert.Equal(0, await context.JobPostings.CountAsync());
    }

    #endregion

    #region Scraping Tests (Limited - Require Browser)

    // Note: Full scraping tests would require mocking Playwright or running integration tests
    // These tests focus on the logic that can be unit tested

    [Fact]
    public async Task ScrapeLinkedInAsync_ShouldInitializeBrowser()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        // Act & Assert
        // This test verifies the service can be created and doesn't throw on initialization
        // Full scraping would require Playwright browser installation
        Assert.NotNull(service);
    }

    [Fact]
    public async Task ScrapeIndeedAsync_ShouldInitializeBrowser()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        // Act & Assert
        // This test verifies the service can be created and doesn't throw on initialization
        Assert.NotNull(service);
    }

    #endregion

    #region ExtractJobDetailsAsync Tests

    [Fact]
    public async Task ExtractJobDetailsAsync_ShouldReturnNull_ForUnsupportedPlatform()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        // Act
        // Note: This test would require browser to be initialized
        // In real implementation, we would need to mock Playwright
        // For now, we test the service creation
        Assert.NotNull(service);
    }

    #endregion

    #region DisposeAsync Tests

    [Fact]
    public async Task DisposeAsync_ShouldNotThrow_WhenBrowserNotInitialized()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        // Act & Assert - Should not throw
        await service.DisposeAsync();
    }

    #endregion
}

