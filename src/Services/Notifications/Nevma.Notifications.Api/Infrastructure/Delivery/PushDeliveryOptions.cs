namespace Nevma.Notifications.Api.Infrastructure.Delivery;

public sealed class PushDeliveryOptions
{
    public const string SectionName = "PushDelivery";

    public bool Enabled { get; init; }
    public string? ProjectId { get; init; }
}
