namespace Nevma.Contracts.Planning;

public enum MeetingInvitationStatus
{
    Pending,
    Accepted,
    Declined,
    CounterProposed,
    Cancelled
}

public sealed record CreateMeetingInvitationRequest(
    Guid InviteeId,
    string Title,
    DateTimeOffset StartsAt,
    TimeSpan Duration,
    string? Location,
    string? Message);

public sealed record CounterProposeMeetingInvitationRequest(
    DateTimeOffset StartsAt,
    TimeSpan Duration,
    string? Location);

public sealed record MeetingInvitationResponse(
    Guid Id,
    Guid OrganizerId,
    Guid InviteeId,
    string Title,
    DateTimeOffset StartsAt,
    TimeSpan Duration,
    string? Location,
    string? Message,
    MeetingInvitationStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? RespondedAt,
    DateTimeOffset? ProposedStartsAt,
    TimeSpan? ProposedDuration,
    string? ProposedLocation);

public sealed record CalendarEventResponse(
    Guid Id,
    Guid InvitationId,
    string Title,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string? Location,
    IReadOnlyCollection<Guid> ParticipantIds);
