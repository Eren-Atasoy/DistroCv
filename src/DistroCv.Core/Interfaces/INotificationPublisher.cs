using DistroCv.Core.Entities;

namespace DistroCv.Core.Interfaces;

public interface INotificationPublisher
{
    Task PublishNotificationAsync(Guid userId, Notification notification);
    Task PublishStatsUpdateAsync(Guid userId, string type, object data);
}
