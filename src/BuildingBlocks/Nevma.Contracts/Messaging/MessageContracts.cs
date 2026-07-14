namespace Nevma.Contracts.Messaging;

public sealed record CreateConversationRequest(
    ConversationKind Kind,
    string? Title,
    IReadOnlyCollection<Guid> ParticipantIds);

public sealed record ConversationResponse(
    Guid Id,
    ConversationKind Kind,
    string? Title,
    IReadOnlyCollection<Guid> ParticipantIds,
    DateTimeOffset CreatedAt);

public sealed record SendMessageRequest(string Text);

public sealed record MessageResponse(
    Guid Id,
    long Sequence,
    Guid ConversationId,
    Guid SenderId,
    string Text,
    DateTimeOffset SentAt);

public sealed record MessagePageResponse(
    IReadOnlyCollection<MessageResponse> Items,
    long? NextBefore);

public enum ConversationKind
{
    Personal,
    Group
}
