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
        Guid[] participantIds,
        DateTimeOffset updatedAt)
    {
        Id = id;
        InvitationId = invitationId;
        Title = title;
        StartsAt = startsAt;
        EndsAt = endsAt;
        Location = location;
        ParticipantIds = participantIds;
        UpdatedAt = updatedAt;
    }

    public Guid Id { get; }
    public Guid InvitationId { get; }
    public string Title { get; }
    public DateTimeOffset StartsAt { get; private set; }
    public DateTimeOffset EndsAt { get; private set; }
    public string? Location { get; private set; }
    public Guid[] ParticipantIds { get; }
    public CalendarEventStatus Status { get; private set; } = CalendarEventStatus.Confirmed;
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }

    public static CalendarEvent FromAcceptedInvitation(
        MeetingInvitation invitation,
        DateTimeOffset acceptedAt) =>
        new(
            Guid.NewGuid(),
            invitation.Id,
            invitation.Title,
            invitation.StartsAt,
            invitation.StartsAt.Add(invitation.Duration),
            invitation.Location,
            [invitation.OrganizerId, invitation.InviteeId],
            acceptedAt);

    public void Reschedule(MeetingInvitation invitation, DateTimeOffset updatedAt)
    {
        StartsAt = invitation.StartsAt;
        EndsAt = invitation.StartsAt.Add(invitation.Duration);
        Location = invitation.Location;
        UpdatedAt = updatedAt;
    }

    public bool Cancel(DateTimeOffset cancelledAt)
    {
        if (Status == CalendarEventStatus.Cancelled)
            return false;
        Status = CalendarEventStatus.Cancelled;
        CancelledAt = cancelledAt;
        UpdatedAt = cancelledAt;
        return true;
    }
}

public enum CalendarEventStatus
{
    Confirmed,
    Cancelled
}
