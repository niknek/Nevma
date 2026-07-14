using System.Security.Claims;
using Nevma.Contracts.Notifications;
using Nevma.Notifications.Api.Application.PushDevices;

namespace Nevma.Notifications.Api.Endpoints;

public static class PushDeviceEndpoints
{
    public static IEndpointRouteBuilder MapPushDeviceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var devices = endpoints.MapGroup("/api/push-devices")
            .RequireAuthorization()
            .WithTags("Push devices");

        devices.MapPost("/", async (
            RegisterPushDeviceRequest request,
            ClaimsPrincipal principal,
            PushDeviceService service,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(principal, out var userId))
                return Results.Unauthorized();
            var result = await service.RegisterAsync(userId, request, cancellationToken);
            return result.IsSuccess
                ? Results.Ok(result.Device)
                : Results.ValidationProblem(result.Errors);
        });

        devices.MapDelete("/{id:guid}", async (
            Guid id,
            ClaimsPrincipal principal,
            PushDeviceService service,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(principal, out var userId))
                return Results.Unauthorized();
            return await service.RevokeAsync(id, userId, cancellationToken) switch
            {
                RevokePushDeviceResult.Revoked => Results.NoContent(),
                RevokePushDeviceResult.NotFound => Results.NotFound(),
                _ => Results.StatusCode(StatusCodes.Status500InternalServerError)
            };
        });

        return endpoints;
    }

    private static bool TryGetUserId(ClaimsPrincipal principal, out Guid userId) =>
        Guid.TryParse(principal.FindFirstValue("sub"), out userId);
}
