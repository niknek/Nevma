using Nevma.Contracts.Identity;
using Nevma.Identity.Api.Application.Users;

namespace Nevma.Identity.Api.Endpoints;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var users = endpoints.MapGroup("/api/users").WithTags("Users");

        users.MapGet("/{id:guid}", (Guid id, UserService service) =>
        {
            var user = service.GetById(id);
            return user is null ? Results.NotFound() : Results.Ok(user);
        });

        users.MapPost("/", (CreateUserRequest request, UserService service) =>
        {
            var result = service.Create(request);
            return result.IsSuccess
                ? Results.Created($"/api/users/{result.User!.Id}", result.User)
                : Results.ValidationProblem(result.Errors);
        });

        return endpoints;
    }
}
