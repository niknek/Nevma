using System.Buffers.Binary;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Options;
using Nevma.Files.Api.Application;

namespace Nevma.Files.Api.Infrastructure.Scanning;

public sealed class ClamAvScanner(
    IOptions<ClamAvOptions> options,
    ILogger<ClamAvScanner> logger) : IFileScanner
{
    public async Task<FileScanResult> ScanAsync(
        Stream content,
        CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(settings.TimeoutSeconds, 1, 120)));

        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(settings.Host, settings.Port, timeout.Token);
            await using var network = client.GetStream();
            await network.WriteAsync("zINSTREAM\0"u8.ToArray(), timeout.Token);

            content.Position = 0;
            var buffer = new byte[8192];
            var lengthPrefix = new byte[4];
            int read;
            while ((read = await content.ReadAsync(buffer, timeout.Token)) > 0)
            {
                BinaryPrimitives.WriteUInt32BigEndian(lengthPrefix, (uint)read);
                await network.WriteAsync(lengthPrefix, timeout.Token);
                await network.WriteAsync(buffer.AsMemory(0, read), timeout.Token);
            }
            BinaryPrimitives.WriteUInt32BigEndian(lengthPrefix, 0);
            await network.WriteAsync(lengthPrefix, timeout.Token);
            await network.FlushAsync(timeout.Token);

            var response = await ReadResponseAsync(network, timeout.Token);
            content.Position = 0;
            if (response.EndsWith("OK", StringComparison.Ordinal))
                return FileScanResult.Safe;
            if (response.Contains("FOUND", StringComparison.Ordinal))
                return new FileScanResult(false, ReadThreatName(response));

            logger.LogError("ClamAV returned an unexpected scan response: {Response}", response);
            return settings.FailClosed ? FileScanResult.Unavailable : FileScanResult.Safe;
        }
        catch (Exception exception) when (
            exception is IOException or SocketException ||
            exception is OperationCanceledException && !cancellationToken.IsCancellationRequested)
        {
            content.Position = 0;
            logger.LogError(exception, "ClamAV could not scan an uploaded file.");
            return settings.FailClosed ? FileScanResult.Unavailable : FileScanResult.Safe;
        }
    }

    private static async Task<string> ReadResponseAsync(NetworkStream stream, CancellationToken cancellationToken)
    {
        var bytes = new List<byte>(256);
        var buffer = new byte[256];
        while (bytes.Count <= 4096)
        {
            var read = await stream.ReadAsync(buffer, cancellationToken);
            if (read == 0) break;
            var terminator = Array.IndexOf(buffer, (byte)0, 0, read);
            var count = terminator >= 0 ? terminator : read;
            bytes.AddRange(buffer.AsSpan(0, count).ToArray());
            if (terminator >= 0) break;
        }
        if (bytes.Count > 4096)
            throw new IOException("ClamAV response exceeded the allowed size.");
        return Encoding.UTF8.GetString(bytes.ToArray()).Trim();
    }

    private static string ReadThreatName(string response)
    {
        var colon = response.IndexOf(':');
        var found = response.LastIndexOf("FOUND", StringComparison.Ordinal);
        if (colon < 0 || found <= colon) return "Malware";
        var value = response[(colon + 1)..found].Trim();
        return value.Length is > 0 and <= 200 ? value : "Malware";
    }
}
