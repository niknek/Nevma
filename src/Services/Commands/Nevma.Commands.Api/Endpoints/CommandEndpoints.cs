using System.Security.Claims;
using Microsoft.Extensions.Options;
using Nevma.Commands.Api.Application;
using Nevma.Commands.Api.Infrastructure.Speech;
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
        commands.MapPost("/transcribe", async (
            HttpRequest request,
            ISpeechToTextProvider provider,
            IOptions<SpeechProviderOptions> options,
            CancellationToken cancellationToken) =>
        {
            if (!request.HasFormContentType)
                return Results.BadRequest(new { message = "A multipart audio upload is required." });
            var form = await request.ReadFormAsync(cancellationToken);
            var audio = form.Files.GetFile("audio");
            if (audio is null || audio.Length == 0)
                return Results.BadRequest(new { message = "Audio is required." });
            if (audio.Length > options.Value.MaxAudioBytes)
                return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);
            if (!AllowedAudioTypes.Contains(audio.ContentType))
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["audio"] = ["Supported audio types are MPEG, MP4/M4A, WAV, WebM, and OGG."]
                });

            await using var stream = audio.OpenReadStream();
            return await provider.TranscribeAsync(
                stream,
                audio.ContentType,
                form["language"].FirstOrDefault(),
                cancellationToken) switch
            {
                SpeechToTextResult.Transcribed transcribed => Results.Ok(
                    new SpeechTranscriptionResponse(transcribed.Transcript, transcribed.Confidence)),
                SpeechToTextResult.Invalid invalid => Results.ValidationProblem(
                    new Dictionary<string, string[]> { ["audio"] = [invalid.Message] }),
                SpeechToTextResult.Unavailable => Results.Problem(
                    statusCode: StatusCodes.Status503ServiceUnavailable,
                    title: "Speech transcription is unavailable."),
                _ => Results.StatusCode(500)
            };
        }).DisableAntiforgery();
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
    private static readonly HashSet<string> AllowedAudioTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "audio/mpeg", "audio/mp4", "audio/x-m4a", "audio/wav", "audio/x-wav", "audio/webm", "audio/ogg"
    };
    private static bool TryGetToken(HttpRequest request, out string token)
    {
        const string prefix = "Bearer ";
        var header = request.Headers.Authorization.ToString();
        token = header.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ? header[prefix.Length..].Trim() : string.Empty;
        return token.Length > 0;
    }
}
