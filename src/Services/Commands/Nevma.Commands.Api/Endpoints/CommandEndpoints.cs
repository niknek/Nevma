using System.Security.Claims;
using Nevma.Commands.Api.Application;
using Nevma.Contracts.Commands;

namespace Nevma.Commands.Api.Endpoints;

public static class CommandEndpoints
{
    public static IEndpointRouteBuilder MapCommandEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var commands = endpoints.MapGroup("/api/commands").RequireAuthorization().WithTags("Commands");
        commands.MapPost("/preview", async (PreviewCommandRequest request, ClaimsPrincipal principal,
            CommandService service, CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(principal, out var userId)) return Results.Unauthorized();
            return await service.PreviewAsync(userId, request, cancellationToken) switch
            {
                PreviewResult.Ready ready when ready.IsNew => Results.Created($"/api/commands/{ready.Command.Id}", ready.Command),
                PreviewResult.Ready ready => Results.Ok(ready.Command),
                PreviewResult.Invalid invalid => Results.ValidationProblem(new Dictionary<string, string[]> { [invalid.Field] = [invalid.Message] }),
                _ => Results.StatusCode(500)
            };
        });
        commands.MapGet("/{id:guid}", async (Guid id, ClaimsPrincipal principal, CommandService service, CancellationToken ct) =>
        {
            if (!TryGetUserId(principal, out var userId)) return Results.Unauthorized();
            var command = await service.GetAsync(id, userId, ct);
            return command is null ? Results.NotFound() : Results.Ok(command);
        });
        commands.MapPost("/{id:guid}/confirm", async (Guid id, HttpRequest request, ClaimsPrincipal principal,
            CommandService service, CancellationToken ct) =>
        {
            if (!TryGetUserId(principal, out var userId) || !TryGetToken(request, out var token)) return Results.Unauthorized();
            return MapAction(await service.ConfirmAsync(id, userId, token, ct));
        });
        commands.MapPost("/{id:guid}/undo", async (Guid id, HttpRequest request, ClaimsPrincipal principal,
            CommandService service, CancellationToken ct) =>
        {
            if (!TryGetUserId(principal, out var userId) || !TryGetToken(request, out var token)) return Results.Unauthorized();
            return MapAction(await service.UndoAsync(id, userId, token, ct));
        });
        return endpoints;
    }

    private static IResult MapAction(CommandActionResult result) => result switch
    {
        CommandActionResult.Changed changed => Results.Ok(changed.Command),
        CommandActionResult.Conflict conflict => Results.Conflict(conflict.Command),
        CommandActionResult.NotFound => Results.NotFound(),
        _ => Results.StatusCode(500)
    };
    private static bool TryGetUserId(ClaimsPrincipal principal, out Guid id) => Guid.TryParse(principal.FindFirstValue("sub"), out id);
    private static bool TryGetToken(HttpRequest request, out string token)
    {
        const string prefix = "Bearer ";
        var header = request.Headers.Authorization.ToString();
        token = header.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ? header[prefix.Length..].Trim() : string.Empty;
        return token.Length > 0;
    }
}
