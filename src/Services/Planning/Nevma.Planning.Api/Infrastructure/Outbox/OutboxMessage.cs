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
    public Guid? LockId { get; private set; }
    public DateTimeOffset? LockedUntil { get; private set; }

    public static OutboxMessage Create(
        Guid id,
        string type,
        string payload,
        DateTimeOffset occurredAt) =>
        new(id, type, payload, occurredAt);

    public void MarkProcessed(DateTimeOffset processedAt)
    {
        ProcessedAt = processedAt;
        LastError = null;
        LockId = null;
        LockedUntil = null;
    }

    public void MarkFailed(string error, DateTimeOffset failedAt)
    {
        Attempts++;
        var delaySeconds = Math.Min(Math.Pow(2, Attempts), 15 * 60);
        NextAttemptAt = failedAt.AddSeconds(delaySeconds);
        LastError = error.Length <= 2_000 ? error : error[..2_000];
        LockId = null;
        LockedUntil = null;
    }
}
