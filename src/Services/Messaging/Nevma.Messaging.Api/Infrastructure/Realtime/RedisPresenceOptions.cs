namespace Nevma.Messaging.Api.Infrastructure.Realtime;

public sealed class RedisPresenceOptions
{
    public const string SectionName = "Redis";
    public bool Enabled { get; init; }
    public string? ConnectionString { get; init; }
    public int PresenceTtlSeconds { get; init; } = 90;
    public int LastSeenTtlDays { get; init; } = 30;
}
