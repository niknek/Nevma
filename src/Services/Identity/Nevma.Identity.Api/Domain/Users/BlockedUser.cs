namespace Nevma.Identity.Api.Domain.Users;

public sealed class BlockedUser
{
    private BlockedUser(Guid blockerId, Guid blockedId, DateTimeOffset createdAt)
    {
        BlockerId = blockerId;
        BlockedId = blockedId;
        CreatedAt = createdAt;
    }

    public Guid BlockerId { get; }
    public Guid BlockedId { get; }
    public DateTimeOffset CreatedAt { get; }

    public static BlockedUser Create(Guid blockerId, Guid blockedId, DateTimeOffset now) =>
        blockerId == blockedId
            ? throw new ArgumentException("A user cannot block themselves.", nameof(blockedId))
            : new BlockedUser(blockerId, blockedId, now);
}
