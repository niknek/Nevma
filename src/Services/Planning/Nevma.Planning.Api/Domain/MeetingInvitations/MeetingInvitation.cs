namespace Nevma.Planning.Api.Domain.MeetingInvitations;

public enum InvitationStatus
{
    Pending,
    Accepted,
    Declined,
    CounterProposed,
    RescheduleProposed,
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
    public DateTimeOffset StartsAt { get; private set; }
    public TimeSpan Duration { get; private set; }
    public string? Location { get; private set; }
    public string? Message { get; }
    public InvitationStatus Status { get; private set; } = InvitationStatus.Pending;
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset? RespondedAt { get; private set; }
    public DateTimeOffset? ProposedStartsAt { get; private set; }
    public TimeSpan? ProposedDuration { get; private set; }
    public string? ProposedLocation { get; private set; }
    public DateTimeOffset AcceptanceStartsAt =>
        ProposedStartsAt ?? StartsAt;
    public TimeSpan AcceptanceDuration =>
        ProposedDuration ?? Duration;

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
        var eligibility = CanAccept(userId);
        if (eligibility != AcceptOutcome.Accepted)
            return eligibility;

        if (Status is InvitationStatus.CounterProposed or InvitationStatus.RescheduleProposed)
        {
            StartsAt = ProposedStartsAt!.Value;
            Duration = ProposedDuration!.Value;
            Location = ProposedLocation;
            ProposedStartsAt = null;
            ProposedDuration = null;
            ProposedLocation = null;
        }

        Status = InvitationStatus.Accepted;
        RespondedAt = respondedAt;
        return AcceptOutcome.Accepted;
    }

    public AcceptOutcome CanAccept(Guid userId)
    {
        if (Status is not (
            InvitationStatus.Pending or
            InvitationStatus.CounterProposed or
            InvitationStatus.RescheduleProposed))
            return AcceptOutcome.AlreadyHandled;

        var expectedResponderId = Status == InvitationStatus.CounterProposed ? OrganizerId : InviteeId;
        if (expectedResponderId != userId)
            return AcceptOutcome.Forbidden;
        return AcceptOutcome.Accepted;
    }

    public InvitationDecision Decline(Guid userId, DateTimeOffset respondedAt)
    {
        if (Status == InvitationStatus.RescheduleProposed)
        {
            if (InviteeId != userId)
                return InvitationDecision.Forbidden;

            ClearProposal();
            Status = InvitationStatus.Accepted;
            RespondedAt = respondedAt;
            return InvitationDecision.Success;
        }

        if (Status is not (InvitationStatus.Pending or InvitationStatus.CounterProposed))
            return InvitationDecision.AlreadyHandled;

        var expectedResponderId = Status == InvitationStatus.Pending ? InviteeId : OrganizerId;
        if (expectedResponderId != userId)
            return InvitationDecision.Forbidden;

        Status = InvitationStatus.Declined;
        RespondedAt = respondedAt;
        return InvitationDecision.Success;
    }

    public InvitationDecision CounterPropose(
        Guid userId,
        DateTimeOffset startsAt,
        TimeSpan duration,
        string? location,
        DateTimeOffset respondedAt)
    {
        if (Status != InvitationStatus.Pending)
            return InvitationDecision.AlreadyHandled;
        if (InviteeId != userId)
            return InvitationDecision.Forbidden;

        ProposedStartsAt = startsAt;
        ProposedDuration = duration;
        ProposedLocation = location?.Trim();
        Status = InvitationStatus.CounterProposed;
        RespondedAt = respondedAt;
        return InvitationDecision.Success;
    }

    public InvitationDecision ProposeReschedule(
        Guid userId,
        DateTimeOffset startsAt,
        TimeSpan duration,
        string? location,
        DateTimeOffset respondedAt)
    {
        if (Status != InvitationStatus.Accepted)
            return InvitationDecision.AlreadyHandled;
        if (OrganizerId != userId)
            return InvitationDecision.Forbidden;

        ProposedStartsAt = startsAt;
        ProposedDuration = duration;
        ProposedLocation = location?.Trim();
        Status = InvitationStatus.RescheduleProposed;
        RespondedAt = respondedAt;
        return InvitationDecision.Success;
    }

    public InvitationDecision Cancel(Guid userId, DateTimeOffset respondedAt)
    {
        if (Status is InvitationStatus.Cancelled or InvitationStatus.Declined)
            return InvitationDecision.AlreadyHandled;
        if (OrganizerId != userId)
            return InvitationDecision.Forbidden;

        ClearProposal();
        Status = InvitationStatus.Cancelled;
        RespondedAt = respondedAt;
        return InvitationDecision.Success;
    }

    private void ClearProposal()
    {
        ProposedStartsAt = null;
        ProposedDuration = null;
        ProposedLocation = null;
    }
}

public enum AcceptOutcome
{
    Accepted,
    Forbidden,
    AlreadyHandled
}

public enum InvitationDecision
{
    Success,
    Forbidden,
    AlreadyHandled
}
