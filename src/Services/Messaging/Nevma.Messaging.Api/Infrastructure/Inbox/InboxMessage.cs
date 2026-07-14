namespace Nevma.Messaging.Api.Infrastructure.Inbox;

public sealed class InboxMessage
{
    private InboxMessage(Guid id, string type, DateTimeOffset processedAt)
    {
        Id = id;
        Type = type;
        ProcessedAt = processedAt;
    }

    public Guid Id { get; }
    public string Type { get; }
    public DateTimeOffset ProcessedAt { get; }

    public static InboxMessage Create(Guid id, string type, DateTimeOffset processedAt) =>
        new(id, type, processedAt);
}
