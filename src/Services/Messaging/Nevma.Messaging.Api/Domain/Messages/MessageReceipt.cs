namespace Nevma.Messaging.Api.Domain.Messages;

public sealed class MessageReceipt
{
    private MessageReceipt(
        Guid messageId,
        Guid userId,
        DateTimeOffset? deliveredAt,
        DateTimeOffset? readAt)
    {
        MessageId = messageId;
        UserId = userId;
        DeliveredAt = deliveredAt;
        ReadAt = readAt;
    }

    public Guid MessageId { get; }
    public Guid UserId { get; }
    public DateTimeOffset? DeliveredAt { get; private set; }
    public DateTimeOffset? ReadAt { get; private set; }

    public static MessageReceipt Create(Guid messageId, Guid userId) =>
        new(messageId, userId, null, null);

    public void MarkDelivered(DateTimeOffset at) => DeliveredAt ??= at;

    public void MarkRead(DateTimeOffset at)
    {
        DeliveredAt ??= at;
        ReadAt ??= at;
    }
}
