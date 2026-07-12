using System.Collections.Concurrent;
using Nevma.Messaging.Api.Application.Messages;
using Nevma.Messaging.Api.Domain.Messages;

namespace Nevma.Messaging.Api.Infrastructure.Messages;

public sealed class InMemoryMessageRepository : IMessageRepository
{
    private readonly ConcurrentDictionary<Guid, ConcurrentQueue<Message>> _messages = new();

    public void Add(Message message) =>
        _messages.GetOrAdd(message.ConversationId, _ => new ConcurrentQueue<Message>()).Enqueue(message);

    public IReadOnlyCollection<Message> GetByConversation(Guid conversationId) =>
        _messages.TryGetValue(conversationId, out var messages) ? messages.ToArray() : [];
}
