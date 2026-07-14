using Microsoft.EntityFrameworkCore;
using Nevma.Messaging.Api.Application.Messages;
using Nevma.Messaging.Api.Domain.Messages;
using Nevma.Messaging.Api.Infrastructure.Persistence;

namespace Nevma.Messaging.Api.Infrastructure.Messages;

public sealed class EfMessageRepository(MessagingDbContext dbContext) : IMessageRepository
{
    public async Task AddAsync(Message message, CancellationToken cancellationToken = default) =>
        await dbContext.Messages.AddAsync(message, cancellationToken);

    public async Task<IReadOnlyList<Message>> ListAsync(
        Guid conversationId,
        long? beforeSequence,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Messages
            .AsNoTracking()
            .Where(message => message.ConversationId == conversationId);
        if (beforeSequence is not null)
            query = query.Where(message => message.Sequence < beforeSequence);

        return await query
            .OrderByDescending(message => message.Sequence)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }
}
