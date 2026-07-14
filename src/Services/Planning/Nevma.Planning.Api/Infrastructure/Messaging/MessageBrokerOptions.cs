namespace Nevma.Planning.Api.Infrastructure.Messaging;

public sealed class MessageBrokerOptions
{
    public const string SectionName = "MessageBroker";

    public bool Enabled { get; init; }
    public string? Uri { get; init; }
    public string Exchange { get; init; } = "nevma.events";
}
