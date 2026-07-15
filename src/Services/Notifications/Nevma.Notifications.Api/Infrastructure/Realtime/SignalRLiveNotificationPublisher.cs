using Microsoft.AspNetCore.SignalR;
using Nevma.Contracts.Notifications;
using Nevma.Notifications.Api.Application.Notifications;
using Nevma.Notifications.Api.Realtime;

namespace Nevma.Notifications.Api.Infrastructure.Realtime;

public sealed class SignalRLiveNotificationPublisher(IHubContext<NotificationHub> hubContext)
    : ILiveNotificationPublisher
{
    public Task PublishAsync(
        Guid userId,
        NotificationResponse notification,
        CancellationToken cancellationToken = default) =>
        hubContext.Clients.Group(NotificationHub.UserGroupName(userId))
            .SendAsync("notification.received", notification, cancellationToken);
}
