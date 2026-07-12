using Nevma.Contracts.Planning;
using Nevma.Planning.Api.Application;

namespace Nevma.Planning.Api.Endpoints;

public static class MeetingInvitationEndpoints
{
    public static IEndpointRouteBuilder MapMeetingInvitationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var invitations = endpoints.MapGroup("/api/meeting-invitations")
            .WithTags("Meeting invitations");

        invitations.MapPost("/", (CreateMeetingInvitationRequest request, PlanningService service) =>
        {
            var result = service.CreateInvitation(request);
            return result.IsSuccess
                ? Results.Created($"/api/meeting-invitations/{result.Invitation!.Id}", result.Invitation)
                : Results.ValidationProblem(result.Errors);
        });

        invitations.MapGet("/{id:guid}", (Guid id, PlanningService service) =>
        {
            var invitation = service.GetInvitation(id);
            return invitation is null ? Results.NotFound() : Results.Ok(invitation);
        });

        invitations.MapPost("/{id:guid}/accept", (Guid id, Guid userId, PlanningService service) =>
            service.AcceptInvitation(id, userId) switch
            {
                AcceptInvitationResult.Accepted accepted => Results.Ok(accepted.Event),
                AcceptInvitationResult.NotFound => Results.NotFound(),
                AcceptInvitationResult.Forbidden => Results.Forbid(),
                AcceptInvitationResult.AlreadyHandled => Results.Conflict(
                    new { message = "Invitation has already been handled." }),
                _ => Results.StatusCode(StatusCodes.Status500InternalServerError)
            });

        return endpoints;
    }
}
