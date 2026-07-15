namespace Nevma.Identity.Api.Domain.Security;

public sealed class SecurityEvent
{
    private SecurityEvent() { }

    private SecurityEvent(
        Guid id,
        Guid userId,
        string eventType,
        bool succeeded,
        string? ipAddress,
        string? userAgent,
        string? correlationId,
        DateTimeOffset occurredAt)
    {
        Id = id;
        UserId = userId;
        EventType = eventType;
        Succeeded = succeeded;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        CorrelationId = correlationId;
        OccurredAt = occurredAt;
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public bool Succeeded { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public string? CorrelationId { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }

    public static SecurityEvent Create(
        Guid userId,
        string eventType,
        bool succeeded,
        string? ipAddress,
        string? userAgent,
        string? correlationId,
        DateTimeOffset occurredAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        return new SecurityEvent(
            Guid.NewGuid(),
            userId,
            eventType.Trim(),
            succeeded,
            Normalize(ipAddress, 45),
            Normalize(userAgent, 256),
            Normalize(correlationId, 64),
            occurredAt);
    }

    private static string? Normalize(string? value, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var normalized = value.Trim();
        return normalized.Length <= maximumLength ? normalized : normalized[..maximumLength];
    }
}
