using DistroCv.Core.Entities;

namespace DistroCv.Core.Interfaces;

/// <summary>
/// Service for managing user notifications
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Creates a notification for a new job match
    /// </summary>
    Task<Notification> CreateNewMatchNotificationAsync(Guid userId, JobMatch jobMatch, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a notification for application status update
    /// </summary>
    Task<Notification> CreateApplicationStatusNotificationAsync(Guid userId, Application application, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets unread notifications for a user
    /// </summary>
    Task<List<Notification>> GetUnreadNotificationsAsync(Guid userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all notifications for a user with pagination
    /// </summary>
    Task<List<Notification>> GetUserNotificationsAsync(Guid userId, int skip = 0, int take = 50, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Marks a notification as read
    /// </summary>
    Task MarkAsReadAsync(Guid notificationId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Marks all notifications as read for a user
    /// </summary>
    Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets unread notification count for a user
    /// </summary>
    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deletes old notifications (older than specified days)
    /// </summary>
    Task DeleteOldNotificationsAsync(int olderThanDays = 30, CancellationToken cancellationToken = default);
}
