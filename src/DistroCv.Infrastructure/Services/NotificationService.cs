using DistroCv.Core.Entities;
using DistroCv.Core.Interfaces;
using DistroCv.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DistroCv.Infrastructure.Services;

/// <summary>
/// Service for managing user notifications
/// </summary>
public class NotificationService : INotificationService
{
    private readonly DistroCvDbContext _context;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        DistroCvDbContext context,
        ILogger<NotificationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Creates a notification for a new job match
    /// </summary>
    public async Task<Notification> CreateNewMatchNotificationAsync(
        Guid userId, 
        JobMatch jobMatch, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating new match notification for user {UserId}, match {MatchId}", userId, jobMatch.Id);

            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Title = "New Job Match Found!",
                Message = $"You have a new job match: {jobMatch.JobPosting?.Title ?? "Unknown"} at {jobMatch.JobPosting?.CompanyName ?? "Unknown Company"} with {jobMatch.MatchScore}% match score.",
                Type = NotificationType.NewMatch,
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                RelatedEntityId = jobMatch.Id,
                RelatedEntityType = "JobMatch"
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created notification {NotificationId} for user {UserId}", notification.Id, userId);
            return notification;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating new match notification for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Creates a notification for application status update
    /// </summary>
    public async Task<Notification> CreateApplicationStatusNotificationAsync(
        Guid userId, 
        Application application, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating application status notification for user {UserId}, application {ApplicationId}", userId, application.Id);

            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Title = "Application Status Update",
                Message = $"Your application to {application.JobMatch?.JobPosting?.CompanyName ?? "Unknown Company"} has been updated to: {application.Status}",
                Type = NotificationType.ApplicationStatusUpdate,
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                RelatedEntityId = application.Id,
                RelatedEntityType = "Application"
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created notification {NotificationId} for user {UserId}", notification.Id, userId);
            return notification;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating application status notification for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Gets unread notifications for a user
    /// </summary>
    public async Task<List<Notification>> GetUnreadNotificationsAsync(
        Guid userId, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync(cancellationToken);

            _logger.LogDebug("Retrieved {Count} unread notifications for user {UserId}", notifications.Count, userId);
            return notifications;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving unread notifications for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Gets all notifications for a user with pagination
    /// </summary>
    public async Task<List<Notification>> GetUserNotificationsAsync(
        Guid userId, 
        int skip = 0, 
        int take = 50, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);

            _logger.LogDebug("Retrieved {Count} notifications for user {UserId} (skip: {Skip}, take: {Take})", 
                notifications.Count, userId, skip, take);
            
            return notifications;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving notifications for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Marks a notification as read
    /// </summary>
    public async Task MarkAsReadAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId, cancellationToken);

            if (notification == null)
            {
                _logger.LogWarning("Notification {NotificationId} not found", notificationId);
                return;
            }

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Marked notification {NotificationId} as read", notificationId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification {NotificationId} as read", notificationId);
            throw;
        }
    }

    /// <summary>
    /// Marks all notifications as read for a user
    /// </summary>
    public async Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var unreadNotifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync(cancellationToken);

            if (unreadNotifications.Any())
            {
                var now = DateTime.UtcNow;
                foreach (var notification in unreadNotifications)
                {
                    notification.IsRead = true;
                    notification.ReadAt = now;
                }

                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Marked {Count} notifications as read for user {UserId}", unreadNotifications.Count, userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all notifications as read for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Gets unread notification count for a user
    /// </summary>
    public async Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var count = await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead, cancellationToken);

            _logger.LogDebug("User {UserId} has {Count} unread notifications", userId, count);
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unread count for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Deletes old notifications (older than specified days)
    /// </summary>
    public async Task DeleteOldNotificationsAsync(int olderThanDays = 30, CancellationToken cancellationToken = default)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-olderThanDays);
            
            var oldNotifications = await _context.Notifications
                .Where(n => n.CreatedAt < cutoffDate)
                .ToListAsync(cancellationToken);

            if (oldNotifications.Any())
            {
                _context.Notifications.RemoveRange(oldNotifications);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Deleted {Count} notifications older than {Days} days", oldNotifications.Count, olderThanDays);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting old notifications");
            throw;
        }
    }
}
