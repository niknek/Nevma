using Nevma.Notifications.Api.Domain.Notifications;

namespace Nevma.Notifications.Api.Application.Notifications;

public interface INotificationPreferenceRepository
{
    Task<NotificationPreference?> GetAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(NotificationPreference preference, CancellationToken cancellationToken = default);
}
