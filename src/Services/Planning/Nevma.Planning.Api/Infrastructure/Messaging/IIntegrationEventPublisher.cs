namespace Nevma.Planning.Api.Infrastructure.Messaging;

public interface IIntegrationEventPublisher
{
    Task PublishAsync(
        Guid eventId,
        string type,
        string payload,
        CancellationToken cancellationToken = default);
}
