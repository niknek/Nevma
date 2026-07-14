using Microsoft.EntityFrameworkCore;
using Nevma.Contracts.Integration;
using Nevma.Contracts.Planning;
using Nevma.Messaging.Api.Application.Conversations;
using Nevma.Messaging.Api.Application.Integration;
using Nevma.Messaging.Api.Infrastructure.Conversations;
using Nevma.Messaging.Api.Infrastructure.Inbox;
using Nevma.Messaging.Api.Infrastructure.Persistence;

namespace Nevma.Messaging.Tests;

public sealed class PlanningEventHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 14, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Planning_event_creates_a_conversation_and_is_recorded_once()
    {
        await using var context = CreateContext();
        var realtime = new FakeRealtimePublisher();
        var handler = CreateHandler(context, realtime);
        var integrationEvent = CreateEvent();

        var first = await handler.HandleAsync(integrationEvent);
        var duplicate = await handler.HandleAsync(integrationEvent);

        Assert.Equal(PlanningEventHandleResult.Processed, first);
        Assert.Equal(PlanningEventHandleResult.AlreadyProcessed, duplicate);
        Assert.Equal(1, await context.Conversations.CountAsync());
        Assert.Equal(1, await context.InboxMessages.CountAsync());
        Assert.Single(realtime.Events);
    }

    [Fact]
    public async Task Failed_realtime_delivery_leaves_the_event_available_for_retry()
    {
        await using var context = CreateContext();
        var realtime = new FakeRealtimePublisher { ThrowOnPublish = true };
        var handler = CreateHandler(context, realtime);
        var integrationEvent = CreateEvent();

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.HandleAsync(integrationEvent));
        Assert.Empty(context.InboxMessages);
        Assert.Equal(1, await context.Conversations.CountAsync());

        realtime.ThrowOnPublish = false;
        var retried = await handler.HandleAsync(integrationEvent);

        Assert.Equal(PlanningEventHandleResult.Processed, retried);
        Assert.Equal(1, await context.Conversations.CountAsync());
        Assert.Equal(1, await context.InboxMessages.CountAsync());
    }

    private static PlanningEventHandler CreateHandler(
        MessagingDbContext context,
        IUserRealtimePublisher realtimePublisher)
    {
        var repository = new EfConversationRepository(context);
        var conversationService = new ConversationService(
            repository,
            context,
            new FixedTimeProvider(Now));
        return new PlanningEventHandler(
            conversationService,
            new EfIntegrationEventInbox(context),
            realtimePublisher,
            context,
            new FixedTimeProvider(Now));
    }

    private static MessagingDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MessagingDbContext>()
            .UseInMemoryDatabase($"planning-events-{Guid.NewGuid():N}")
            .Options;
        return new MessagingDbContext(options);
    }

    private static MeetingInvitationChangedIntegrationEvent CreateEvent() =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Coffee",
            Now.AddDays(1),
            TimeSpan.FromHours(1),
            "Athens",
            MeetingInvitationStatus.Pending,
            null,
            null,
            null,
            null,
            Now);

    private sealed class FakeRealtimePublisher : IUserRealtimePublisher
    {
        public bool ThrowOnPublish { get; set; }
        public List<MeetingInvitationChangedIntegrationEvent> Events { get; } = [];

        public Task PublishMeetingInvitationChangedAsync(
            IReadOnlyCollection<Guid> userIds,
            MeetingInvitationChangedIntegrationEvent integrationEvent,
            CancellationToken cancellationToken = default)
        {
            if (ThrowOnPublish)
                throw new InvalidOperationException("Realtime unavailable.");
            Events.Add(integrationEvent);
            return Task.CompletedTask;
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
