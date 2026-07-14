using Nevma.Contracts.Messaging;
using Nevma.Messaging.Api.Application.Conversations;
using Nevma.Messaging.Api.Domain.Messages;

namespace Nevma.Messaging.Api.Application.Messages;

public sealed class MessageService(
    IMessageRepository messageRepository,
    IConversationRepository conversationRepository,
    IMessagingUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<MessagePageResponse?> GetMessagesAsync(
        Guid conversationId,
        Guid userId,
        long? beforeSequence,
        int requestedSize,
        CancellationToken cancellationToken = default)
    {
        if (!await conversationRepository.IsParticipantAsync(conversationId, userId, cancellationToken))
            return null;

        var pageSize = Math.Clamp(requestedSize, 1, 100);
        var messages = await messageRepository.ListAsync(
            conversationId,
            beforeSequence,
            pageSize + 1,
            cancellationToken);
        var hasMore = messages.Count > pageSize;
        var items = messages.Take(pageSize).Select(ToResponse).ToArray();
        return new MessagePageResponse(
            items,
            hasMore ? items[^1].Sequence : null);
    }

    public async Task<SendMessageResult> SendAsync(
        Guid conversationId,
        Guid senderId,
        SendMessageRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
            return new SendMessageResult.Invalid("text", "Message text is required.");
        if (request.Text.Length > 4_000)
            return new SendMessageResult.Invalid("text", "Message text cannot exceed 4000 characters.");
        if (!await conversationRepository.IsParticipantAsync(conversationId, senderId, cancellationToken))
            return new SendMessageResult.NotFound();

        var message = Message.Create(conversationId, senderId, request.Text, timeProvider.GetUtcNow());
        await messageRepository.AddAsync(message, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new SendMessageResult.Sent(ToResponse(message));
    }

    private static MessageResponse ToResponse(Message message) =>
        new(
            message.Id,
            message.Sequence,
            message.ConversationId,
            message.SenderId,
            message.Text,
            message.SentAt);
}

public abstract record SendMessageResult
{
    public sealed record Sent(MessageResponse Message) : SendMessageResult;
    public sealed record NotFound : SendMessageResult;
    public sealed record Invalid(string Field, string Error) : SendMessageResult;
}
