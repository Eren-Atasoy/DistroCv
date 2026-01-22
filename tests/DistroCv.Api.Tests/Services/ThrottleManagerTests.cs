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
/// Unit tests for ThrottleManager
/// Task 23.5: Test quota limits, delay generation
/// </summary>
public class ThrottleManagerTests : IDisposable
{
    private readonly DistroCvDbContext _context;
    private readonly Mock<ILogger<ThrottleManager>> _loggerMock;
    private readonly ThrottleManager _throttleManager;
    private readonly Guid _testUserId;

    public ThrottleManagerTests()
    {
        var options = new DbContextOptionsBuilder<DistroCvDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new DistroCvDbContext(options);
        _loggerMock = new Mock<ILogger<ThrottleManager>>();
        _throttleManager = new ThrottleManager(_context, _loggerMock.Object);
        _testUserId = Guid.NewGuid();

        // Create test user
        _context.Users.Add(new User 
        { 
            Id = _testUserId, 
            Email = "test@example.com",
            FullName = "Test User"
        });
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    #region CanSendConnectionRequestAsync Tests (Max 20/day)

    [Fact]
    public async Task CanSendConnectionRequestAsync_FirstRequest_ShouldReturnTrue()
    {
        // Act
        var result = await _throttleManager.CanSendConnectionRequestAsync(_testUserId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CanSendConnectionRequestAsync_AtLimit_ShouldReturnFalse()
    {
        // Arrange - Add 20 connections today
        var today = DateTime.UtcNow.Date;
        for (int i = 0; i < 20; i++)
        {
            _context.ThrottleLogs.Add(new ThrottleLog
            {
                Id = Guid.NewGuid(),
                UserId = _testUserId,
                ActionType = "ConnectionRequest",
                Platform = "LinkedIn",
                Timestamp = today.AddMinutes(i)
            });
        }
        await _context.SaveChangesAsync();

        // Act
        var result = await _throttleManager.CanSendConnectionRequestAsync(_testUserId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CanSendConnectionRequestAsync_BelowLimit_ShouldReturnTrue()
    {
        // Arrange - Add 10 connections today
        var today = DateTime.UtcNow.Date;
        for (int i = 0; i < 10; i++)
        {
            _context.ThrottleLogs.Add(new ThrottleLog
            {
                Id = Guid.NewGuid(),
                UserId = _testUserId,
                ActionType = "ConnectionRequest",
                Platform = "LinkedIn",
                Timestamp = today.AddMinutes(i)
            });
        }
        await _context.SaveChangesAsync();

        // Act
        var result = await _throttleManager.CanSendConnectionRequestAsync(_testUserId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CanSendConnectionRequestAsync_YesterdayLogs_ShouldNotCount()
    {
        // Arrange - Add 25 connections yesterday
        var yesterday = DateTime.UtcNow.Date.AddDays(-1);
        for (int i = 0; i < 25; i++)
        {
            _context.ThrottleLogs.Add(new ThrottleLog
            {
                Id = Guid.NewGuid(),
                UserId = _testUserId,
                ActionType = "ConnectionRequest",
                Platform = "LinkedIn",
                Timestamp = yesterday.AddMinutes(i)
            });
        }
        await _context.SaveChangesAsync();

        // Act
        var result = await _throttleManager.CanSendConnectionRequestAsync(_testUserId);

        // Assert - Yesterday's logs should not count, so today's limit is available
        Assert.True(result);
    }

    #endregion

    #region CanSendMessageAsync Tests (Max 80/day)

    [Fact]
    public async Task CanSendMessageAsync_AtMaxLimit_ShouldReturnFalse()
    {
        // Arrange - Add 80 messages today (max limit)
        var today = DateTime.UtcNow.Date;
        for (int i = 0; i < 80; i++)
        {
            _context.ThrottleLogs.Add(new ThrottleLog
            {
                Id = Guid.NewGuid(),
                UserId = _testUserId,
                ActionType = "MessageSent",
                Platform = "LinkedIn",
                Timestamp = today.AddMinutes(i)
            });
        }
        await _context.SaveChangesAsync();

        // Act
        var result = await _throttleManager.CanSendMessageAsync(_testUserId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CanSendMessageAsync_BelowLimit_ShouldReturnTrue()
    {
        // Arrange - Add 40 messages today (below 80 max limit)
        var today = DateTime.UtcNow.Date;
        for (int i = 0; i < 40; i++)
        {
            _context.ThrottleLogs.Add(new ThrottleLog
            {
                Id = Guid.NewGuid(),
                UserId = _testUserId,
                ActionType = "MessageSent",
                Platform = "LinkedIn",
                Timestamp = today.AddMinutes(i)
            });
        }
        await _context.SaveChangesAsync();

        // Act
        var result = await _throttleManager.CanSendMessageAsync(_testUserId);

        // Assert
        Assert.True(result);
    }

    #endregion

    #region RecordConnectionRequestAsync Tests

    [Fact]
    public async Task RecordConnectionRequestAsync_ShouldCreateThrottleLog()
    {
        // Act
        await _throttleManager.RecordConnectionRequestAsync(_testUserId);

        // Assert
        var logs = await _context.ThrottleLogs
            .Where(tl => tl.UserId == _testUserId && tl.ActionType == "ConnectionRequest")
            .ToListAsync();

        Assert.Single(logs);
        Assert.Equal("LinkedIn", logs[0].Platform);
    }

    #endregion

    #region RecordMessageSentAsync Tests

    [Fact]
    public async Task RecordMessageSentAsync_ShouldCreateThrottleLog()
    {
        // Act
        await _throttleManager.RecordMessageSentAsync(_testUserId);

        // Assert
        var log = await _context.ThrottleLogs
            .FirstOrDefaultAsync(tl => tl.UserId == _testUserId && tl.ActionType == "MessageSent");

        Assert.NotNull(log);
        Assert.Equal("LinkedIn", log.Platform);
    }

    #endregion

    #region GetRandomDelay Tests

    [Fact]
    public void GetRandomDelay_ShouldReturnDelayWithinRange()
    {
        // Act
        var delay = _throttleManager.GetRandomDelay();

        // Assert - Delay should be between 2-8 minutes (plus seconds)
        Assert.True(delay.TotalMinutes >= 2);
        Assert.True(delay.TotalMinutes <= 9); // 8 minutes + up to 59 seconds
    }

    [Fact]
    public void GetRandomDelay_MultipleCallsShouldVary()
    {
        // Act
        var delays = new List<TimeSpan>();
        for (int i = 0; i < 10; i++)
        {
            delays.Add(_throttleManager.GetRandomDelay());
        }

        // Assert - Not all delays should be identical (randomness)
        var uniqueDelays = delays.Select(d => d.TotalSeconds).Distinct().Count();
        Assert.True(uniqueDelays > 1, "Random delays should vary");
    }

    #endregion

    #region GetQuotaStatusAsync Tests

    [Fact]
    public async Task GetQuotaStatusAsync_NoActions_ShouldReturnZeroCounts()
    {
        // Act
        var status = await _throttleManager.GetQuotaStatusAsync(_testUserId);

        // Assert
        Assert.Equal(0, status.ConnectionRequestsToday);
        Assert.Equal(0, status.MessagesSentToday);
        Assert.Equal(20, status.MaxConnectionRequests);
        Assert.Equal(80, status.MaxMessages);
    }

    [Fact]
    public async Task GetQuotaStatusAsync_WithActions_ShouldReturnCorrectCounts()
    {
        // Arrange
        var today = DateTime.UtcNow.Date;
        
        // Add 5 connection requests
        for (int i = 0; i < 5; i++)
        {
            _context.ThrottleLogs.Add(new ThrottleLog
            {
                Id = Guid.NewGuid(),
                UserId = _testUserId,
                ActionType = "ConnectionRequest",
                Platform = "LinkedIn",
                Timestamp = today.AddMinutes(i)
            });
        }
        
        // Add 10 messages
        for (int i = 0; i < 10; i++)
        {
            _context.ThrottleLogs.Add(new ThrottleLog
            {
                Id = Guid.NewGuid(),
                UserId = _testUserId,
                ActionType = "MessageSent",
                Platform = "LinkedIn",
                Timestamp = today.AddMinutes(i + 10)
            });
        }
        await _context.SaveChangesAsync();

        // Act
        var status = await _throttleManager.GetQuotaStatusAsync(_testUserId);

        // Assert
        Assert.Equal(5, status.ConnectionRequestsToday);
        Assert.Equal(10, status.MessagesSentToday);
    }

    #endregion

    #region ShouldQueueOperationAsync Tests

    [Fact]
    public async Task ShouldQueueOperationAsync_ConnectionRequest_BelowLimit_ShouldReturnFalse()
    {
        // Act
        var shouldQueue = await _throttleManager.ShouldQueueOperationAsync(_testUserId, "ConnectionRequest");

        // Assert
        Assert.False(shouldQueue);
    }

    [Fact]
    public async Task ShouldQueueOperationAsync_ConnectionRequest_AtLimit_ShouldReturnTrue()
    {
        // Arrange - Fill today's connection quota
        var today = DateTime.UtcNow.Date;
        for (int i = 0; i < 20; i++)
        {
            _context.ThrottleLogs.Add(new ThrottleLog
            {
                Id = Guid.NewGuid(),
                UserId = _testUserId,
                ActionType = "ConnectionRequest",
                Platform = "LinkedIn",
                Timestamp = today.AddMinutes(i)
            });
        }
        await _context.SaveChangesAsync();

        // Act
        var shouldQueue = await _throttleManager.ShouldQueueOperationAsync(_testUserId, "ConnectionRequest");

        // Assert
        Assert.True(shouldQueue);
    }

    [Fact]
    public async Task ShouldQueueOperationAsync_MessageSent_AtLimit_ShouldReturnTrue()
    {
        // Arrange - Fill today's message quota
        var today = DateTime.UtcNow.Date;
        for (int i = 0; i < 80; i++)
        {
            _context.ThrottleLogs.Add(new ThrottleLog
            {
                Id = Guid.NewGuid(),
                UserId = _testUserId,
                ActionType = "MessageSent",
                Platform = "LinkedIn",
                Timestamp = today.AddMinutes(i)
            });
        }
        await _context.SaveChangesAsync();

        // Act
        var shouldQueue = await _throttleManager.ShouldQueueOperationAsync(_testUserId, "MessageSent");

        // Assert
        Assert.True(shouldQueue);
    }

    #endregion

    #region Property-Based Tests (Property 3)

    [Fact]
    public async Task ThrottleLimits_Property3_ConnectionRequests_ShouldNeverExceed20()
    {
        // Arrange - Try to add 25 connections
        int successfulConnections = 0;

        for (int i = 0; i < 25; i++)
        {
            if (await _throttleManager.CanSendConnectionRequestAsync(_testUserId))
            {
                await _throttleManager.RecordConnectionRequestAsync(_testUserId);
                successfulConnections++;
            }
        }

        // Assert - Property 3: ConnectionRequests ≤ 20
        Assert.True(successfulConnections <= 20);
    }

    [Fact]
    public async Task ThrottleLimits_Property3_Messages_ShouldNeverExceed80()
    {
        // Arrange - Try to add 100 messages
        int successfulMessages = 0;

        for (int i = 0; i < 100; i++)
        {
            if (await _throttleManager.CanSendMessageAsync(_testUserId))
            {
                await _throttleManager.RecordMessageSentAsync(_testUserId);
                successfulMessages++;
            }
        }

        // Assert - Property 3: Messages ≤ 80
        Assert.True(successfulMessages <= 80);
    }

    #endregion
}
