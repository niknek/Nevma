namespace Nevma.Messaging.Api.Domain.Conversations;

public sealed class ConversationParticipant
{
    private ConversationParticipant(Guid conversationId, Guid userId, DateTimeOffset joinedAt)
    {
        ConversationId = conversationId;
        UserId = userId;
        JoinedAt = joinedAt;
    }

    public Guid ConversationId { get; }
    public Guid UserId { get; }
    public DateTimeOffset JoinedAt { get; }

    internal static ConversationParticipant Create(
        Guid conversationId,
        Guid userId,
        DateTimeOffset joinedAt) =>
        new(conversationId, userId, joinedAt);
}
