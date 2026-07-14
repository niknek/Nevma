namespace Nevma.Messaging.Api.Domain.Messages;

public sealed class MessageReaction
{
    private MessageReaction(Guid messageId, Guid userId, string emoji, DateTimeOffset createdAt)
    {
        MessageId = messageId;
        UserId = userId;
        Emoji = emoji;
        CreatedAt = createdAt;
    }

    public Guid MessageId { get; }
    public Guid UserId { get; }
    public string Emoji { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static MessageReaction Create(Guid messageId, Guid userId, string emoji, DateTimeOffset createdAt) =>
        new(messageId, userId, emoji, createdAt);

    public void Change(string emoji, DateTimeOffset changedAt)
    {
        Emoji = emoji;
        CreatedAt = changedAt;
    }
}
