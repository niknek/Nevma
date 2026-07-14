using Nevma.Planning.Api.Infrastructure.Outbox;

namespace Nevma.Planning.Tests;

public sealed class OutboxMessageTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 14, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Failed_delivery_is_scheduled_with_exponential_backoff()
    {
        var message = OutboxMessage.Create(Guid.NewGuid(), "test.event.v1", "{}", Now);

        message.MarkFailed("BrokerUnreachableException", Now);
        var firstRetry = message.NextAttemptAt;
        message.MarkFailed("BrokerUnreachableException", firstRetry);

        Assert.Equal(2, message.Attempts);
        Assert.Equal(Now.AddSeconds(2), firstRetry);
        Assert.Equal(firstRetry.AddSeconds(4), message.NextAttemptAt);
        Assert.Null(message.ProcessedAt);
    }

    [Fact]
    public void Successful_delivery_clears_the_previous_error()
    {
        var message = OutboxMessage.Create(Guid.NewGuid(), "test.event.v1", "{}", Now);
        message.MarkFailed("AlreadyClosedException", Now);

        message.MarkProcessed(Now.AddMinutes(1));

        Assert.Equal(Now.AddMinutes(1), message.ProcessedAt);
        Assert.Null(message.LastError);
    }
}
