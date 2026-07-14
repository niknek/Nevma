using Microsoft.EntityFrameworkCore;
using Nevma.Notifications.Api.Domain.Delivery;
using Nevma.Notifications.Api.Domain.Notifications;
using Nevma.Notifications.Api.Domain.PushDevices;
using Nevma.Notifications.Api.Infrastructure.Persistence;

namespace Nevma.Notifications.Api.Infrastructure.Delivery;

public sealed class DeliveryAttemptStore(NotificationsDbContext dbContext)
{
    public async Task<IReadOnlyList<DeliveryWorkItem>> ClaimBatchAsync(
        DateTimeOffset now,
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        var candidateIds = await dbContext.DeliveryAttempts
            .AsNoTracking()
            .Where(attempt =>
                attempt.Status == DeliveryStatus.Pending &&
                attempt.NextAttemptAt <= now &&
                (attempt.LockedUntil == null || attempt.LockedUntil <= now))
            .OrderBy(attempt => attempt.CreatedAt)
            .Select(attempt => attempt.Id)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
        if (candidateIds.Count == 0)
            return [];

        var claimId = Guid.NewGuid();
        var lockedUntil = now.AddMinutes(1);
        foreach (var candidateId in candidateIds)
        {
            await dbContext.DeliveryAttempts
                .Where(attempt =>
                    attempt.Id == candidateId &&
                    attempt.Status == DeliveryStatus.Pending &&
                    attempt.NextAttemptAt <= now &&
                    (attempt.LockedUntil == null || attempt.LockedUntil <= now))
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(attempt => attempt.LockId, claimId)
                        .SetProperty(attempt => attempt.LockedUntil, lockedUntil),
                    cancellationToken);
        }

        return await (
            from attempt in dbContext.DeliveryAttempts
            join notification in dbContext.Notifications
                on attempt.NotificationId equals notification.Id
            join device in dbContext.PushDevices
                on attempt.PushDeviceId equals device.Id
            where attempt.LockId == claimId
            orderby attempt.CreatedAt
            select new DeliveryWorkItem(attempt, notification, device))
            .ToListAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}

public sealed record DeliveryWorkItem(
    DeliveryAttempt Attempt,
    Notification Notification,
    PushDevice Device);
