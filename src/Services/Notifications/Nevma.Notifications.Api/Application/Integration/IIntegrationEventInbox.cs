namespace Nevma.Notifications.Api.Application.Integration;

public interface IIntegrationEventInbox
{
    Task<bool> ExistsAsync(Guid eventId, CancellationToken cancellationToken = default);
    void Add(Guid eventId, string type, DateTimeOffset processedAt);
}
