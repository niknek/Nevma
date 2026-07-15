namespace Nevma.Notifications.Api.Infrastructure.Realtime;

public sealed class NotificationRealtimeOptions
{
    public const string SectionName = "Redis";
    public bool Enabled { get; init; }
    public string? ConnectionString { get; init; }
}
