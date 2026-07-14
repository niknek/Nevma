using Microsoft.EntityFrameworkCore;
using Nevma.Files.Api.Application;
using Nevma.Files.Api.Infrastructure.Persistence;

namespace Nevma.Files.Tests;

public sealed class FileAssetServiceTests
{
    [Fact]
    public async Task Valid_pdf_is_hashed_stored_and_owned_by_the_caller()
    {
        await using var context = CreateContext();
        var storage = new MemoryStorage();
        var service = CreateService(context, storage);
        var ownerId = Guid.NewGuid();
        var bytes = "%PDF-1.7\ncontent"u8.ToArray();

        var result = await service.UploadAsync(
            ownerId,
            new FileUpload("report.pdf", "application/pdf", bytes.Length, new MemoryStream(bytes)));

        var uploaded = Assert.IsType<FileUploadResult.Uploaded>(result);
        Assert.Equal(64, uploaded.File.Sha256.Length);
        Assert.Single(storage.Files);
        Assert.True((await context.FileAssets.SingleAsync()).CanRead(ownerId));
    }

    [Fact]
    public async Task Declared_image_with_wrong_signature_is_rejected()
    {
        await using var context = CreateContext();
        var storage = new MemoryStorage();
        var service = CreateService(context, storage);
        var bytes = "not-a-real-png"u8.ToArray();

        var result = await service.UploadAsync(
            Guid.NewGuid(),
            new FileUpload("photo.png", "image/png", bytes.Length, new MemoryStream(bytes)));

        Assert.IsType<FileUploadResult.Invalid>(result);
        Assert.Empty(storage.Files);
    }

    [Fact]
    public async Task Owner_can_grant_download_access()
    {
        await using var context = CreateContext();
        var storage = new MemoryStorage();
        var service = CreateService(context, storage);
        var ownerId = Guid.NewGuid();
        var readerId = Guid.NewGuid();
        var bytes = "%PDF-1.7\ncontent"u8.ToArray();
        var uploaded = Assert.IsType<FileUploadResult.Uploaded>(await service.UploadAsync(
            ownerId,
            new FileUpload("report.pdf", "application/pdf", bytes.Length, new MemoryStream(bytes))));

        Assert.True(await service.GrantAsync(uploaded.File.Id, ownerId, readerId));

        Assert.IsType<FileDownloadResult.Found>(await service.OpenAsync(uploaded.File.Id, readerId));
    }

    private static FileAssetService CreateService(FilesDbContext context, MemoryStorage storage) =>
        new(new EfFileAssetRepository(context), storage, new AlwaysSafeScanner(), context, TimeProvider.System);

    private static FilesDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<FilesDbContext>()
            .UseInMemoryDatabase($"files-{Guid.NewGuid():N}").Options);

    private sealed class AlwaysSafeScanner : IFileScanner
    {
        public Task<FileScanResult> ScanAsync(Stream content, CancellationToken cancellationToken = default) =>
            Task.FromResult(FileScanResult.Safe);
    }

    private sealed class MemoryStorage : IFileStorage
    {
        public Dictionary<string, byte[]> Files { get; } = [];
        public async Task SaveAsync(string storageKey, Stream content, CancellationToken cancellationToken = default)
        {
            using var buffer = new MemoryStream();
            await content.CopyToAsync(buffer, cancellationToken);
            Files[storageKey] = buffer.ToArray();
        }
        public Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default) =>
            Task.FromResult<Stream>(new MemoryStream(Files[storageKey]));
        public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
        {
            Files.Remove(storageKey);
            return Task.CompletedTask;
        }
    }
}
