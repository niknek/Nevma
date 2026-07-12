using Nevma.Contracts.Messaging;
using Nevma.Messaging.Api.Domain.Messages;

namespace Nevma.Messaging.Api.Application.Messages;

public sealed class MessageService(IMessageRepository repository, TimeProvider timeProvider)
{
    public IReadOnlyCollection<MessageResponse> GetMessages(Guid conversationId) =>
        repository.GetByConversation(conversationId).Select(ToResponse).ToArray();

    public SendMessageResult Send(Guid conversationId, SendMessageRequest request)
    {
        if (request.SenderId == Guid.Empty || string.IsNullOrWhiteSpace(request.Text))
        {
            return SendMessageResult.Failure("message", "Sender and text are required.");
        }

        var message = Message.Create(conversationId, request.SenderId, request.Text, timeProvider.GetUtcNow());
        repository.Add(message);
        return SendMessageResult.Success(ToResponse(message));
    }

    private static MessageResponse ToResponse(Message message) =>
        new(message.Id, message.ConversationId, message.SenderId, message.Text, message.SentAt);
}

public sealed record SendMessageResult(
    MessageResponse? Message,
    IReadOnlyDictionary<string, string[]> Errors)
{
    public bool IsSuccess => Message is not null;

    public static SendMessageResult Success(MessageResponse message) =>
        new(message, new Dictionary<string, string[]>());

    public static SendMessageResult Failure(string field, string error) =>
        new(null, new Dictionary<string, string[]> { [field] = [error] });
}
