namespace Nevma.Contracts.Identity;

public sealed record SecurityEventResponse(
    Guid Id,
    string EventType,
    bool Succeeded,
    string? IpAddress,
    string? UserAgent,
    DateTimeOffset OccurredAt);
