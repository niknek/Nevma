namespace Nevma.Contracts.Identity;

public sealed record DeviceSessionResponse(
    Guid Id,
    string DeviceId,
    string DeviceName,
    string Platform,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastSeenAt,
    DateTimeOffset? RevokedAt,
    bool IsCurrent);
