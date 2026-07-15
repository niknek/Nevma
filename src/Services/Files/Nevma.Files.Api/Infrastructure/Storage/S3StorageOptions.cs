namespace Nevma.Files.Api.Infrastructure.Storage;

public sealed class S3StorageOptions
{
    public const string SectionName = "FileStorage";
    public string Provider { get; init; } = "Local";
    public string? BucketName { get; init; }
    public string? ServiceUrl { get; init; }
    public string Region { get; init; } = "eu-central-1";
    public string? AccessKey { get; init; }
    public string? SecretKey { get; init; }
    public bool ForcePathStyle { get; init; } = true;
    public bool ServerSideEncryption { get; init; } = true;
    public int SignedUrlLifetimeMinutes { get; init; } = 5;
}
