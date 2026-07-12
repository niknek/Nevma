using Nevma.Messaging.Api.Domain.Messages;

namespace Nevma.Messaging.Api.Application.Messages;

public interface IMessageRepository
{
    void Add(Message message);
    IReadOnlyCollection<Message> GetByConversation(Guid conversationId);
}
