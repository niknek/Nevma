using Nevma.Planning.Api.Domain.MeetingInvitations;

namespace Nevma.Planning.Api.Domain.Calendar;

public sealed class CalendarEvent
{
    private CalendarEvent(
        Guid id,
        Guid invitationId,
        string title,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        string? location,
        IReadOnlyCollection<Guid> participantIds)
    {
        Id = id;
        InvitationId = invitationId;
        Title = title;
        StartsAt = startsAt;
        EndsAt = endsAt;
        Location = location;
        ParticipantIds = participantIds;
    }

    public Guid Id { get; }
    public Guid InvitationId { get; }
    public string Title { get; }
    public DateTimeOffset StartsAt { get; }
    public DateTimeOffset EndsAt { get; }
    public string? Location { get; }
    public IReadOnlyCollection<Guid> ParticipantIds { get; }

    public static CalendarEvent FromAcceptedInvitation(MeetingInvitation invitation) =>
        new(
            Guid.NewGuid(),
            invitation.Id,
            invitation.Title,
            invitation.StartsAt,
            invitation.StartsAt.Add(invitation.Duration),
            invitation.Location,
            [invitation.OrganizerId, invitation.InviteeId]);
}
