using System.Security.Claims;
using Nevma.Contracts.Planning;
using Nevma.Planning.Api.Application;

namespace Nevma.Planning.Api.Endpoints;

public static class MeetingInvitationEndpoints
{
    public static IEndpointRouteBuilder MapMeetingInvitationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var invitations = endpoints.MapGroup("/api/meeting-invitations")
            .RequireAuthorization()
            .WithTags("Meeting invitations");

        invitations.MapPost("/", async (
            CreateMeetingInvitationRequest request,
            ClaimsPrincipal principal,
            PlanningService service,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(principal, out var organizerId))
                return Results.Unauthorized();

            var result = await service.CreateInvitationAsync(organizerId, request, cancellationToken);
            return result.IsSuccess
                ? Results.Created($"/api/meeting-invitations/{result.Invitation!.Id}", result.Invitation)
                : Results.ValidationProblem(result.Errors);
        });

        invitations.MapGet("/{id:guid}", async (
            Guid id,
            ClaimsPrincipal principal,
            PlanningService service,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(principal, out var actorId))
                return Results.Unauthorized();

            return await service.GetInvitationAsync(id, actorId, cancellationToken) switch
            {
                GetInvitationResult.Found found => Results.Ok(found.Invitation),
                GetInvitationResult.NotFound => Results.NotFound(),
                GetInvitationResult.Forbidden => Results.Forbid(),
                _ => Results.StatusCode(StatusCodes.Status500InternalServerError)
            };
        });

        invitations.MapPost("/{id:guid}/accept", async (
            Guid id,
            ClaimsPrincipal principal,
            PlanningService service,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(principal, out var actorId))
                return Results.Unauthorized();

            return await service.AcceptInvitationAsync(id, actorId, cancellationToken) switch
            {
                AcceptInvitationResult.Accepted accepted => Results.Ok(accepted.Event),
                AcceptInvitationResult.NotFound => Results.NotFound(),
                AcceptInvitationResult.Forbidden => Results.Forbid(),
                AcceptInvitationResult.AlreadyHandled => Results.Conflict(
                    new { message = "Invitation has already been handled." }),
                _ => Results.StatusCode(StatusCodes.Status500InternalServerError)
            };
        });

        invitations.MapPost("/{id:guid}/decline", async (
            Guid id,
            ClaimsPrincipal principal,
            PlanningService service,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(principal, out var actorId))
                return Results.Unauthorized();

            return ToResult(await service.DeclineInvitationAsync(id, actorId, cancellationToken));
        });

        invitations.MapPost("/{id:guid}/counter-propose", async (
            Guid id,
            CounterProposeMeetingInvitationRequest request,
            ClaimsPrincipal principal,
            PlanningService service,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(principal, out var actorId))
                return Results.Unauthorized();

            return ToResult(await service.CounterProposeInvitationAsync(
                id,
                actorId,
                request,
                cancellationToken));
        });

        return endpoints;
    }

    private static bool TryGetUserId(ClaimsPrincipal principal, out Guid userId) =>
        Guid.TryParse(principal.FindFirstValue("sub"), out userId);

    private static IResult ToResult(InvitationActionResult result) =>
        result switch
        {
            InvitationActionResult.Updated updated => Results.Ok(updated.Invitation),
            InvitationActionResult.NotFound => Results.NotFound(),
            InvitationActionResult.Forbidden => Results.Forbid(),
            InvitationActionResult.AlreadyHandled => Results.Conflict(
                new { message = "Invitation has already been handled." }),
            InvitationActionResult.ValidationFailed validation => Results.ValidationProblem(validation.Errors),
            _ => Results.StatusCode(StatusCodes.Status500InternalServerError)
        };
}
