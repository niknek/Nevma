namespace Nevma.Identity.Api.Domain.Sessions;

public sealed class DeviceSession
{
    private DeviceSession() { }

    private DeviceSession(
        Guid id,
        Guid userId,
        string deviceId,
        string deviceName,
        string platform,
        string authorizationId,
        DateTimeOffset createdAt)
    {
        Id = id;
        UserId = userId;
        DeviceId = deviceId;
        DeviceName = deviceName;
        Platform = platform;
        AuthorizationId = authorizationId;
        CreatedAt = createdAt;
        LastSeenAt = createdAt;
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string DeviceId { get; private set; } = string.Empty;
    public string DeviceName { get; private set; } = string.Empty;
    public string Platform { get; private set; } = string.Empty;
    public string AuthorizationId { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset LastSeenAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }

    public static DeviceSession Create(
        Guid id,
        Guid userId,
        string deviceId,
        string deviceName,
        string platform,
        string authorizationId,
        DateTimeOffset createdAt) =>
        new(id, userId, deviceId, deviceName, platform, authorizationId, createdAt);

    public void Revoke(DateTimeOffset revokedAt) => RevokedAt ??= revokedAt;
}
