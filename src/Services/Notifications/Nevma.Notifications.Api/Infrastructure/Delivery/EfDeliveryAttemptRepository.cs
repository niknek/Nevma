using Nevma.Notifications.Api.Application.Delivery;
using Nevma.Notifications.Api.Domain.Delivery;
using Nevma.Notifications.Api.Infrastructure.Persistence;

namespace Nevma.Notifications.Api.Infrastructure.Delivery;

public sealed class EfDeliveryAttemptRepository(NotificationsDbContext dbContext)
    : IDeliveryAttemptRepository
{
    public async Task AddAsync(
        DeliveryAttempt attempt,
        CancellationToken cancellationToken = default) =>
        await dbContext.DeliveryAttempts.AddAsync(attempt, cancellationToken);
}
