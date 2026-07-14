using Microsoft.EntityFrameworkCore;
using Nevma.Notifications.Api.Application.Notifications;
using Nevma.Notifications.Api.Domain.Notifications;
using Nevma.Notifications.Api.Infrastructure.Persistence;

namespace Nevma.Notifications.Api.Infrastructure.Notifications;

public sealed class EfNotificationPreferenceRepository(NotificationsDbContext dbContext)
    : INotificationPreferenceRepository
{
    public Task<NotificationPreference?> GetAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        dbContext.NotificationPreferences.SingleOrDefaultAsync(
            preference => preference.UserId == userId,
            cancellationToken);

    public async Task AddAsync(
        NotificationPreference preference,
        CancellationToken cancellationToken = default) =>
        await dbContext.NotificationPreferences.AddAsync(preference, cancellationToken);
}
