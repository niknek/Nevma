namespace Nevma.Files.Api.Infrastructure.Scanning;

public sealed class ClamAvOptions
{
    public const string SectionName = "MalwareScanning";
    public string Provider { get; init; } = "BuiltIn";
    public string Host { get; init; } = "localhost";
    public int Port { get; init; } = 3310;
    public int TimeoutSeconds { get; init; } = 30;
    public bool FailClosed { get; init; } = true;
}
