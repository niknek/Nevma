using Nevma.Contracts.Planning;
using Nevma.Planning.Api.Domain.Calendar;
using Nevma.Planning.Api.Domain.MeetingInvitations;

namespace Nevma.Planning.Api.Application;

public sealed class PlanningService(
    IPlanningRepository repository,
    IPlanningUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<CreateInvitationResult> CreateInvitationAsync(
        Guid organizerId,
        CreateMeetingInvitationRequest request,
        CancellationToken cancellationToken = default)
    {
        var errors = Validate(organizerId, request, timeProvider.GetUtcNow());
        if (errors.Count > 0)
            return CreateInvitationResult.Failure(errors);

        var invitation = MeetingInvitation.Create(
            organizerId,
            request.InviteeId,
            request.Title,
            request.StartsAt,
            request.Duration,
            request.Location,
            request.Message,
            timeProvider.GetUtcNow());

        await repository.AddInvitationAsync(invitation, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return CreateInvitationResult.Success(ToResponse(invitation));
    }

    public async Task<GetInvitationResult> GetInvitationAsync(
        Guid id,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        var invitation = await repository.GetInvitationAsync(id, cancellationToken);
        if (invitation is null)
            return new GetInvitationResult.NotFound();

        if (invitation.OrganizerId != actorId && invitation.InviteeId != actorId)
            return new GetInvitationResult.Forbidden();

        return new GetInvitationResult.Found(ToResponse(invitation));
    }

    public async Task<AcceptInvitationResult> AcceptInvitationAsync(
        Guid invitationId,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        var invitation = await repository.GetInvitationAsync(invitationId, cancellationToken);
        if (invitation is null)
            return new AcceptInvitationResult.NotFound();

        var outcome = invitation.Accept(actorId, timeProvider.GetUtcNow());
        if (outcome == AcceptOutcome.Forbidden)
            return new AcceptInvitationResult.Forbidden();
        if (outcome == AcceptOutcome.AlreadyHandled)
            return new AcceptInvitationResult.AlreadyHandled();

        var calendarEvent = CalendarEvent.FromAcceptedInvitation(invitation);
        await repository.AddCalendarEventAsync(calendarEvent, cancellationToken);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (PlanningConcurrencyException)
        {
            return new AcceptInvitationResult.AlreadyHandled();
        }

        return new AcceptInvitationResult.Accepted(ToResponse(calendarEvent));
    }

    public async Task<IReadOnlyCollection<CalendarEventResponse>> GetCalendarAsync(
        Guid userId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default)
    {
        var events = await repository.GetCalendarAsync(userId, from, to, cancellationToken);
        return events.Select(ToResponse).ToArray();
    }

    private static Dictionary<string, string[]> Validate(
        Guid organizerId,
        CreateMeetingInvitationRequest request,
        DateTimeOffset now)
    {
        var errors = new Dictionary<string, string[]>();
        if (organizerId == Guid.Empty || request.InviteeId == Guid.Empty)
            errors["participants"] = ["Organizer and invitee are required."];
        if (organizerId == request.InviteeId)
            errors[nameof(request.InviteeId)] = ["Organizer and invitee must be different users."];
        if (string.IsNullOrWhiteSpace(request.Title))
            errors[nameof(request.Title)] = ["Title is required."];
        if (request.Title?.Length > 200)
            errors[nameof(request.Title)] = ["Title cannot exceed 200 characters."];
        if (request.StartsAt <= now)
            errors[nameof(request.StartsAt)] = ["Start time must be in the future."];
        if (request.Duration <= TimeSpan.Zero || request.Duration > TimeSpan.FromDays(1))
            errors[nameof(request.Duration)] = ["Duration must be greater than zero and at most 24 hours."];
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

public abstract record GetInvitationResult
{
    public sealed record Found(MeetingInvitationResponse Invitation) : GetInvitationResult;
    public sealed record NotFound : GetInvitationResult;
    public sealed record Forbidden : GetInvitationResult;
}

public abstract record AcceptInvitationResult
{
    public sealed record Accepted(CalendarEventResponse Event) : AcceptInvitationResult;
    public sealed record NotFound : AcceptInvitationResult;
    public sealed record Forbidden : AcceptInvitationResult;
    public sealed record AlreadyHandled : AcceptInvitationResult;
}
