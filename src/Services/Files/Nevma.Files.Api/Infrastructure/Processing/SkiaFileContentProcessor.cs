using Nevma.Files.Api.Application;
using SkiaSharp;

namespace Nevma.Files.Api.Infrastructure.Processing;

public sealed class SkiaFileContentProcessor : IFileContentProcessor
{
    private const int MaximumDimension = 12_000;
    private const long MaximumPixels = 20_000_000;
    private readonly SemaphoreSlim imageSlots = new(2, 2);

    public async Task<FileProcessingResult> ProcessAsync(
        string contentType,
        ReadOnlyMemory<byte> content,
        CancellationToken cancellationToken = default)
    {
        if (!contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            return FileProcessingResult.Success(content.ToArray());

        await imageSlots.WaitAsync(cancellationToken);
        try
        {
            return ProcessImage(contentType, content);
        }
        finally
        {
            imageSlots.Release();
        }
    }

    private static FileProcessingResult ProcessImage(string contentType, ReadOnlyMemory<byte> content)
    {
        using var data = SKData.CreateCopy(content.Span);
        using var codec = SKCodec.Create(data);
        if (codec is null)
            return FileProcessingResult.Invalid("Image data could not be decoded safely.");

        var info = codec.Info;
        if (info.Width <= 0 || info.Height <= 0 ||
            info.Width > MaximumDimension || info.Height > MaximumDimension ||
            (long)info.Width * info.Height > MaximumPixels)
            return FileProcessingResult.Invalid("Image dimensions exceed the safe processing limit.");
        if (codec.FrameCount > 1)
            return FileProcessingResult.Invalid("Animated images are not supported.");

        using var bitmap = SKBitmap.Decode(codec);
        if (bitmap is null)
            return FileProcessingResult.Invalid("Image pixels could not be decoded safely.");
        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(ToFormat(contentType), QualityFor(contentType));
        if (encoded is null)
            return FileProcessingResult.Invalid("Image data could not be normalized.");

        var normalized = encoded.ToArray();
        return normalized.Length > 0 && normalized.LongLength <= FileAssetService.MaxFileSize
            ? FileProcessingResult.Success(normalized)
            : FileProcessingResult.Invalid("The normalized image exceeds the file size limit.");
    }

    private static SKEncodedImageFormat ToFormat(string contentType) =>
        contentType.ToLowerInvariant() switch
        {
            "image/jpeg" => SKEncodedImageFormat.Jpeg,
            "image/png" => SKEncodedImageFormat.Png,
            "image/webp" => SKEncodedImageFormat.Webp,
            _ => throw new InvalidOperationException($"Unsupported image content type '{contentType}'.")
        };

    private static int QualityFor(string contentType) =>
        string.Equals(contentType, "image/png", StringComparison.OrdinalIgnoreCase) ? 100 : 90;
}
