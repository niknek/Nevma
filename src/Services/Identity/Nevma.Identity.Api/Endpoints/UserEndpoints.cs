using Nevma.Contracts.Identity;
using Nevma.Identity.Api.Application.Users;

namespace Nevma.Identity.Api.Endpoints;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var users = endpoints.MapGroup("/api/users").WithTags("Users");

        users.MapGet("/{id:guid}", async (
            Guid id,
            UserService service,
            CancellationToken cancellationToken) =>
        {
            var user = await service.GetByIdAsync(id, cancellationToken);
            return user is null ? Results.NotFound() : Results.Ok(user);
        });

        users.MapPost("/", async (
            CreateUserRequest request,
            UserService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.CreateAsync(request, cancellationToken);
            return result.IsSuccess
                ? Results.Created($"/api/users/{result.User!.Id}", result.User)
                : Results.ValidationProblem(result.Errors);
        });

        return endpoints;
    }
}
