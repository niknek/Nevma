namespace Nevma.Commands.Api.Infrastructure.Ai;

public sealed class AiProviderOptions
{
    public const string SectionName = "CommandAI";
    public bool Enabled { get; init; }
    public string? Endpoint { get; init; }
    public string? ApiKey { get; init; }
}
