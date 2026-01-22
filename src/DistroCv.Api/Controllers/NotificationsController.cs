using DistroCv.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DistroCv.Api.Controllers;

/// <summary>
/// Controller for managing user notifications
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class NotificationsController : BaseApiController
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(
        INotificationService notificationService,
        ILogger<NotificationsController> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// Gets unread notifications for the current user
    /// </summary>
    [HttpGet("unread")]
    public async Task<IActionResult> GetUnreadNotifications(CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetUserId();
            var notifications = await _notificationService.GetUnreadNotificationsAsync(userId, cancellationToken);
            
            return Ok(new
            {
                count = notifications.Count,
                notifications = notifications.Select(n => new
                {
                    id = n.Id,
                    title = n.Title,
                    message = n.Message,
                    type = n.Type.ToString(),
                    createdAt = n.CreatedAt,
                    relatedEntityId = n.RelatedEntityId,
                    relatedEntityType = n.RelatedEntityType
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving unread notifications");
            return StatusCode(500, new { error = "Failed to retrieve notifications" });
        }
    }

    /// <summary>
    /// Gets all notifications for the current user with pagination
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetNotifications(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetUserId();
            var notifications = await _notificationService.GetUserNotificationsAsync(userId, skip, take, cancellationToken);
            
            return Ok(new
            {
                count = notifications.Count,
                skip,
                take,
                notifications = notifications.Select(n => new
                {
                    id = n.Id,
                    title = n.Title,
                    message = n.Message,
                    type = n.Type.ToString(),
                    isRead = n.IsRead,
                    createdAt = n.CreatedAt,
                    readAt = n.ReadAt,
                    relatedEntityId = n.RelatedEntityId,
                    relatedEntityType = n.RelatedEntityType
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving notifications");
            return StatusCode(500, new { error = "Failed to retrieve notifications" });
        }
    }

    /// <summary>
    /// Gets unread notification count for the current user
    /// </summary>
    [HttpGet("count")]
    public async Task<IActionResult> GetUnreadCount(CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetUserId();
            var count = await _notificationService.GetUnreadCountAsync(userId, cancellationToken);
            
            return Ok(new { count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving unread count");
            return StatusCode(500, new { error = "Failed to retrieve unread count" });
        }
    }

    /// <summary>
    /// Marks a notification as read
    /// </summary>
    [HttpPut("{notificationId}/read")]
    public async Task<IActionResult> MarkAsRead(Guid notificationId, CancellationToken cancellationToken)
    {
        try
        {
            await _notificationService.MarkAsReadAsync(notificationId, cancellationToken);
            return Ok(new { message = "Notification marked as read" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification as read");
            return StatusCode(500, new { error = "Failed to mark notification as read" });
        }
    }

    /// <summary>
    /// Marks all notifications as read for the current user
    /// </summary>
    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetUserId();
            await _notificationService.MarkAllAsReadAsync(userId, cancellationToken);
            return Ok(new { message = "All notifications marked as read" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all notifications as read");
            return StatusCode(500, new { error = "Failed to mark all notifications as read" });
        }
    }

    /// <summary>
    /// Gets the current user's ID from claims
    /// </summary>
    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("User ID not found in token");
        }
        return userId;
    }
}
