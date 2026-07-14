using Microsoft.EntityFrameworkCore;
using Nevma.Contracts.Planning;
using Nevma.Planning.Api.Application;
using Nevma.Planning.Api.Domain.MeetingInvitations;
using Nevma.Planning.Api.Infrastructure;
using Nevma.Planning.Api.Infrastructure.Persistence;

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

    private static PlanningService CreateService(PlanningDbContext context) =>
        new(new EfPlanningRepository(context), context, new FixedTimeProvider(Now));

    private static PlanningDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<PlanningDbContext>()
            .UseInMemoryDatabase($"planning-{Guid.NewGuid():N}")
            .Options;

        return new PlanningDbContext(options);
    }

    private static CreateMeetingInvitationRequest CreateRequest(Guid inviteeId) =>
        new(
            inviteeId,
            "Coffee",
            Now.AddDays(1),
            TimeSpan.FromHours(1),
            "Athens",
            null);

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
