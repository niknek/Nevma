namespace Nevma.Contracts.Notifications;

public sealed record RegisterPushDeviceRequest(
    string DeviceId,
    PushPlatform Platform,
    string Token);

public sealed record PushDeviceResponse(
    Guid Id,
    string DeviceId,
    PushPlatform Platform,
    bool IsActive,
    DateTimeOffset RegisteredAt,
    DateTimeOffset LastSeenAt);

public sealed record NotificationResponse(
    Guid Id,
    string Type,
    string Title,
    string Body,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReadAt);

public enum PushPlatform
{
    Android,
    Ios
}
