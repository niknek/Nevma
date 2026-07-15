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

        tasks.MapGet("/shared", async (
            ClaimsPrincipal principal,
            TaskSharingService service,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(principal, out var userId))
                return Results.Unauthorized();
            return Results.Ok(await service.ListSharedAsync(userId, cancellationToken));
        });

        tasks.MapGet("/{id:guid}/collaborators", async (
            Guid id,
            ClaimsPrincipal principal,
            TaskSharingService service,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(principal, out var ownerId))
                return Results.Unauthorized();
            var collaborators = await service.ListCollaboratorsAsync(id, ownerId, cancellationToken);
            return collaborators is null ? Results.NotFound() : Results.Ok(collaborators);
        });

        tasks.MapPost("/{id:guid}/collaborators", async (
            Guid id,
            ShareTaskRequest request,
            ClaimsPrincipal principal,
            TaskSharingService service,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(principal, out var ownerId))
                return Results.Unauthorized();
            return await service.ShareAsync(id, ownerId, request, cancellationToken) switch
            {
                TaskShareResult.Changed changed => Results.Ok(changed.Share),
                TaskShareResult.Invalid invalid => Results.ValidationProblem(
                    new Dictionary<string, string[]> { ["userId"] = [invalid.Message] }),
                TaskShareResult.NotFound => Results.NotFound(),
                _ => Results.StatusCode(StatusCodes.Status500InternalServerError)
            };
        });

        tasks.MapDelete("/{id:guid}/collaborators/{userId:guid}", async (
            Guid id,
            Guid userId,
            ClaimsPrincipal principal,
            TaskSharingService service,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(principal, out var ownerId))
                return Results.Unauthorized();
            return await service.RevokeAsync(id, ownerId, userId, cancellationToken)
                ? Results.NoContent()
                : Results.NotFound();
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

        tasks.MapPut("/{id:guid}", async (
            Guid id,
            UpdateTaskRequest request,
            ClaimsPrincipal principal,
            TaskService service,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(principal, out var ownerId))
                return Results.Unauthorized();

            return await service.UpdateAsync(id, ownerId, request, cancellationToken) switch
            {
                ChangeTaskResult.Updated updated => Results.Ok(updated.Task),
                ChangeTaskResult.NotFound => Results.NotFound(),
                ChangeTaskResult.Conflict conflict => Results.Conflict(new { message = conflict.Message }),
                ChangeTaskResult.ValidationFailed invalid => Results.ValidationProblem(invalid.Errors),
                _ => Results.StatusCode(StatusCodes.Status500InternalServerError)
            };
        });

        tasks.MapDelete("/{id:guid}", async (
            Guid id,
            ClaimsPrincipal principal,
            TaskService service,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(principal, out var ownerId))
                return Results.Unauthorized();

            return await service.DeleteAsync(id, ownerId, cancellationToken) switch
            {
                DeleteTaskResult.Deleted => Results.NoContent(),
                DeleteTaskResult.NotFound => Results.NotFound(),
                DeleteTaskResult.Conflict => Results.Conflict(new { message = "Task changed during deletion." }),
                _ => Results.StatusCode(StatusCodes.Status500InternalServerError)
            };
        });

        return endpoints;
    }

    private static bool TryGetUserId(ClaimsPrincipal principal, out Guid userId) =>
        Guid.TryParse(principal.FindFirstValue("sub"), out userId);
}
