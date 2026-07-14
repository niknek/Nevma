using Nevma.Contracts.Identity;
using Nevma.Identity.Api.Application.Authentication;
using Nevma.Identity.Api.Infrastructure.Authentication;

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

        return endpoints;
    }
}
