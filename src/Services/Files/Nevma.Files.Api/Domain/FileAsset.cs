namespace Nevma.Files.Api.Domain;

public sealed class FileAsset
{
    private readonly List<FileAccessGrant> _accessGrants = [];

    private FileAsset(
        Guid id,
        Guid ownerId,
        string fileName,
        string contentType,
        long size,
        string sha256,
        string storageKey,
        DateTimeOffset createdAt)
    {
        Id = id;
        OwnerId = ownerId;
        FileName = fileName;
        ContentType = contentType;
        Size = size;
        Sha256 = sha256;
        StorageKey = storageKey;
        CreatedAt = createdAt;
    }

    public Guid Id { get; }
    public Guid OwnerId { get; }
    public string FileName { get; }
    public string ContentType { get; }
    public long Size { get; }
    public string Sha256 { get; }
    public string StorageKey { get; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset? DeletedAt { get; private set; }
    public IReadOnlyCollection<FileAccessGrant> AccessGrants => _accessGrants.AsReadOnly();

    public static FileAsset Create(
        Guid ownerId,
        string fileName,
        string contentType,
        long size,
        string sha256,
        string storageKey,
        DateTimeOffset now) =>
        new(Guid.NewGuid(), ownerId, fileName, contentType, size, sha256, storageKey, now);

    public bool CanRead(Guid userId) =>
        DeletedAt is null && (OwnerId == userId || _accessGrants.Any(grant => grant.UserId == userId));

    public bool Grant(Guid ownerId, Guid userId, DateTimeOffset now)
    {
        if (OwnerId != ownerId || userId == OwnerId || userId == Guid.Empty || DeletedAt is not null)
            return false;
        if (_accessGrants.Any(grant => grant.UserId == userId))
            return true;
        _accessGrants.Add(FileAccessGrant.Create(Id, userId, now));
        return true;
    }

    public bool Revoke(Guid ownerId, Guid userId)
    {
        if (OwnerId != ownerId)
            return false;
        var grant = _accessGrants.SingleOrDefault(item => item.UserId == userId);
        return grant is not null && _accessGrants.Remove(grant);
    }

    public bool Delete(Guid ownerId, DateTimeOffset now)
    {
        if (OwnerId != ownerId || DeletedAt is not null)
            return false;
        DeletedAt = now;
        return true;
    }
}

public sealed class FileAccessGrant
{
    private FileAccessGrant(Guid fileAssetId, Guid userId, DateTimeOffset createdAt)
    {
        FileAssetId = fileAssetId;
        UserId = userId;
        CreatedAt = createdAt;
    }

    public Guid FileAssetId { get; }
    public Guid UserId { get; }
    public DateTimeOffset CreatedAt { get; }

    internal static FileAccessGrant Create(Guid fileAssetId, Guid userId, DateTimeOffset now) =>
        new(fileAssetId, userId, now);
}
