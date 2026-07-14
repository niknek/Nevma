using System.Text;
using Nevma.Files.Api.Application;

namespace Nevma.Files.Api.Infrastructure.Scanning;

public sealed class SafeContentScanner : IFileScanner
{
    private static readonly byte[] EicarMarker = Encoding.ASCII.GetBytes("EICAR-STANDARD-ANTIVIRUS-TEST-FILE");

    public async Task<FileScanResult> ScanAsync(Stream content, CancellationToken cancellationToken = default)
    {
        using var buffer = new MemoryStream();
        content.Position = 0;
        await content.CopyToAsync(buffer, cancellationToken);
        var bytes = buffer.GetBuffer().AsSpan(0, (int)buffer.Length);
        content.Position = 0;
        if (bytes.StartsWith("MZ"u8) || bytes.IndexOf(EicarMarker) >= 0)
            return new FileScanResult(false, "UnsafeContent");
        return FileScanResult.Safe;
    }
}
