namespace Nevma.Contracts.Messaging;

public sealed record SendMessageRequest(Guid SenderId, string Text);

public sealed record MessageResponse(
    Guid Id,
    Guid ConversationId,
    Guid SenderId,
    string Text,
    DateTimeOffset SentAt);
