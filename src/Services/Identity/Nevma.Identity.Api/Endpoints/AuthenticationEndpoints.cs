using System.Security.Claims;
using Nevma.Contracts.Identity;
using Nevma.Identity.Api.Application.Authentication;
using Nevma.Identity.Api.Infrastructure.Authentication;
using OpenIddict.Abstractions;

namespace Nevma.Identity.Api.Endpoints;

public static class AuthenticationEndpoints
{
    public static IEndpointRouteBuilder MapAuthenticationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/auth/register", async (
            RegisterUserRequest request,
            IRegistrationService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.RegisterAsync(request, cancellationToken);
            return result.IsSuccess
                ? Results.Created("/api/users/me", result.User)
                : Results.ValidationProblem(result.Errors);
        })
        .AllowAnonymous()
        .RequireRateLimiting(AuthenticationConfiguration.AuthenticationRateLimitPolicy)
        .WithTags("Authentication");

        endpoints.MapPost("/api/auth/logout-all", async (
            ClaimsPrincipal principal,
            IOpenIddictTokenManager tokenManager,
            IOpenIddictAuthorizationManager authorizationManager,
            CancellationToken cancellationToken) =>
        {
            var subject = principal.FindFirstValue(OpenIddictConstants.Claims.Subject);
            if (string.IsNullOrWhiteSpace(subject))
                return Results.Unauthorized();

            await tokenManager.RevokeBySubjectAsync(subject, cancellationToken);
            await authorizationManager.RevokeBySubjectAsync(subject, cancellationToken);
            return Results.NoContent();
        })
        .RequireAuthorization()
        .WithTags("Authentication");

        return endpoints;
    }
}
