namespace Nevma.Notifications.Api.Domain.Notifications;

public sealed class Notification
{
    private Notification(
        Guid id,
        Guid userId,
        string type,
        string title,
        string body,
        DateTimeOffset createdAt)
    {
        Id = id;
        UserId = userId;
        Type = type;
        Title = title;
        Body = body;
        CreatedAt = createdAt;
    }

    public Guid Id { get; }
    public Guid UserId { get; }
    public string Type { get; }
    public string Title { get; }
    public string Body { get; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset? ReadAt { get; private set; }

    public static Notification Create(
        Guid userId,
        string type,
        string title,
        string body,
        DateTimeOffset createdAt) =>
        new(Guid.NewGuid(), userId, type, title.Trim(), body.Trim(), createdAt);

    public bool MarkRead(DateTimeOffset readAt)
    {
        if (ReadAt is not null)
            return false;
        ReadAt = readAt;
        return true;
    }
}
