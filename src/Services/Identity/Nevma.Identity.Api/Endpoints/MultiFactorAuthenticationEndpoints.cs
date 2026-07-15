using System.Security.Claims;
using Nevma.Contracts.Identity;
using Nevma.Identity.Api.Application.Authentication;
using Nevma.Identity.Api.Application.Security;
using OpenIddict.Abstractions;

namespace Nevma.Identity.Api.Endpoints;

public static class MultiFactorAuthenticationEndpoints
{
    public static IEndpointRouteBuilder MapMultiFactorAuthenticationEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var mfa = endpoints.MapGroup("/api/auth/mfa")
            .RequireAuthorization()
            .WithTags("Multi-factor authentication");

        mfa.MapGet("/", async (
            ClaimsPrincipal principal,
            HttpContext context,
            IMultiFactorAuthenticationService service) =>
        {
            SetSensitiveResponseHeaders(context.Response);
            return TryGetUserId(principal, out var userId)
                ? ToResult(await service.GetStatusAsync(userId))
                : Results.Unauthorized();
        });

        mfa.MapPost("/setup", async (
            ClaimsPrincipal principal,
            HttpContext context,
            IMultiFactorAuthenticationService service,
            SecurityEventService securityEvents) =>
        {
            SetSensitiveResponseHeaders(context.Response);
            if (!TryGetUserId(principal, out var userId))
                return Results.Unauthorized();
            var result = await service.BeginSetupAsync(userId);
            await securityEvents.RecordFromRequestAsync(
                userId,
                "mfa.setup_started",
                result.IsSuccess,
                context);
            return ToResult(result);
        });

        mfa.MapPost("/enable", async (
            MfaCodeRequest request,
            ClaimsPrincipal principal,
            HttpContext context,
            IMultiFactorAuthenticationService service,
            SecurityEventService securityEvents,
            IOpenIddictTokenManager tokenManager,
            IOpenIddictAuthorizationManager authorizationManager,
            CancellationToken cancellationToken) =>
        {
            SetSensitiveResponseHeaders(context.Response);
            if (!TryGetUserId(principal, out var userId))
                return Results.Unauthorized();

            var result = await service.EnableAsync(userId, request.Code);
            if (result.IsSuccess)
                await RevokeExistingTokensAsync(userId, tokenManager, authorizationManager, cancellationToken);
            await securityEvents.RecordFromRequestAsync(userId, "mfa.enabled", result.IsSuccess, context);
            return ToResult(result);
        });

        mfa.MapPost("/recovery-codes", async (
            MfaCodeRequest request,
            ClaimsPrincipal principal,
            HttpContext context,
            IMultiFactorAuthenticationService service,
            SecurityEventService securityEvents) =>
        {
            SetSensitiveResponseHeaders(context.Response);
            if (!TryGetUserId(principal, out var userId))
                return Results.Unauthorized();
            var result = await service.RegenerateRecoveryCodesAsync(userId, request.Code);
            await securityEvents.RecordFromRequestAsync(
                userId,
                "mfa.recovery_codes_regenerated",
                result.IsSuccess,
                context);
            return ToResult(result);
        });

        mfa.MapPost("/disable", async (
            MfaCodeRequest request,
            ClaimsPrincipal principal,
            HttpContext context,
            IMultiFactorAuthenticationService service,
            SecurityEventService securityEvents,
            IOpenIddictTokenManager tokenManager,
            IOpenIddictAuthorizationManager authorizationManager,
            CancellationToken cancellationToken) =>
        {
            SetSensitiveResponseHeaders(context.Response);
            if (!TryGetUserId(principal, out var userId))
                return Results.Unauthorized();

            var result = await service.DisableAsync(userId, request.Code);
            if (result.IsSuccess)
                await RevokeExistingTokensAsync(userId, tokenManager, authorizationManager, cancellationToken);
            await securityEvents.RecordFromRequestAsync(userId, "mfa.disabled", result.IsSuccess, context);
            return ToResult(result);
        });

        return endpoints;
    }

    private static IResult ToResult<T>(MfaOperationResult<T> result) =>
        result.IsSuccess ? Results.Ok(result.Value) : ToErrorResult(result.Error);

    private static IResult ToResult(MfaOperationResult result) =>
        result.IsSuccess ? Results.NoContent() : ToErrorResult(result.Error);

    private static IResult ToErrorResult(MfaError error) => error switch
    {
        MfaError.AccountNotFound => Results.NotFound(),
        MfaError.AlreadyEnabled => Results.Conflict(new { detail = "Multi-factor authentication is already enabled." }),
        MfaError.NotEnabled => Results.Conflict(new { detail = "Multi-factor authentication is not enabled." }),
        MfaError.SetupRequired => Results.Conflict(new { detail = "Create an authenticator setup before enabling MFA." }),
        MfaError.InvalidCode => Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["code"] = ["The authenticator code is invalid."]
        }),
        _ => Results.StatusCode(StatusCodes.Status500InternalServerError)
    };

    private static async Task RevokeExistingTokensAsync(
        Guid userId,
        IOpenIddictTokenManager tokenManager,
        IOpenIddictAuthorizationManager authorizationManager,
        CancellationToken cancellationToken)
    {
        var subject = userId.ToString();
        await tokenManager.RevokeBySubjectAsync(subject, cancellationToken);
        await authorizationManager.RevokeBySubjectAsync(subject, cancellationToken);
    }

    private static void SetSensitiveResponseHeaders(HttpResponse response)
    {
        response.Headers.CacheControl = "no-store";
        response.Headers.Pragma = "no-cache";
    }

    private static bool TryGetUserId(ClaimsPrincipal principal, out Guid userId) =>
        Guid.TryParse(principal.FindFirstValue(OpenIddictConstants.Claims.Subject), out userId);
}
