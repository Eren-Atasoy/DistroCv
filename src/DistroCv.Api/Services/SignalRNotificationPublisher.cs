using DistroCv.Api.Hubs;
using DistroCv.Core.Entities;
using DistroCv.Core.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace DistroCv.Api.Services;

public class SignalRNotificationPublisher : INotificationPublisher
{
    private readonly IHubContext<NotificationHub, INotificationClient> _hubContext;
    private readonly ILogger<SignalRNotificationPublisher> _logger;

    public SignalRNotificationPublisher(
        IHubContext<NotificationHub, INotificationClient> hubContext,
        ILogger<SignalRNotificationPublisher> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task PublishNotificationAsync(Guid userId, Notification notification)
    {
        try
        {
            await _hubContext.Clients.Group(userId.ToString()).ReceiveNotification(notification);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing notification via SignalR to user {UserId}", userId);
        }
    }

    public async Task PublishStatsUpdateAsync(Guid userId, string type, object data)
    {
        try
        {
            await _hubContext.Clients.Group(userId.ToString()).ReceiveStatsUpdate(type, data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing stats update via SignalR to user {UserId}", userId);
        }
    }
}
