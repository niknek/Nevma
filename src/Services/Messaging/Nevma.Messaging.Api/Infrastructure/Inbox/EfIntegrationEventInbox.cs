using Microsoft.EntityFrameworkCore;
using Nevma.Messaging.Api.Application.Integration;
using Nevma.Messaging.Api.Infrastructure.Persistence;

namespace Nevma.Messaging.Api.Infrastructure.Inbox;

public sealed class EfIntegrationEventInbox(MessagingDbContext dbContext) : IIntegrationEventInbox
{
    public Task<bool> ExistsAsync(Guid eventId, CancellationToken cancellationToken = default) =>
        dbContext.InboxMessages.AnyAsync(message => message.Id == eventId, cancellationToken);

    public void Add(Guid eventId, string type, DateTimeOffset processedAt) =>
        dbContext.InboxMessages.Add(InboxMessage.Create(eventId, type, processedAt));
}
