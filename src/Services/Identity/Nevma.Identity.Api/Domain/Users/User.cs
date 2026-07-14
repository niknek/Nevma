namespace Nevma.Identity.Api.Domain.Users;

public sealed class User
{
    private User(Guid id, string displayName, string? avatarUrl, DateTimeOffset createdAt)
    {
        Id = id;
        DisplayName = displayName;
        AvatarUrl = avatarUrl;
        CreatedAt = createdAt;
    }

    public Guid Id { get; }
    public string DisplayName { get; }
    public string? AvatarUrl { get; }
    public DateTimeOffset CreatedAt { get; }

    public static User Create(Guid id, string displayName, string? avatarUrl, DateTimeOffset createdAt) =>
        new(id, displayName.Trim(), avatarUrl?.Trim(), createdAt);
}
