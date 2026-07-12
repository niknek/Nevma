namespace Nevma.Planning.Api.Domain.MeetingInvitations;

public enum InvitationStatus
{
    Pending,
    Accepted,
    Declined,
    CounterProposed,
    Cancelled
}

public sealed class MeetingInvitation
{
    private MeetingInvitation(
        Guid id,
        Guid organizerId,
        Guid inviteeId,
        string title,
        DateTimeOffset startsAt,
        TimeSpan duration,
        string? location,
        string? message,
        DateTimeOffset createdAt)
    {
        Id = id;
        OrganizerId = organizerId;
        InviteeId = inviteeId;
        Title = title;
        StartsAt = startsAt;
        Duration = duration;
        Location = location;
        Message = message;
        CreatedAt = createdAt;
    }

    public Guid Id { get; }
    public Guid OrganizerId { get; }
    public Guid InviteeId { get; }
    public string Title { get; }
    public DateTimeOffset StartsAt { get; }
    public TimeSpan Duration { get; }
    public string? Location { get; }
    public string? Message { get; }
    public InvitationStatus Status { get; private set; } = InvitationStatus.Pending;
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset? RespondedAt { get; private set; }

    public static MeetingInvitation Create(
        Guid organizerId,
        Guid inviteeId,
        string title,
        DateTimeOffset startsAt,
        TimeSpan duration,
        string? location,
        string? message,
        DateTimeOffset createdAt) =>
        new(
            Guid.NewGuid(),
            organizerId,
            inviteeId,
            title.Trim(),
            startsAt,
            duration,
            location?.Trim(),
            message?.Trim(),
            createdAt);

    public AcceptOutcome Accept(Guid userId, DateTimeOffset respondedAt)
    {
        if (InviteeId != userId)
            return AcceptOutcome.Forbidden;
        if (Status != InvitationStatus.Pending)
            return AcceptOutcome.AlreadyHandled;

        Status = InvitationStatus.Accepted;
        RespondedAt = respondedAt;
        return AcceptOutcome.Accepted;
    }
}

public enum AcceptOutcome
{
    Accepted,
    Forbidden,
    AlreadyHandled
}
