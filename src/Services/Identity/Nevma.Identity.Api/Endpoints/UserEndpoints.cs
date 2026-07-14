using System.Security.Claims;
using Nevma.Identity.Api.Application.Users;
using OpenIddict.Abstractions;

namespace Nevma.Identity.Api.Endpoints;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var users = endpoints.MapGroup("/api/users")
            .WithTags("Users")
            .RequireAuthorization();

        users.MapGet("/me", async (
            ClaimsPrincipal principal,
            UserService service,
            CancellationToken cancellationToken) =>
        {
            var subject = principal.FindFirstValue(OpenIddictConstants.Claims.Subject);
            if (!Guid.TryParse(subject, out var userId))
                return Results.Unauthorized();

            var user = await service.GetByIdAsync(userId, cancellationToken);
            return user is null ? Results.NotFound() : Results.Ok(user);
        });

        users.MapGet("/{id:guid}", async (
            Guid id,
            UserService service,
            CancellationToken cancellationToken) =>
        {
            var user = await service.GetByIdAsync(id, cancellationToken);
            return user is null ? Results.NotFound() : Results.Ok(user);
        });

        return endpoints;
    }
}
