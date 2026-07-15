using System.Text;
using Nevma.Files.Api.Infrastructure.Processing;
using SkiaSharp;

namespace Nevma.Files.Tests;

public sealed class SkiaFileContentProcessorTests
{
    [Fact]
    public async Task Jpeg_is_decoded_and_reencoded_without_embedded_metadata()
    {
        var original = CreateJpegWithExifMarker();
        var processor = new SkiaFileContentProcessor();

        var result = await processor.ProcessAsync("image/jpeg", original);

        Assert.True(result.IsSuccess, result.Error);
        Assert.NotNull(result.Content);
        Assert.DoesNotContain("Exif", Encoding.ASCII.GetString(result.Content), StringComparison.Ordinal);
        using var bitmap = SKBitmap.Decode(result.Content);
        Assert.NotNull(bitmap);
        Assert.Equal(2, bitmap.Width);
        Assert.Equal(2, bitmap.Height);
    }

    [Fact]
    public async Task Corrupt_image_is_rejected()
    {
        var processor = new SkiaFileContentProcessor();

        var result = await processor.ProcessAsync("image/png", "not-an-image"u8.ToArray());

        Assert.False(result.IsSuccess);
        Assert.Null(result.Content);
    }

    [Fact]
    public async Task Non_image_content_is_not_modified()
    {
        var original = "%PDF-1.7\ncontent"u8.ToArray();
        var processor = new SkiaFileContentProcessor();

        var result = await processor.ProcessAsync("application/pdf", original);

        Assert.True(result.IsSuccess);
        Assert.Equal(original, result.Content);
    }

    private static byte[] CreateJpegWithExifMarker()
    {
        using var bitmap = new SKBitmap(2, 2);
        bitmap.Erase(SKColors.CornflowerBlue);
        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(SKEncodedImageFormat.Jpeg, 90);
        var jpeg = encoded.ToArray();
        var exif = new byte[]
        {
            0xff, 0xe1, 0x00, 0x10,
            (byte)'E', (byte)'x', (byte)'i', (byte)'f', 0x00, 0x00,
            0x49, 0x49, 0x2a, 0x00, 0x08, 0x00, 0x00, 0x00
        };
        return [.. jpeg.AsSpan(0, 2), .. exif, .. jpeg.AsSpan(2)];
    }
}
