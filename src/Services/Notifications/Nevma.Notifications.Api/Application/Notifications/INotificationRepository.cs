using Nevma.Notifications.Api.Domain.Notifications;

namespace Nevma.Notifications.Api.Application.Notifications;

public interface INotificationRepository
{
    Task AddAsync(Notification notification, CancellationToken cancellationToken = default);
    Task<Notification?> GetAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Notification>> ListAsync(
        Guid userId,
        int limit,
        CancellationToken cancellationToken = default);
}
