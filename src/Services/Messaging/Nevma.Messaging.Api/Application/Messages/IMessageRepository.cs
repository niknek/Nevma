using Nevma.Messaging.Api.Domain.Messages;

namespace Nevma.Messaging.Api.Application.Messages;

public interface IMessageRepository
{
    Task AddAsync(Message message, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Message>> ListAsync(
        Guid conversationId,
        long? beforeSequence,
        int limit,
        CancellationToken cancellationToken = default);
}
