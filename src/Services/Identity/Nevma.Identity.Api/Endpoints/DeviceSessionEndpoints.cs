using System.Security.Claims;
using Nevma.Identity.Api.Application.Sessions;

namespace Nevma.Identity.Api.Endpoints;

public static class DeviceSessionEndpoints
{
    public static IEndpointRouteBuilder MapDeviceSessionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var sessions = endpoints.MapGroup("/api/auth/sessions")
            .RequireAuthorization()
            .WithTags("Device sessions");

        sessions.MapGet("/", async (
            ClaimsPrincipal principal,
            DeviceSessionService service,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(principal, out var userId))
                return Results.Unauthorized();
            var currentSessionId = Guid.TryParse(
                principal.FindFirstValue("session_id"),
                out var parsedSessionId)
                    ? parsedSessionId
                    : (Guid?)null;
            return Results.Ok(await service.ListAsync(userId, currentSessionId, cancellationToken));
        });

        sessions.MapDelete("/{id:guid}", async (
            Guid id,
            ClaimsPrincipal principal,
            DeviceSessionService service,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(principal, out var userId))
                return Results.Unauthorized();
            return await service.RevokeAsync(id, userId, cancellationToken)
                ? Results.NoContent()
                : Results.NotFound();
        });

        return endpoints;
    }

    private static bool TryGetUserId(ClaimsPrincipal principal, out Guid userId) =>
        Guid.TryParse(principal.FindFirstValue("sub"), out userId);
}
