using System.Security.Claims;
using Nevma.Identity.Api.Application.Security;
using OpenIddict.Abstractions;

namespace Nevma.Identity.Api.Endpoints;

public static class SecurityEventEndpoints
{
    public static IEndpointRouteBuilder MapSecurityEventEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/auth/security-events", async (
            int? limit,
            ClaimsPrincipal principal,
            HttpResponse response,
            SecurityEventService service,
            CancellationToken cancellationToken) =>
        {
            response.Headers.CacheControl = "no-store";
            if (!Guid.TryParse(
                    principal.FindFirstValue(OpenIddictConstants.Claims.Subject),
                    out var userId))
                return Results.Unauthorized();

            return Results.Ok(await service.ListAsync(userId, limit ?? 50, cancellationToken));
        })
        .RequireAuthorization()
        .WithTags("Account security");

        return endpoints;
    }

    internal static Task RecordFromRequestAsync(
        this SecurityEventService service,
        Guid userId,
        string eventType,
        bool succeeded,
        HttpContext context) =>
        service.RecordAsync(
            userId,
            eventType,
            succeeded,
            context.Connection.RemoteIpAddress?.ToString(),
            context.Request.Headers.UserAgent.ToString(),
            context.TraceIdentifier,
            context.RequestAborted);
}
