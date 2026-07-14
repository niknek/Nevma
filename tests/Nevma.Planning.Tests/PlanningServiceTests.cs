using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Nevma.Contracts.Integration;
using Nevma.Contracts.Planning;
using Nevma.Planning.Api.Application;
using Nevma.Planning.Api.Domain.MeetingInvitations;
using Nevma.Planning.Api.Infrastructure;
using Nevma.Planning.Api.Infrastructure.Persistence;
using Nevma.Planning.Api.Infrastructure.Outbox;

namespace Nevma.Planning.Tests;

public sealed class PlanningServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 14, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Authenticated_user_becomes_the_organizer()
    {
        await using var context = CreateContext();
        var service = CreateService(context);
        var organizerId = Guid.NewGuid();

        var result = await service.CreateInvitationAsync(
            organizerId,
            CreateRequest(Guid.NewGuid()));

        Assert.True(result.IsSuccess);
        Assert.Equal(organizerId, result.Invitation!.OrganizerId);
        Assert.Equal(1, await context.MeetingInvitations.CountAsync());
    }

    [Fact]
    public async Task Invitation_change_is_written_to_the_transactional_outbox()
    {
        await using var context = CreateContext();
        var service = CreateService(context);
        var inviteeId = Guid.NewGuid();

        var created = await service.CreateInvitationAsync(Guid.NewGuid(), CreateRequest(inviteeId));
        await service.AcceptInvitationAsync(created.Invitation!.Id, inviteeId);

        var messages = await context.OutboxMessages.OrderBy(message => message.OccurredAt).ToListAsync();
        Assert.Equal(2, messages.Count);
        Assert.All(messages, message => Assert.Equal(
            PlanningIntegrationEventTypes.MeetingInvitationChanged,
            message.Type));
        using var payload = JsonDocument.Parse(messages[0].Payload);
        Assert.Equal("pending", payload.RootElement.GetProperty("status").GetString());
        Assert.False(payload.RootElement.TryGetProperty("message", out _));
        Assert.Equal(1, await context.CalendarEvents.CountAsync());
    }

    [Fact]
    public async Task Forbidden_invitation_action_does_not_write_an_outbox_event()
    {
        await using var context = CreateContext();
        var service = CreateService(context);
        var created = await service.CreateInvitationAsync(Guid.NewGuid(), CreateRequest(Guid.NewGuid()));

        var result = await service.AcceptInvitationAsync(created.Invitation!.Id, Guid.NewGuid());

        Assert.IsType<AcceptInvitationResult.Forbidden>(result);
        Assert.Equal(1, await context.OutboxMessages.CountAsync());
    }

    [Fact]
    public async Task Only_the_invitee_can_accept_an_invitation()
    {
        await using var context = CreateContext();
        var service = CreateService(context);
        var organizerId = Guid.NewGuid();
        var inviteeId = Guid.NewGuid();
        var created = await service.CreateInvitationAsync(organizerId, CreateRequest(inviteeId));

        var forbidden = await service.AcceptInvitationAsync(created.Invitation!.Id, organizerId);
        var accepted = await service.AcceptInvitationAsync(created.Invitation.Id, inviteeId);

        Assert.IsType<AcceptInvitationResult.Forbidden>(forbidden);
        Assert.IsType<AcceptInvitationResult.Accepted>(accepted);
        Assert.Equal(1, await context.CalendarEvents.CountAsync());
    }

    [Fact]
    public async Task Unrelated_user_cannot_read_an_invitation()
    {
        await using var context = CreateContext();
        var service = CreateService(context);
        var created = await service.CreateInvitationAsync(Guid.NewGuid(), CreateRequest(Guid.NewGuid()));

        var result = await service.GetInvitationAsync(created.Invitation!.Id, Guid.NewGuid());

        Assert.IsType<GetInvitationResult.Forbidden>(result);
    }

    [Fact]
    public async Task Invitation_in_the_past_is_rejected()
    {
        await using var context = CreateContext();
        var service = CreateService(context);
        var request = CreateRequest(Guid.NewGuid()) with { StartsAt = Now.AddMinutes(-1) };

        var result = await service.CreateInvitationAsync(Guid.NewGuid(), request);

        Assert.False(result.IsSuccess);
        Assert.Empty(context.MeetingInvitations);
    }

    [Fact]
    public async Task Invitee_can_counter_and_organizer_can_accept_the_new_time()
    {
        await using var context = CreateContext();
        var service = CreateService(context);
        var organizerId = Guid.NewGuid();
        var inviteeId = Guid.NewGuid();
        var created = await service.CreateInvitationAsync(organizerId, CreateRequest(inviteeId));
        var proposedStart = Now.AddDays(2);

        var countered = await service.CounterProposeInvitationAsync(
            created.Invitation!.Id,
            inviteeId,
            new CounterProposeMeetingInvitationRequest(
                proposedStart,
                TimeSpan.FromMinutes(90),
                "Piraeus"));
        var accepted = await service.AcceptInvitationAsync(created.Invitation.Id, organizerId);

        var counterResponse = Assert.IsType<InvitationActionResult.Updated>(countered);
        Assert.Equal(MeetingInvitationStatus.CounterProposed, counterResponse.Invitation.Status);
        Assert.Equal(proposedStart, counterResponse.Invitation.ProposedStartsAt);
        var acceptedResponse = Assert.IsType<AcceptInvitationResult.Accepted>(accepted);
        Assert.Equal(proposedStart, acceptedResponse.Event.StartsAt);
        Assert.Equal("Piraeus", acceptedResponse.Event.Location);
    }

    [Fact]
    public async Task Invitee_can_decline_a_pending_invitation()
    {
        await using var context = CreateContext();
        var service = CreateService(context);
        var inviteeId = Guid.NewGuid();
        var created = await service.CreateInvitationAsync(Guid.NewGuid(), CreateRequest(inviteeId));

        var declined = await service.DeclineInvitationAsync(created.Invitation!.Id, inviteeId);

        var response = Assert.IsType<InvitationActionResult.Updated>(declined);
        Assert.Equal(MeetingInvitationStatus.Declined, response.Invitation.Status);
        Assert.Empty(context.CalendarEvents);
    }

    [Fact]
    public async Task Organizer_cannot_counter_their_own_pending_invitation()
    {
        await using var context = CreateContext();
        var service = CreateService(context);
        var organizerId = Guid.NewGuid();
        var created = await service.CreateInvitationAsync(organizerId, CreateRequest(Guid.NewGuid()));

        var result = await service.CounterProposeInvitationAsync(
            created.Invitation!.Id,
            organizerId,
            new CounterProposeMeetingInvitationRequest(
                Now.AddDays(2),
                TimeSpan.FromHours(1),
                null));

        Assert.IsType<InvitationActionResult.Forbidden>(result);
    }

    [Fact]
    public async Task Overlapping_meeting_for_a_shared_participant_is_rejected()
    {
        await using var context = CreateContext();
        var service = CreateService(context);
        var sharedParticipant = Guid.NewGuid();
        var first = await service.CreateInvitationAsync(
            Guid.NewGuid(),
            CreateRequest(sharedParticipant));
        await service.AcceptInvitationAsync(first.Invitation!.Id, sharedParticipant);
        var overlapping = await service.CreateInvitationAsync(
            Guid.NewGuid(),
            CreateRequest(sharedParticipant, Now.AddDays(1).AddMinutes(30)));

        var result = await service.AcceptInvitationAsync(
            overlapping.Invitation!.Id,
            sharedParticipant);

        Assert.IsType<AcceptInvitationResult.CalendarConflict>(result);
        Assert.Equal(1, await context.CalendarEvents.CountAsync());
    }

    [Fact]
    public async Task Adjacent_meetings_do_not_conflict()
    {
        await using var context = CreateContext();
        var service = CreateService(context);
        var sharedParticipant = Guid.NewGuid();
        var first = await service.CreateInvitationAsync(
            Guid.NewGuid(),
            CreateRequest(sharedParticipant));
        await service.AcceptInvitationAsync(first.Invitation!.Id, sharedParticipant);
        var adjacent = await service.CreateInvitationAsync(
            Guid.NewGuid(),
            CreateRequest(sharedParticipant, Now.AddDays(1).AddHours(1)));

        var result = await service.AcceptInvitationAsync(adjacent.Invitation!.Id, sharedParticipant);

        Assert.IsType<AcceptInvitationResult.Accepted>(result);
        Assert.Equal(2, await context.CalendarEvents.CountAsync());
    }

    [Fact]
    public async Task Accepted_reschedule_updates_the_existing_calendar_event()
    {
        await using var context = CreateContext();
        var service = CreateService(context);
        var organizerId = Guid.NewGuid();
        var inviteeId = Guid.NewGuid();
        var created = await service.CreateInvitationAsync(organizerId, CreateRequest(inviteeId));
        await service.AcceptInvitationAsync(created.Invitation!.Id, inviteeId);
        var newStart = Now.AddDays(3);

        var proposed = await service.ProposeRescheduleAsync(
            created.Invitation.Id,
            organizerId,
            new RescheduleMeetingRequest(newStart, TimeSpan.FromHours(2), "Thessaloniki"));
        var accepted = await service.AcceptInvitationAsync(created.Invitation.Id, inviteeId);

        var proposal = Assert.IsType<InvitationActionResult.Updated>(proposed);
        Assert.Equal(MeetingInvitationStatus.RescheduleProposed, proposal.Invitation.Status);
        var response = Assert.IsType<AcceptInvitationResult.Accepted>(accepted);
        Assert.Equal(newStart, response.Event.StartsAt);
        Assert.Equal("Thessaloniki", response.Event.Location);
        Assert.Equal(1, await context.CalendarEvents.CountAsync());
    }

    [Fact]
    public async Task Cancelling_an_accepted_meeting_removes_it_from_the_active_calendar()
    {
        await using var context = CreateContext();
        var service = CreateService(context);
        var organizerId = Guid.NewGuid();
        var inviteeId = Guid.NewGuid();
        var created = await service.CreateInvitationAsync(organizerId, CreateRequest(inviteeId));
        await service.AcceptInvitationAsync(created.Invitation!.Id, inviteeId);

        var cancelled = await service.CancelInvitationAsync(created.Invitation.Id, organizerId);
        var calendar = await service.GetCalendarAsync(
            inviteeId,
            Now,
            Now.AddDays(30));

        var response = Assert.IsType<InvitationActionResult.Updated>(cancelled);
        Assert.Equal(MeetingInvitationStatus.Cancelled, response.Invitation.Status);
        Assert.Empty(calendar);
        var storedEvent = await context.CalendarEvents.SingleAsync();
        Assert.Equal(Nevma.Planning.Api.Domain.Calendar.CalendarEventStatus.Cancelled, storedEvent.Status);
        Assert.NotNull(storedEvent.CancelledAt);
    }

    [Fact]
    public async Task Invitee_cannot_cancel_the_organizers_meeting()
    {
        await using var context = CreateContext();
        var service = CreateService(context);
        var organizerId = Guid.NewGuid();
        var inviteeId = Guid.NewGuid();
        var created = await service.CreateInvitationAsync(organizerId, CreateRequest(inviteeId));
        await service.AcceptInvitationAsync(created.Invitation!.Id, inviteeId);

        var result = await service.CancelInvitationAsync(created.Invitation.Id, inviteeId);

        Assert.IsType<InvitationActionResult.Forbidden>(result);
        Assert.Equal(
            Nevma.Planning.Api.Domain.Calendar.CalendarEventStatus.Confirmed,
            (await context.CalendarEvents.SingleAsync()).Status);
    }

    [Fact]
    public async Task Declined_reschedule_keeps_the_original_calendar_event()
    {
        await using var context = CreateContext();
        var service = CreateService(context);
        var organizerId = Guid.NewGuid();
        var inviteeId = Guid.NewGuid();
        var originalStart = Now.AddDays(1);
        var created = await service.CreateInvitationAsync(organizerId, CreateRequest(inviteeId, originalStart));
        await service.AcceptInvitationAsync(created.Invitation!.Id, inviteeId);
        await service.ProposeRescheduleAsync(
            created.Invitation.Id,
            organizerId,
            new RescheduleMeetingRequest(Now.AddDays(5), TimeSpan.FromHours(1), null));

        var result = await service.DeclineInvitationAsync(created.Invitation.Id, inviteeId);

        var declined = Assert.IsType<InvitationActionResult.Updated>(result);
        Assert.Equal(MeetingInvitationStatus.Accepted, declined.Invitation.Status);
        Assert.Equal(originalStart, (await context.CalendarEvents.SingleAsync()).StartsAt);
    }

    private static PlanningService CreateService(PlanningDbContext context) =>
        new(
            new EfPlanningRepository(context),
            context,
            new EfPlanningEventOutbox(context),
            new FixedTimeProvider(Now));

    private static PlanningDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<PlanningDbContext>()
            .UseInMemoryDatabase($"planning-{Guid.NewGuid():N}")
            .Options;

        return new PlanningDbContext(options);
    }

    private static CreateMeetingInvitationRequest CreateRequest(
        Guid inviteeId,
        DateTimeOffset? startsAt = null) =>
        new(
            inviteeId,
            "Coffee",
            startsAt ?? Now.AddDays(1),
            TimeSpan.FromHours(1),
            "Athens",
            null);

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
