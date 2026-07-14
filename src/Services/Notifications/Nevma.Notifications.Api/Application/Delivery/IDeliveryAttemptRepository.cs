using Nevma.Notifications.Api.Domain.Delivery;

namespace Nevma.Notifications.Api.Application.Delivery;

public interface IDeliveryAttemptRepository
{
    Task AddAsync(DeliveryAttempt attempt, CancellationToken cancellationToken = default);
}
