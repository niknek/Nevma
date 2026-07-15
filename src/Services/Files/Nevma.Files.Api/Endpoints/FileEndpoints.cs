using System.Security.Claims;
using Nevma.Contracts.Files;
using Nevma.Files.Api.Application;

namespace Nevma.Files.Api.Endpoints;

public static class FileEndpoints
{
    public static IEndpointRouteBuilder MapFileEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var files = endpoints.MapGroup("/api/files").RequireAuthorization().WithTags("Files");

        files.MapPost("/", async (
            IFormFile file,
            ClaimsPrincipal principal,
            FileAssetService service,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(principal, out var userId))
                return Results.Unauthorized();
            await using var stream = file.OpenReadStream();
            var result = await service.UploadAsync(
                userId,
                new FileUpload(file.FileName, file.ContentType, file.Length, stream),
                cancellationToken);
            return result switch
            {
                FileUploadResult.Uploaded uploaded => Results.Created($"/api/files/{uploaded.File.Id}", uploaded.File),
                FileUploadResult.Invalid invalid => Results.ValidationProblem(
                    new Dictionary<string, string[]> { [invalid.Field] = [invalid.Message] }),
                FileUploadResult.Rejected rejected => Results.BadRequest(new { message = rejected.Message }),
                FileUploadResult.ScanUnavailable => Results.Problem(
                    statusCode: StatusCodes.Status503ServiceUnavailable,
                    title: "File security scanning is unavailable."),
                _ => Results.StatusCode(StatusCodes.Status500InternalServerError)
            };
        }).DisableAntiforgery();

        files.MapGet("/", async (
            ClaimsPrincipal principal,
            FileAssetService service,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(principal, out var userId))
                return Results.Unauthorized();
            return Results.Ok(await service.ListAsync(userId, cancellationToken));
        });

        files.MapPost("/{id:guid}/download-url", async (
            Guid id,
            ClaimsPrincipal principal,
            FileAssetService service,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(principal, out var userId))
                return Results.Unauthorized();
            return await service.CreateDownloadUrlAsync(id, userId, cancellationToken) switch
            {
                FileUrlResult.Created created => Results.Ok(created.Download),
                FileUrlResult.NotFound => Results.NotFound(),
                FileUrlResult.NotSupported => Results.Problem(
                    statusCode: StatusCodes.Status501NotImplemented,
                    title: "Temporary download URLs are unavailable for the configured storage provider."),
                _ => Results.StatusCode(StatusCodes.Status500InternalServerError)
            };
        });

        files.MapGet("/{id:guid}", async (
            Guid id,
            ClaimsPrincipal principal,
            FileAssetService service,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(principal, out var userId))
                return Results.Unauthorized();
            var file = await service.GetAsync(id, userId, cancellationToken);
            return file is null ? Results.NotFound() : Results.Ok(file);
        });

        files.MapGet("/{id:guid}/content", async (
            Guid id,
            ClaimsPrincipal principal,
            FileAssetService service,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(principal, out var userId))
                return Results.Unauthorized();
            return await service.OpenAsync(id, userId, cancellationToken) switch
            {
                FileDownloadResult.Found found => Results.File(
                    found.Content, found.ContentType, found.FileName, enableRangeProcessing: true),
                FileDownloadResult.NotFound => Results.NotFound(),
                _ => Results.StatusCode(StatusCodes.Status500InternalServerError)
            };
        });

        files.MapPost("/{id:guid}/grants", async (
            Guid id,
            GrantFileAccessRequest request,
            ClaimsPrincipal principal,
            FileAssetService service,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(principal, out var ownerId))
                return Results.Unauthorized();
            return await service.GrantAsync(id, ownerId, request.UserId, cancellationToken)
                ? Results.NoContent() : Results.NotFound();
        });

        files.MapDelete("/{id:guid}/grants/{userId:guid}", async (
            Guid id,
            Guid userId,
            ClaimsPrincipal principal,
            FileAssetService service,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(principal, out var ownerId))
                return Results.Unauthorized();
            return await service.RevokeAsync(id, ownerId, userId, cancellationToken)
                ? Results.NoContent() : Results.NotFound();
        });

        files.MapDelete("/{id:guid}", async (
            Guid id,
            ClaimsPrincipal principal,
            FileAssetService service,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(principal, out var ownerId))
                return Results.Unauthorized();
            return await service.DeleteAsync(id, ownerId, cancellationToken)
                ? Results.NoContent() : Results.NotFound();
        });
        return endpoints;
    }

    private static bool TryGetUserId(ClaimsPrincipal principal, out Guid userId) =>
        Guid.TryParse(principal.FindFirstValue("sub"), out userId);
}
