using DistroCv.Core.Entities;
using DistroCv.Core.Interfaces;
using DistroCv.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DistroCv.Infrastructure.Services;

/// <summary>
/// Service for managing rate limiting and throttling for LinkedIn operations
/// </summary>
public class ThrottleManager : IThrottleManager
{
    private readonly DistroCvDbContext _context;
    private readonly ILogger<ThrottleManager> _logger;
    private readonly Random _random;

    // Constants for throttle limits
    private const int MAX_CONNECTIONS_PER_DAY = 20;
    private const int MIN_MESSAGES_PER_DAY = 50;
    private const int MAX_MESSAGES_PER_DAY = 80;
    private const int MIN_DELAY_MINUTES = 2;
    private const int MAX_DELAY_MINUTES = 8;

    public ThrottleManager(
        DistroCvDbContext context,
        ILogger<ThrottleManager> logger)
    {
        _context = context;
        _logger = logger;
        _random = new Random();
    }

    /// <summary>
    /// Checks if user can send a LinkedIn connection request (Validates: Requirement 6.1)
    /// </summary>
    public async Task<bool> CanSendConnectionRequestAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Checking connection request quota for user {UserId}", userId);

        try
        {
            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            var connectionCount = await _context.ThrottleLogs
                .Where(tl => tl.UserId == userId 
                    && tl.ActionType == "ConnectionRequest"
                    && tl.Timestamp >= today 
                    && tl.Timestamp < tomorrow)
                .CountAsync(cancellationToken);

            var canSend = connectionCount < MAX_CONNECTIONS_PER_DAY;
            
            _logger.LogInformation(
                "User {UserId} has sent {Count}/{Max} connection requests today. Can send: {CanSend}",
                userId, connectionCount, MAX_CONNECTIONS_PER_DAY, canSend);

            return canSend;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking connection request quota for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Checks if user can send a LinkedIn message (Validates: Requirement 6.2)
    /// </summary>
    public async Task<bool> CanSendMessageAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Checking message quota for user {UserId}", userId);

        try
        {
            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            var messageCount = await _context.ThrottleLogs
                .Where(tl => tl.UserId == userId 
                    && tl.ActionType == "MessageSent"
                    && tl.Timestamp >= today 
                    && tl.Timestamp < tomorrow)
                .CountAsync(cancellationToken);

            var canSend = messageCount < MAX_MESSAGES_PER_DAY;
            
            _logger.LogInformation(
                "User {UserId} has sent {Count}/{Max} messages today. Can send: {CanSend}",
                userId, messageCount, MAX_MESSAGES_PER_DAY, canSend);

            return canSend;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking message quota for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Records a LinkedIn connection request
    /// </summary>
    public async Task RecordConnectionRequestAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Recording connection request for user {UserId}", userId);

        try
        {
            var log = new ThrottleLog
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ActionType = "ConnectionRequest",
                Platform = "LinkedIn",
                Timestamp = DateTime.UtcNow
            };

            _context.ThrottleLogs.Add(log);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully recorded connection request for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording connection request for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Records a LinkedIn message sent
    /// </summary>
    public async Task RecordMessageSentAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Recording message sent for user {UserId}", userId);

        try
        {
            var log = new ThrottleLog
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ActionType = "MessageSent",
                Platform = "LinkedIn",
                Timestamp = DateTime.UtcNow
            };

            _context.ThrottleLogs.Add(log);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully recorded message sent for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording message sent for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Gets random delay between operations (2-8 minutes) (Validates: Requirement 6.3)
    /// </summary>
    public TimeSpan GetRandomDelay()
    {
        var delayMinutes = _random.Next(MIN_DELAY_MINUTES, MAX_DELAY_MINUTES + 1);
        var delaySeconds = _random.Next(0, 60); // Add random seconds for more natural behavior
        
        var delay = TimeSpan.FromMinutes(delayMinutes).Add(TimeSpan.FromSeconds(delaySeconds));
        
        _logger.LogInformation("Generated random delay: {Delay}", delay);
        
        return delay;
    }

    /// <summary>
    /// Gets daily quota status for user
    /// </summary>
    public async Task<ThrottleQuotaStatus> GetQuotaStatusAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting quota status for user {UserId}", userId);

        try
        {
            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            var connectionCount = await _context.ThrottleLogs
                .Where(tl => tl.UserId == userId 
                    && tl.ActionType == "ConnectionRequest"
                    && tl.Timestamp >= today 
                    && tl.Timestamp < tomorrow)
                .CountAsync(cancellationToken);

            var messageCount = await _context.ThrottleLogs
                .Where(tl => tl.UserId == userId 
                    && tl.ActionType == "MessageSent"
                    && tl.Timestamp >= today 
                    && tl.Timestamp < tomorrow)
                .CountAsync(cancellationToken);

            var status = new ThrottleQuotaStatus
            {
                ConnectionRequestsToday = connectionCount,
                MaxConnectionRequests = MAX_CONNECTIONS_PER_DAY,
                MessagesSentToday = messageCount,
                MaxMessages = MAX_MESSAGES_PER_DAY,
                QuotaResetTime = tomorrow
            };

            _logger.LogInformation(
                "Quota status for user {UserId}: Connections {Connections}/{MaxConnections}, Messages {Messages}/{MaxMessages}",
                userId, connectionCount, MAX_CONNECTIONS_PER_DAY, messageCount, MAX_MESSAGES_PER_DAY);

            return status;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting quota status for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Checks if operation should be queued due to quota exceeded (Validates: Requirement 6.4)
    /// </summary>
    public async Task<bool> ShouldQueueOperationAsync(
        Guid userId, 
        string operationType, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Checking if operation {OperationType} should be queued for user {UserId}", 
            operationType, userId);

        try
        {
            bool shouldQueue = operationType switch
            {
                "ConnectionRequest" => !await CanSendConnectionRequestAsync(userId, cancellationToken),
                "MessageSent" => !await CanSendMessageAsync(userId, cancellationToken),
                _ => false
            };

            if (shouldQueue)
            {
                _logger.LogWarning(
                    "Operation {OperationType} for user {UserId} should be queued due to quota exceeded",
                    operationType, userId);
            }

            return shouldQueue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if operation should be queued for user {UserId}", userId);
            throw;
        }
    }
}
