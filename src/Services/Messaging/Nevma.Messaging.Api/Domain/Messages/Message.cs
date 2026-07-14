namespace Nevma.Messaging.Api.Domain.Messages;

public sealed class Message
{
    private readonly List<MessageAttachment> _attachments = [];
    private Message(
        Guid id,
        Guid conversationId,
        Guid senderId,
        string text,
        Guid? replyToMessageId,
        DateTimeOffset sentAt,
        DateTimeOffset? editedAt,
        DateTimeOffset? deletedAt)
    {
        Id = id;
        ConversationId = conversationId;
        SenderId = senderId;
        Text = text;
        ReplyToMessageId = replyToMessageId;
        SentAt = sentAt;
        EditedAt = editedAt;
        DeletedAt = deletedAt;
    }

    public Guid Id { get; }
    public long Sequence { get; private set; }
    public Guid ConversationId { get; }
    public Guid SenderId { get; }
    public string Text { get; private set; }
    public Guid? ReplyToMessageId { get; }
    public DateTimeOffset SentAt { get; }
    public DateTimeOffset? EditedAt { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }
    public IReadOnlyCollection<MessageAttachment> Attachments => _attachments.AsReadOnly();

    public static Message Create(
        Guid conversationId,
        Guid senderId,
        string text,
        Guid? replyToMessageId,
        IReadOnlyCollection<Guid>? attachmentIds,
        DateTimeOffset sentAt) =>
        CreateWithAttachments(conversationId, senderId, text, replyToMessageId, attachmentIds, sentAt);

    private static Message CreateWithAttachments(
        Guid conversationId,
        Guid senderId,
        string text,
        Guid? replyToMessageId,
        IReadOnlyCollection<Guid>? attachmentIds,
        DateTimeOffset sentAt)
    {
        var message = new Message(Guid.NewGuid(), conversationId, senderId, text.Trim(), replyToMessageId, sentAt, null, null);
        message._attachments.AddRange((attachmentIds ?? []).Distinct().Select(id => MessageAttachment.Create(message.Id, id)));
        return message;
    }

    public static Message Create(
        Guid conversationId,
        Guid senderId,
        string text,
        DateTimeOffset sentAt) =>
        Create(conversationId, senderId, text, null, null, sentAt);

    public bool Edit(Guid actorId, string text, DateTimeOffset editedAt, TimeSpan allowedWindow)
    {
        if (SenderId != actorId || DeletedAt is not null || editedAt - SentAt > allowedWindow)
            return false;
        Text = text.Trim();
        EditedAt = editedAt;
        return true;
    }

    public bool Delete(Guid actorId, DateTimeOffset deletedAt, TimeSpan allowedWindow)
    {
        if (SenderId != actorId || DeletedAt is not null || deletedAt - SentAt > allowedWindow)
            return false;
        Text = string.Empty;
        DeletedAt = deletedAt;
        return true;
    }
}

public sealed class MessageAttachment
{
    private MessageAttachment(Guid messageId, Guid fileAssetId)
    {
        MessageId = messageId;
        FileAssetId = fileAssetId;
    }
    public Guid MessageId { get; }
    public Guid FileAssetId { get; }
    internal static MessageAttachment Create(Guid messageId, Guid fileAssetId) => new(messageId, fileAssetId);
}
