using Nevma.Contracts.Planning;
using Nevma.Planning.Api.Domain.Calendar;
using Nevma.Planning.Api.Domain.MeetingInvitations;

namespace Nevma.Planning.Api.Application;

public sealed class PlanningService(IPlanningRepository repository, TimeProvider timeProvider)
{
    private readonly Lock _sync = new();

    public CreateInvitationResult CreateInvitation(CreateMeetingInvitationRequest request)
    {
        var errors = Validate(request);
        if (errors.Count > 0)
            return CreateInvitationResult.Failure(errors);

        var invitation = MeetingInvitation.Create(
            request.OrganizerId,
            request.InviteeId,
            request.Title,
            request.StartsAt,
            request.Duration,
            request.Location,
            request.Message,
            timeProvider.GetUtcNow());

        repository.AddInvitation(invitation);
        return CreateInvitationResult.Success(ToResponse(invitation));
    }

    public MeetingInvitationResponse? GetInvitation(Guid id)
    {
        var invitation = repository.GetInvitation(id);
        return invitation is null ? null : ToResponse(invitation);
    }

    public AcceptInvitationResult AcceptInvitation(Guid invitationId, Guid userId)
    {
        lock (_sync)
        {
            var invitation = repository.GetInvitation(invitationId);
            if (invitation is null)
                return new AcceptInvitationResult.NotFound();

            var outcome = invitation.Accept(userId, timeProvider.GetUtcNow());
            if (outcome == AcceptOutcome.Forbidden)
                return new AcceptInvitationResult.Forbidden();
            if (outcome == AcceptOutcome.AlreadyHandled)
                return new AcceptInvitationResult.AlreadyHandled();

            var calendarEvent = CalendarEvent.FromAcceptedInvitation(invitation);
            repository.AddCalendarEvent(calendarEvent);
            return new AcceptInvitationResult.Accepted(ToResponse(calendarEvent));
        }
    }

    public IReadOnlyCollection<CalendarEventResponse> GetCalendar(
        Guid userId,
        DateTimeOffset from,
        DateTimeOffset to) =>
        repository.GetCalendar(userId, from, to).Select(ToResponse).ToArray();

    private static Dictionary<string, string[]> Validate(CreateMeetingInvitationRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (request.OrganizerId == Guid.Empty || request.InviteeId == Guid.Empty)
            errors["participants"] = ["Organizer and invitee are required."];
        if (request.OrganizerId == request.InviteeId)
            errors[nameof(request.InviteeId)] = ["Organizer and invitee must be different users."];
        if (string.IsNullOrWhiteSpace(request.Title))
            errors[nameof(request.Title)] = ["Title is required."];
        if (request.Duration <= TimeSpan.Zero || request.Duration > TimeSpan.FromDays(1))
            errors[nameof(request.Duration)] = ["Duration must be between zero and 24 hours."];
        return errors;
    }

    private static MeetingInvitationResponse ToResponse(MeetingInvitation invitation) =>
        new(
            invitation.Id,
            invitation.OrganizerId,
            invitation.InviteeId,
            invitation.Title,
            invitation.StartsAt,
            invitation.Duration,
            invitation.Location,
            invitation.Message,
            (MeetingInvitationStatus)invitation.Status,
            invitation.CreatedAt,
            invitation.RespondedAt);

    private static CalendarEventResponse ToResponse(CalendarEvent calendarEvent) =>
        new(
            calendarEvent.Id,
            calendarEvent.InvitationId,
            calendarEvent.Title,
            calendarEvent.StartsAt,
            calendarEvent.EndsAt,
            calendarEvent.Location,
            calendarEvent.ParticipantIds);
}

public sealed record CreateInvitationResult(
    MeetingInvitationResponse? Invitation,
    IReadOnlyDictionary<string, string[]> Errors)
{
    public bool IsSuccess => Invitation is not null;

    public static CreateInvitationResult Success(MeetingInvitationResponse invitation) =>
        new(invitation, new Dictionary<string, string[]>());

    public static CreateInvitationResult Failure(IReadOnlyDictionary<string, string[]> errors) =>
        new(null, errors);
}

public abstract record AcceptInvitationResult
{
    public sealed record Accepted(CalendarEventResponse Event) : AcceptInvitationResult;
    public sealed record NotFound : AcceptInvitationResult;
    public sealed record Forbidden : AcceptInvitationResult;
    public sealed record AlreadyHandled : AcceptInvitationResult;
}
