namespace Nevma.Contracts.Files;

public sealed record FileAssetResponse(
    Guid Id,
    string FileName,
    string ContentType,
    long Size,
    string Sha256,
    DateTimeOffset CreatedAt);

public sealed record GrantFileAccessRequest(Guid UserId);
public sealed record FileDownloadUrlResponse(Uri Url, DateTimeOffset ExpiresAt);
