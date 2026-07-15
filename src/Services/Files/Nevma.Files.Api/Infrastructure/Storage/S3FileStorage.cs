using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using Nevma.Files.Api.Application;

namespace Nevma.Files.Api.Infrastructure.Storage;

public sealed class S3FileStorage(
    IAmazonS3 client,
    IOptions<S3StorageOptions> options) : IFileStorage
{
    private readonly S3StorageOptions _options = options.Value;

    public async Task SaveAsync(
        string storageKey,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        var request = new PutObjectRequest
        {
            BucketName = RequireBucket(),
            Key = Normalize(storageKey),
            InputStream = content,
            AutoCloseStream = false,
            UseChunkEncoding = false
        };
        if (_options.ServerSideEncryption)
            request.ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256;
        await client.PutObjectAsync(request, cancellationToken);
    }

    public async Task<Stream> OpenReadAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        var response = await client.GetObjectAsync(
            RequireBucket(),
            Normalize(storageKey),
            cancellationToken);
        return new OwnedResponseStream(response);
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default) =>
        client.DeleteObjectAsync(RequireBucket(), Normalize(storageKey), cancellationToken);

    public Task<Uri?> CreateReadUrlAsync(
        string storageKey,
        string fileName,
        string contentType,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default)
    {
        var safeFileName = fileName.Replace("\"", string.Empty, StringComparison.Ordinal)
            .Replace("\r", string.Empty, StringComparison.Ordinal)
            .Replace("\n", string.Empty, StringComparison.Ordinal);
        var request = new GetPreSignedUrlRequest
        {
            BucketName = RequireBucket(),
            Key = Normalize(storageKey),
            Verb = HttpVerb.GET,
            Expires = expiresAt.UtcDateTime,
            ResponseHeaderOverrides = new ResponseHeaderOverrides
            {
                ContentType = contentType,
                ContentDisposition = $"attachment; filename=\"{safeFileName}\""
            }
        };
        return Task.FromResult<Uri?>(new Uri(client.GetPreSignedURL(request)));
    }

    private string RequireBucket() =>
        !string.IsNullOrWhiteSpace(_options.BucketName)
            ? _options.BucketName
            : throw new InvalidOperationException("FileStorage:BucketName is required for S3 storage.");

    private static string Normalize(string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey) || storageKey.Contains("..", StringComparison.Ordinal))
            throw new InvalidOperationException("Storage key is invalid.");
        return storageKey.Replace('\\', '/').TrimStart('/');
    }

    private sealed class OwnedResponseStream(GetObjectResponse response) : Stream
    {
        private readonly Stream _inner = response.ResponseStream;
        public override bool CanRead => _inner.CanRead;
        public override bool CanSeek => _inner.CanSeek;
        public override bool CanWrite => false;
        public override long Length => _inner.Length;
        public override long Position { get => _inner.Position; set => _inner.Position = value; }
        public override void Flush() => _inner.Flush();
        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
            _inner.ReadAsync(buffer, cancellationToken);
        public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        protected override void Dispose(bool disposing)
        {
            if (disposing) response.Dispose();
            base.Dispose(disposing);
        }
        public override async ValueTask DisposeAsync()
        {
            response.Dispose();
            await base.DisposeAsync();
        }
    }
}
