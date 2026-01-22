using DistroCv.Core.DTOs;
using DistroCv.Core.Entities;
using DistroCv.Core.Interfaces;
using DistroCv.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DistroCv.Api.Tests.Services;

public class SessionServiceTests
{
    private readonly Mock<ISessionRepository> _mockRepository;
    private readonly Mock<ILogger<SessionService>> _mockLogger;
    private readonly SessionService _service;

    public SessionServiceTests()
    {
        _mockRepository = new Mock<ISessionRepository>();
        _mockLogger = new Mock<ILogger<SessionService>>();
        _service = new SessionService(_mockRepository.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task CreateSessionAsync_ShouldCreateSession_WhenUnderLimit()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var dto = new CreateSessionDto(
            userId,
            "access-token",
            "refresh-token",
            3600,
            "Desktop",
            "192.168.1.1",
            "Mozilla/5.0"
        );

        _mockRepository.Setup(r => r.CountActiveSessionsAsync(userId))
            .ReturnsAsync(2);

        _mockRepository.Setup(r => r.CreateAsync(It.IsAny<UserSession>()))
            .ReturnsAsync((UserSession s) => s);

        // Act
        var result = await _service.CreateSessionAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.UserId);
        Assert.Equal("access-token", result.AccessToken);
        Assert.Equal("refresh-token", result.RefreshToken);
        Assert.True(result.IsActive);
        _mockRepository.Verify(r => r.CreateAsync(It.IsAny<UserSession>()), Times.Once);
    }

    [Fact]
    public async Task CreateSessionAsync_ShouldRevokeOldestSession_WhenAtLimit()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var dto = new CreateSessionDto(
            userId,
            "access-token",
            "refresh-token",
            3600,
            "Desktop",
            "192.168.1.1",
            "Mozilla/5.0"
        );

        var oldestSession = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CreatedAt = DateTime.UtcNow.AddDays(-5),
            LastActivityAt = DateTime.UtcNow.AddDays(-5)
        };

        _mockRepository.Setup(r => r.CountActiveSessionsAsync(userId))
            .ReturnsAsync(5); // At max limit

        _mockRepository.Setup(r => r.GetActiveSessionsByUserIdAsync(userId))
            .ReturnsAsync(new List<UserSession> { oldestSession });

        _mockRepository.Setup(r => r.CreateAsync(It.IsAny<UserSession>()))
            .ReturnsAsync((UserSession s) => s);

        // Act
        var result = await _service.CreateSessionAsync(dto);

        // Assert
        Assert.NotNull(result);
        _mockRepository.Verify(r => r.RevokeSessionAsync(oldestSession.Id, It.IsAny<string>()), Times.Once);
        _mockRepository.Verify(r => r.CreateAsync(It.IsAny<UserSession>()), Times.Once);
    }

    [Fact]
    public async Task GetActiveSessionsAsync_ShouldReturnActiveSessions()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var sessions = new List<UserSession>
        {
            new UserSession
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                DeviceInfo = "Desktop",
                IpAddress = "192.168.1.1",
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                IsActive = true
            },
            new UserSession
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                DeviceInfo = "Mobile",
                IpAddress = "192.168.1.2",
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                IsActive = true
            }
        };

        _mockRepository.Setup(r => r.GetActiveSessionsByUserIdAsync(userId))
            .ReturnsAsync(sessions);

        // Act
        var result = await _service.GetActiveSessionsAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.All(result, s => Assert.True(s.IsActive));
    }

    [Fact]
    public async Task RevokeSessionAsync_ShouldReturnFalse_WhenSessionNotFound()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _mockRepository.Setup(r => r.GetByIdAsync(sessionId))
            .ReturnsAsync((UserSession?)null);

        // Act
        var result = await _service.RevokeSessionAsync(sessionId, userId);

        // Assert
        Assert.False(result);
        _mockRepository.Verify(r => r.RevokeSessionAsync(It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RevokeSessionAsync_ShouldReturnFalse_WhenUserIdMismatch()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var differentUserId = Guid.NewGuid();

        var session = new UserSession
        {
            Id = sessionId,
            UserId = differentUserId,
            IsActive = true
        };

        _mockRepository.Setup(r => r.GetByIdAsync(sessionId))
            .ReturnsAsync(session);

        // Act
        var result = await _service.RevokeSessionAsync(sessionId, userId);

        // Assert
        Assert.False(result);
        _mockRepository.Verify(r => r.RevokeSessionAsync(It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RevokeSessionAsync_ShouldReturnTrue_WhenSuccessful()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var session = new UserSession
        {
            Id = sessionId,
            UserId = userId,
            IsActive = true
        };

        _mockRepository.Setup(r => r.GetByIdAsync(sessionId))
            .ReturnsAsync(session);

        // Act
        var result = await _service.RevokeSessionAsync(sessionId, userId);

        // Assert
        Assert.True(result);
        _mockRepository.Verify(r => r.RevokeSessionAsync(sessionId, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ValidateSessionAsync_ShouldReturnFalse_WhenSessionNotFound()
    {
        // Arrange
        var accessToken = "invalid-token";

        _mockRepository.Setup(r => r.GetByAccessTokenAsync(accessToken))
            .ReturnsAsync((UserSession?)null);

        // Act
        var result = await _service.ValidateSessionAsync(accessToken);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateSessionAsync_ShouldReturnFalse_WhenSessionExpired()
    {
        // Arrange
        var accessToken = "expired-token";
        var session = new UserSession
        {
            Id = Guid.NewGuid(),
            AccessToken = accessToken,
            IsActive = true,
            ExpiresAt = DateTime.UtcNow.AddHours(-1) // Expired
        };

        _mockRepository.Setup(r => r.GetByAccessTokenAsync(accessToken))
            .ReturnsAsync(session);

        // Act
        var result = await _service.ValidateSessionAsync(accessToken);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateSessionAsync_ShouldReturnTrue_WhenSessionValid()
    {
        // Arrange
        var accessToken = "valid-token";
        var session = new UserSession
        {
            Id = Guid.NewGuid(),
            AccessToken = accessToken,
            IsActive = true,
            ExpiresAt = DateTime.UtcNow.AddHours(1) // Not expired
        };

        _mockRepository.Setup(r => r.GetByAccessTokenAsync(accessToken))
            .ReturnsAsync(session);

        // Act
        var result = await _service.ValidateSessionAsync(accessToken);

        // Assert
        Assert.True(result);
        _mockRepository.Verify(r => r.UpdateActivityAsync(session.Id), Times.Once);
    }

    [Fact]
    public async Task RevokeAllSessionsAsync_ShouldRevokeAllUserSessions()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        await _service.RevokeAllSessionsAsync(userId);

        // Assert
        _mockRepository.Verify(r => r.RevokeAllUserSessionsAsync(userId, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task CleanupExpiredSessionsAsync_ShouldCallRepository()
    {
        // Act
        await _service.CleanupExpiredSessionsAsync();

        // Assert
        _mockRepository.Verify(r => r.DeleteExpiredSessionsAsync(), Times.Once);
    }

    [Fact]
    public void ToDto_ShouldConvertSessionToDto()
    {
        // Arrange
        var session = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            DeviceInfo = "Desktop",
            IpAddress = "192.168.1.1",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            LastActivityAt = DateTime.UtcNow,
            IsActive = true
        };

        // Act
        var dto = _service.ToDto(session);

        // Assert
        Assert.NotNull(dto);
        Assert.Equal(session.Id, dto.Id);
        Assert.Equal(session.UserId, dto.UserId);
        Assert.Equal(session.DeviceInfo, dto.DeviceInfo);
        Assert.Equal(session.IpAddress, dto.IpAddress);
        Assert.Equal(session.IsActive, dto.IsActive);
    }
}
