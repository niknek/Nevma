using Microsoft.EntityFrameworkCore;
using Nevma.Notifications.Api.Application.Notifications;
using Nevma.Notifications.Api.Domain.Notifications;
using Nevma.Notifications.Api.Infrastructure.Persistence;

namespace Nevma.Notifications.Api.Infrastructure.Notifications;

public sealed class EfNotificationRepository(NotificationsDbContext dbContext)
    : INotificationRepository
{
    public async Task AddAsync(Notification notification, CancellationToken cancellationToken = default) =>
        await dbContext.Notifications.AddAsync(notification, cancellationToken);

    public Task<Notification?> GetAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default) =>
        dbContext.Notifications.SingleOrDefaultAsync(
            notification => notification.Id == id && notification.UserId == userId,
            cancellationToken);

    public async Task<IReadOnlyList<Notification>> ListAsync(
        Guid userId,
        int limit,
        CancellationToken cancellationToken = default) =>
        await dbContext.Notifications
            .AsNoTracking()
            .Where(notification => notification.UserId == userId)
            .OrderBy(notification => notification.ReadAt != null)
            .ThenByDescending(notification => notification.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
}
