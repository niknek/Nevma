namespace Nevma.Identity.Api.Domain.Users;

public sealed class UserSettings
{
    private UserSettings(
        Guid userId,
        string timeZoneId,
        string locale,
        bool allowPresence,
        DateTimeOffset updatedAt)
    {
        UserId = userId;
        TimeZoneId = timeZoneId;
        Locale = locale;
        AllowPresence = allowPresence;
        UpdatedAt = updatedAt;
    }

    public Guid UserId { get; }
    public string TimeZoneId { get; private set; }
    public string Locale { get; private set; }
    public bool AllowPresence { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static UserSettings CreateDefault(Guid userId, DateTimeOffset now) =>
        new(userId, "UTC", "en", true, now);

    public void Update(string timeZoneId, string locale, bool allowPresence, DateTimeOffset now)
    {
        TimeZoneId = timeZoneId.Trim();
        Locale = locale.Trim();
        AllowPresence = allowPresence;
        UpdatedAt = now;
    }
}
