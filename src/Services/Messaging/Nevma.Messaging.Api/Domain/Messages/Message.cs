namespace Nevma.Messaging.Api.Domain.Messages;

public sealed class Message
{
    private Message(Guid id, Guid conversationId, Guid senderId, string text, DateTimeOffset sentAt)
    {
        Id = id;
        ConversationId = conversationId;
        SenderId = senderId;
        Text = text;
        SentAt = sentAt;
    }

    public Guid Id { get; }
    public long Sequence { get; private set; }
    public Guid ConversationId { get; }
    public Guid SenderId { get; }
    public string Text { get; }
    public DateTimeOffset SentAt { get; }

    public static Message Create(
        Guid conversationId,
        Guid senderId,
        string text,
        DateTimeOffset sentAt) =>
        new(Guid.NewGuid(), conversationId, senderId, text.Trim(), sentAt);
}
