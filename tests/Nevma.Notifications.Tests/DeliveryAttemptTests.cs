using Nevma.Notifications.Api.Domain.Delivery;

namespace Nevma.Notifications.Tests;

public sealed class DeliveryAttemptTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 14, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Retryable_failure_uses_bounded_exponential_backoff()
    {
        var attempt = CreateAttempt();

        attempt.MarkRetryableFailure("Unavailable", Now);
        var firstRetry = attempt.NextAttemptAt;
        attempt.MarkRetryableFailure("Unavailable", firstRetry);

        Assert.Equal(2, attempt.Attempts);
        Assert.Equal(Now.AddSeconds(2), firstRetry);
        Assert.Equal(firstRetry.AddSeconds(4), attempt.NextAttemptAt);
        Assert.Equal(DeliveryStatus.Pending, attempt.Status);
    }

    [Fact]
    public void Successful_delivery_records_only_the_provider_message_id()
    {
        var attempt = CreateAttempt();

        attempt.MarkSent("projects/nevma/messages/123", Now);

        Assert.Equal(DeliveryStatus.Sent, attempt.Status);
        Assert.Equal("projects/nevma/messages/123", attempt.ProviderMessageId);
        Assert.Equal(Now, attempt.CompletedAt);
        Assert.Null(attempt.LastError);
    }

    [Fact]
    public void Permanent_failure_stops_future_retries()
    {
        var attempt = CreateAttempt();

        attempt.MarkPermanentFailure("Unregistered", Now);

        Assert.Equal(DeliveryStatus.PermanentFailure, attempt.Status);
        Assert.Equal("Unregistered", attempt.LastError);
        Assert.Equal(Now, attempt.CompletedAt);
    }

    private static DeliveryAttempt CreateAttempt() =>
        DeliveryAttempt.Create(Guid.NewGuid(), Guid.NewGuid(), Now);
}
