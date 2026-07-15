using Nevma.Files.Api.Domain;

namespace Nevma.Files.Api.Application;

public interface IFileAssetRepository
{
    Task AddAsync(FileAsset asset, CancellationToken cancellationToken = default);
    Task<FileAsset?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FileAsset>> ListAsync(Guid userId, CancellationToken cancellationToken = default);
}

public interface IFilesUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface IFileStorage
{
    Task SaveAsync(string storageKey, Stream content, CancellationToken cancellationToken = default);
    Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default);
    Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default);
    Task<Uri?> CreateReadUrlAsync(
        string storageKey,
        string fileName,
        string contentType,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default);
}

public interface IFileScanner
{
    Task<FileScanResult> ScanAsync(Stream content, CancellationToken cancellationToken = default);
}

public interface IFileContentProcessor
{
    Task<FileProcessingResult> ProcessAsync(
        string contentType,
        ReadOnlyMemory<byte> content,
        CancellationToken cancellationToken = default);
}

public sealed record FileProcessingResult(byte[]? Content, string? Error)
{
    public bool IsSuccess => Content is not null && Error is null;

    public static FileProcessingResult Success(byte[] content) => new(content, null);
    public static FileProcessingResult Invalid(string error) => new(null, error);
}

public sealed record FileScanResult(bool IsSafe, string? ThreatName, bool IsAvailable = true)
{
    public static FileScanResult Safe { get; } = new(true, null);
    public static FileScanResult Unavailable { get; } = new(false, null, false);
}

public sealed record FileUpload(string FileName, string ContentType, long Length, Stream Content);
