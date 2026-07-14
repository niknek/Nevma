using Microsoft.EntityFrameworkCore;
using Nevma.Notifications.Api.Application.Integration;
using Nevma.Notifications.Api.Infrastructure.Persistence;

namespace Nevma.Notifications.Api.Infrastructure.Inbox;

public sealed class EfIntegrationEventInbox(NotificationsDbContext dbContext)
    : IIntegrationEventInbox
{
    public Task<bool> ExistsAsync(Guid eventId, CancellationToken cancellationToken = default) =>
        dbContext.InboxMessages.AnyAsync(message => message.Id == eventId, cancellationToken);

    public void Add(Guid eventId, string type, DateTimeOffset processedAt) =>
        dbContext.InboxMessages.Add(InboxMessage.Create(eventId, type, processedAt));
}
