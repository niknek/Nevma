using Nevma.Contracts.Planning;

namespace Nevma.Contracts.Integration;

public sealed record MeetingInvitationChangedIntegrationEvent(
    Guid EventId,
    Guid InvitationId,
    Guid OrganizerId,
    Guid InviteeId,
    string Title,
    DateTimeOffset StartsAt,
    TimeSpan Duration,
    string? Location,
    MeetingInvitationStatus Status,
    DateTimeOffset? ProposedStartsAt,
    TimeSpan? ProposedDuration,
    string? ProposedLocation,
    Guid? CalendarEventId,
    DateTimeOffset OccurredAt);

public static class PlanningIntegrationEventTypes
{
    public const string MeetingInvitationChanged = "planning.meeting-invitation.changed.v1";
}
