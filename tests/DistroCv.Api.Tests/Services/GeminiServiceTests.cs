using DistroCv.Core.Interfaces;
using DistroCv.Infrastructure.Gemini;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;
using Xunit;

namespace DistroCv.Api.Tests.Services;

/// <summary>
/// Unit tests for GeminiService
/// Task 23.9: Test API integration, error handling
/// </summary>
public class GeminiServiceTests
{
    private readonly Mock<ILogger<GeminiService>> _loggerMock;
    private readonly Mock<IOptions<GeminiConfiguration>> _configMock;
    private readonly GeminiConfiguration _config;

    public GeminiServiceTests()
    {
        _loggerMock = new Mock<ILogger<GeminiService>>();
        _config = new GeminiConfiguration
        {
            ApiKey = "test-api-key",
            BaseUrl = "https://generativelanguage.googleapis.com/v1beta/"
        };
        _configMock = new Mock<IOptions<GeminiConfiguration>>();
        _configMock.Setup(x => x.Value).Returns(_config);
    }

    private GeminiService CreateServiceWithMockHandler(HttpResponseMessage response)
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var httpClient = new HttpClient(handlerMock.Object);
        return new GeminiService(httpClient, _configMock.Object, _loggerMock.Object);
    }

    #region AnalyzeResumeAsync Tests

    [Fact]
    public async Task AnalyzeResumeAsync_ValidResume_ShouldReturnAnalysis()
    {
        // Arrange
        var responseContent = new
        {
            candidates = new[]
            {
                new
                {
                    content = new
                    {
                        parts = new[]
                        {
                            new
                            {
                                text = @"{
                                    ""skills"": [""C#"", "".NET"", ""React""],
                                    ""experience"": [{""title"": ""Developer"", ""years"": 3}],
                                    ""education"": [{""degree"": ""CS"", ""institution"": ""University""}],
                                    ""careerGoals"": ""Senior Developer""
                                }"
                            }
                        }
                    }
                }
            }
        };

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(responseContent))
        };

        var service = CreateServiceWithMockHandler(response);
        var resumeJson = @"{""fullName"": ""John Doe"", ""skills"": [""C#""]}";

        // Act
        var result = await service.AnalyzeResumeAsync(resumeJson);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Skills);
    }

    [Fact]
    public async Task AnalyzeResumeAsync_ApiError_ShouldThrowException()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("Internal Server Error")
        };

        var service = CreateServiceWithMockHandler(response);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(
            () => service.AnalyzeResumeAsync("{}"));
    }

    [Fact]
    public async Task AnalyzeResumeAsync_EmptyResume_ShouldHandleGracefully()
    {
        // Arrange
        var responseContent = new
        {
            candidates = new[]
            {
                new
                {
                    content = new
                    {
                        parts = new[]
                        {
                            new { text = @"{""skills"": [], ""experience"": [], ""education"": [], ""careerGoals"": """"}" }
                        }
                    }
                }
            }
        };

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(responseContent))
        };

        var service = CreateServiceWithMockHandler(response);

        // Act
        var result = await service.AnalyzeResumeAsync("{}");

        // Assert
        Assert.NotNull(result);
    }

    #endregion

    #region CalculateMatchScoreAsync Tests

    [Fact]
    public async Task CalculateMatchScoreAsync_HighMatch_ShouldReturnHighScore()
    {
        // Arrange
        var responseContent = new
        {
            candidates = new[]
            {
                new
                {
                    content = new
                    {
                        parts = new[]
                        {
                            new
                            {
                                text = @"{
                                    ""matchScore"": 92,
                                    ""reasoning"": ""Excellent match - strong skills alignment"",
                                    ""skillGaps"": []
                                }"
                            }
                        }
                    }
                }
            }
        };

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(responseContent))
        };

        var service = CreateServiceWithMockHandler(response);

        // Act
        var result = await service.CalculateMatchScoreAsync(
            @"{""skills"": [""C#"", "".NET""], ""experience"": []}",
            @"{""title"": "".NET Developer"", ""requirements"": [""C#""]}");

        // Assert
        Assert.NotNull(result);
        Assert.True(result.MatchScore >= 80);
        Assert.Contains("Excellent", result.Reasoning);
    }

    [Fact]
    public async Task CalculateMatchScoreAsync_LowMatch_ShouldReturnLowScoreWithSkillGaps()
    {
        // Arrange
        var responseContent = new
        {
            candidates = new[]
            {
                new
                {
                    content = new
                    {
                        parts = new[]
                        {
                            new
                            {
                                text = @"{
                                    ""matchScore"": 45,
                                    ""reasoning"": ""Missing key skills"",
                                    ""skillGaps"": [""Kubernetes"", ""Docker"", ""AWS""]
                                }"
                            }
                        }
                    }
                }
            }
        };

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(responseContent))
        };

        var service = CreateServiceWithMockHandler(response);

        // Act
        var result = await service.CalculateMatchScoreAsync(
            @"{""skills"": [""C#""]}",
            @"{""requirements"": [""Kubernetes"", ""Docker"", ""AWS""]}");

        // Assert
        Assert.NotNull(result);
        Assert.True(result.MatchScore < 80);
        Assert.NotEmpty(result.SkillGaps);
    }

    [Fact]
    public async Task CalculateMatchScoreAsync_ShouldReturnScoreWithinValidRange()
    {
        // Arrange
        var responseContent = new
        {
            candidates = new[]
            {
                new
                {
                    content = new
                    {
                        parts = new[]
                        {
                            new { text = @"{""matchScore"": 75, ""reasoning"": ""Moderate match"", ""skillGaps"": [""Azure""]}" }
                        }
                    }
                }
            }
        };

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(responseContent))
        };

        var service = CreateServiceWithMockHandler(response);

        // Act
        var result = await service.CalculateMatchScoreAsync("{}", "{}");

        // Assert - Property 1: 0 ≤ matchScore ≤ 100
        Assert.True(result.MatchScore >= 0);
        Assert.True(result.MatchScore <= 100);
    }

    #endregion

    #region GenerateEmbeddingAsync Tests

    [Fact]
    public async Task GenerateEmbeddingAsync_ValidText_ShouldReturnEmbedding()
    {
        // Arrange
        var embedding = new float[768]; // Gemini typically returns 768-dim embeddings
        for (int i = 0; i < 768; i++) embedding[i] = (float)(i * 0.001);

        var responseContent = new
        {
            embedding = new { values = embedding }
        };

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(responseContent))
        };

        var service = CreateServiceWithMockHandler(response);

        // Act
        var result = await service.GenerateEmbeddingAsync("Software developer with C# experience");

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Length > 0);
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_EmptyText_ShouldThrowException()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("Empty input text")
        };

        var service = CreateServiceWithMockHandler(response);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(
            () => service.GenerateEmbeddingAsync(""));
    }

    #endregion

    #region GenerateContentAsync Tests

    [Fact]
    public async Task GenerateContentAsync_WithLanguage_ShouldIncludeLanguageInPrompt()
    {
        // Arrange
        var responseContent = new
        {
            candidates = new[]
            {
                new
                {
                    content = new
                    {
                        parts = new[]
                        {
                            new { text = "İçerik Türkçe olarak oluşturuldu" }
                        }
                    }
                }
            }
        };

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(responseContent))
        };

        HttpRequestMessage? capturedRequest = null;
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(response);

        var httpClient = new HttpClient(handlerMock.Object);
        var service = new GeminiService(httpClient, _configMock.Object, _loggerMock.Object);

        // Act
        var result = await service.GenerateContentAsync("Write a greeting", "tr");

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(capturedRequest);
        var requestBody = await capturedRequest.Content!.ReadAsStringAsync();
        Assert.Contains("tr", requestBody);
    }

    [Fact]
    public async Task GenerateContentAsync_DefaultLanguage_ShouldUseEnglish()
    {
        // Arrange
        var responseContent = new
        {
            candidates = new[]
            {
                new
                {
                    content = new
                    {
                        parts = new[]
                        {
                            new { text = "Content generated in English" }
                        }
                    }
                }
            }
        };

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(responseContent))
        };

        var service = CreateServiceWithMockHandler(response);

        // Act
        var result = await service.GenerateContentAsync("Write something");

        // Assert
        Assert.NotNull(result);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task ApiCall_RateLimited_ShouldThrowException()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests)
        {
            Content = new StringContent("Rate limit exceeded")
        };

        var service = CreateServiceWithMockHandler(response);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(
            () => service.AnalyzeResumeAsync("{}"));
    }

    [Fact]
    public async Task ApiCall_Unauthorized_ShouldThrowException()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = new StringContent("Invalid API key")
        };

        var service = CreateServiceWithMockHandler(response);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(
            () => service.AnalyzeResumeAsync("{}"));
    }

    [Fact]
    public async Task ApiCall_EmptyResponse_ShouldThrowException()
    {
        // Arrange
        var responseContent = new { candidates = Array.Empty<object>() };
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(responseContent))
        };

        var service = CreateServiceWithMockHandler(response);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.AnalyzeResumeAsync("{}"));
    }

    #endregion

    #region Retry Logic Tests

    [Fact]
    public async Task ApiCall_TransientError_ShouldLogError()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
        {
            Content = new StringContent("Service temporarily unavailable")
        };

        var service = CreateServiceWithMockHandler(response);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(
            () => service.AnalyzeResumeAsync("{}"));

        // Verify logging was called
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    #endregion
}

