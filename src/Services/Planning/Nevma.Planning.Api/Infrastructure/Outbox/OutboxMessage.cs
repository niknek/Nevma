namespace Nevma.Planning.Api.Infrastructure.Outbox;

public sealed class OutboxMessage
{
    private OutboxMessage(
        Guid id,
        string type,
        string payload,
        DateTimeOffset occurredAt)
    {
        Id = id;
        Type = type;
        Payload = payload;
        OccurredAt = occurredAt;
        NextAttemptAt = occurredAt;
    }

    public Guid Id { get; }
    public string Type { get; }
    public string Payload { get; }
    public DateTimeOffset OccurredAt { get; }
    public int Attempts { get; private set; }
    public DateTimeOffset NextAttemptAt { get; private set; }
    public DateTimeOffset? ProcessedAt { get; private set; }
    public string? LastError { get; private set; }

    public static OutboxMessage Create(
        Guid id,
        string type,
        string payload,
        DateTimeOffset occurredAt) =>
        new(id, type, payload, occurredAt);
}
