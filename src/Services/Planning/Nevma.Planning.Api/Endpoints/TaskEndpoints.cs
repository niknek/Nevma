using System.Security.Claims;
using Nevma.Contracts.Planning;
using Nevma.Planning.Api.Application.Tasks;

namespace Nevma.Planning.Api.Endpoints;

public static class TaskEndpoints
{
    public static IEndpointRouteBuilder MapTaskEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var tasks = endpoints.MapGroup("/api/tasks")
            .RequireAuthorization()
            .WithTags("Tasks");

        tasks.MapPost("/", async (
            CreateTaskRequest request,
            ClaimsPrincipal principal,
            TaskService service,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(principal, out var ownerId))
                return Results.Unauthorized();

            var result = await service.CreateAsync(ownerId, request, cancellationToken);
            return result.IsSuccess
                ? Results.Created($"/api/tasks/{result.Task!.Id}", result.Task)
                : Results.ValidationProblem(result.Errors);
        });

        tasks.MapGet("/", async (
            TaskFilter? filter,
            ClaimsPrincipal principal,
            TaskService service,
            TimeProvider timeProvider,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(principal, out var ownerId))
                return Results.Unauthorized();

            var now = timeProvider.GetUtcNow();
            var dayStart = new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, TimeSpan.Zero);
            return Results.Ok(await service.ListAsync(
                ownerId,
                filter ?? TaskFilter.All,
                dayStart,
                cancellationToken));
        });

        tasks.MapPost("/{id:guid}/complete", async (
            Guid id,
            ClaimsPrincipal principal,
            TaskService service,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(principal, out var ownerId))
                return Results.Unauthorized();

            return await service.CompleteAsync(id, ownerId, cancellationToken) switch
            {
                CompleteTaskResult.Completed completed => Results.Ok(completed.Task),
                CompleteTaskResult.NotFound => Results.NotFound(),
                CompleteTaskResult.AlreadyCompleted => Results.Conflict(
                    new { message = "Task has already been completed." }),
                _ => Results.StatusCode(StatusCodes.Status500InternalServerError)
            };
        });

        return endpoints;
    }

    private static bool TryGetUserId(ClaimsPrincipal principal, out Guid userId) =>
        Guid.TryParse(principal.FindFirstValue("sub"), out userId);
}
