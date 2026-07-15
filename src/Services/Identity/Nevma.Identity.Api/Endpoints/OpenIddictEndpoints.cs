using System.Security.Claims;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Nevma.Identity.Api.Application.Users;
using Nevma.Identity.Api.Application.Sessions;
using Nevma.Identity.Api.Infrastructure.Authentication;
using Nevma.Identity.Api.Infrastructure.Persistence;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;

namespace Nevma.Identity.Api.Endpoints;

public static class OpenIddictEndpoints
{
    public static IEndpointRouteBuilder MapOpenIddictEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/connect/authorize", AuthorizeAsync)
            .AllowAnonymous()
            .RequireRateLimiting(AuthenticationConfiguration.AuthenticationRateLimitPolicy)
            .WithTags("OpenID Connect");

        endpoints.MapPost("/connect/logout", (Delegate)LogoutAsync)
            .AllowAnonymous()
            .RequireRateLimiting(AuthenticationConfiguration.AuthenticationRateLimitPolicy)
            .WithTags("OpenID Connect");

        return endpoints;
    }

    private static async Task<IResult> AuthorizeAsync(
        HttpContext context,
        UserManager<IdentityAccount> userManager,
        SignInManager<IdentityAccount> signInManager,
        UserService userService,
        DeviceSessionService deviceSessionService,
        IOpenIddictApplicationManager applicationManager,
        IOpenIddictAuthorizationManager authorizationManager)
    {
        var request = context.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("The OpenID Connect request is unavailable.");
        var authentication = await context.AuthenticateAsync(IdentityConstants.ApplicationScheme);

        if (!authentication.Succeeded || authentication.Principal is null)
        {
            var returnUrl = context.Request.PathBase + context.Request.Path + context.Request.QueryString;
            return Results.Challenge(
                new AuthenticationProperties { RedirectUri = returnUrl },
                [IdentityConstants.ApplicationScheme]);
        }

        var account = await userManager.GetUserAsync(authentication.Principal);
        if (account is null)
            return Results.Forbid(authenticationSchemes: [IdentityConstants.ApplicationScheme]);

        var profile = await userService.GetByIdAsync(account.Id, context.RequestAborted);
        if (profile is null)
            return Results.Forbid(authenticationSchemes: [IdentityConstants.ApplicationScheme]);

        var principal = await signInManager.CreateUserPrincipalAsync(account);
        principal.SetClaim(OpenIddictConstants.Claims.Subject, account.Id.ToString());
        principal.SetClaim(OpenIddictConstants.Claims.Name, profile.DisplayName);
        principal.SetClaim(OpenIddictConstants.Claims.Email, account.Email);
        principal.SetScopes(request.GetScopes());
        principal.SetResources("nevma_api");

        var application = await applicationManager.FindByClientIdAsync(
            request.ClientId ?? string.Empty,
            context.RequestAborted);
        if (application is null)
            return Results.Forbid(authenticationSchemes: [IdentityConstants.ApplicationScheme]);
        var applicationId = await applicationManager.GetIdAsync(application, context.RequestAborted)
            ?? throw new InvalidOperationException("The OpenID Connect client has no identifier.");
        var authorizationDescriptor = new OpenIddictAuthorizationDescriptor
        {
            ApplicationId = applicationId,
            Status = OpenIddictConstants.Statuses.Valid,
            Subject = account.Id.ToString(),
            Type = OpenIddictConstants.AuthorizationTypes.AdHoc
        };
        authorizationDescriptor.Scopes.UnionWith(request.GetScopes());
        var authorization = await authorizationManager.CreateAsync(
            authorizationDescriptor,
            context.RequestAborted);
        var authorizationId = await authorizationManager.GetIdAsync(
            authorization,
            context.RequestAborted)
            ?? throw new InvalidOperationException("The OpenID Connect authorization has no identifier.");
        principal.SetAuthorizationId(authorizationId);

        var sessionId = Guid.NewGuid();
        principal.SetClaim("session_id", sessionId.ToString());
        await deviceSessionService.StartAsync(
            sessionId,
            account.Id,
            ReadDeviceParameter(request, "device_id", $"browser:{sessionId:N}", 128),
            ReadDeviceParameter(request, "device_name", "Unknown device", 100),
            ReadDeviceParameter(request, "device_platform", "Unknown", 30),
            authorizationId,
            context.RequestAborted);

        foreach (var claim in principal.Claims)
            claim.SetDestinations(GetDestinations(claim, principal));

        return Results.SignIn(
            principal,
            authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private static async Task<IResult> LogoutAsync(HttpContext context)
    {
        await context.SignOutAsync(IdentityConstants.ApplicationScheme);

        return Results.SignOut(
            new AuthenticationProperties { RedirectUri = "/" },
            [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme]);
    }

    private static IEnumerable<string> GetDestinations(Claim claim, ClaimsPrincipal principal)
    {
        return claim.Type switch
        {
            OpenIddictConstants.Claims.Subject =>
                [OpenIddictConstants.Destinations.AccessToken, OpenIddictConstants.Destinations.IdentityToken],
            OpenIddictConstants.Claims.Name when principal.HasScope(OpenIddictConstants.Scopes.Profile) =>
                [OpenIddictConstants.Destinations.AccessToken, OpenIddictConstants.Destinations.IdentityToken],
            OpenIddictConstants.Claims.Email when principal.HasScope(OpenIddictConstants.Scopes.Email) =>
                [OpenIddictConstants.Destinations.AccessToken, OpenIddictConstants.Destinations.IdentityToken],
            "session_id" =>
                [OpenIddictConstants.Destinations.AccessToken, OpenIddictConstants.Destinations.IdentityToken],
            ClaimTypes.Role =>
                [OpenIddictConstants.Destinations.AccessToken],
            _ => []
        };
    }

    private static string ReadDeviceParameter(
        OpenIddictRequest request,
        string name,
        string fallback,
        int maxLength)
    {
        var value = (string?)request.GetParameter(name);
        if (string.IsNullOrWhiteSpace(value))
            return fallback;
        value = value.Trim();
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
