using Nevma.Contracts.Notifications;

namespace Nevma.Notifications.Api.Application.Notifications;

public interface ILiveNotificationPublisher
{
    Task PublishAsync(
        Guid userId,
        NotificationResponse notification,
        CancellationToken cancellationToken = default);
}
