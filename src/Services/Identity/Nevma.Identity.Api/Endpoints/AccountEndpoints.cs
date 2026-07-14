using System.Net;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Nevma.Identity.Api.Infrastructure.Authentication;
using Nevma.Identity.Api.Infrastructure.Persistence;

namespace Nevma.Identity.Api.Endpoints;

public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var account = endpoints.MapGroup("/account")
            .AllowAnonymous()
            .WithTags("Authentication");

        account.MapGet("/login", (HttpContext context, string? returnUrl) =>
        {
            var antiforgery = context.RequestServices.GetRequiredService<IAntiforgery>();
            var requestToken = antiforgery.GetAndStoreTokens(context).RequestToken
                ?? throw new InvalidOperationException("An antiforgery request token could not be created.");
            var safeReturnUrl = GetSafeReturnUrl(returnUrl);

            var html = $$"""
                <!doctype html>
                <html lang="en">
                <head>
                    <meta charset="utf-8">
                    <meta name="viewport" content="width=device-width, initial-scale=1">
                    <title>Sign in to Nevma</title>
                </head>
                <body>
                    <main>
                        <h1>Sign in to Nevma</h1>
                        <form method="post" action="/account/login">
                            <input type="hidden" name="__RequestVerificationToken" value="{{WebUtility.HtmlEncode(requestToken)}}">
                            <input type="hidden" name="ReturnUrl" value="{{WebUtility.HtmlEncode(safeReturnUrl)}}">
                            <label>Email <input name="Email" type="email" autocomplete="username" required></label>
                            <label>Password <input name="Password" type="password" autocomplete="current-password" required></label>
                            <button type="submit">Sign in</button>
                        </form>
                    </main>
                </body>
                </html>
                """;

            return Results.Content(html, "text/html; charset=utf-8");
        });

        account.MapPost("/login", async (
            [FromForm] LoginForm form,
            SignInManager<IdentityAccount> signInManager) =>
        {
            var result = await signInManager.PasswordSignInAsync(
                form.Email.Trim(),
                form.Password,
                isPersistent: false,
                lockoutOnFailure: true);

            return result.Succeeded
                ? Results.LocalRedirect(GetSafeReturnUrl(form.ReturnUrl))
                : Results.Problem(
                    statusCode: StatusCodes.Status401Unauthorized,
                    title: "Sign-in failed.",
                    detail: "The credentials are invalid or the account is temporarily unavailable.");
        })
        .RequireRateLimiting(AuthenticationConfiguration.AuthenticationRateLimitPolicy);

        return endpoints;
    }

    private static string GetSafeReturnUrl(string? returnUrl) =>
        !string.IsNullOrWhiteSpace(returnUrl) &&
        Uri.IsWellFormedUriString(returnUrl, UriKind.Relative) &&
        returnUrl.StartsWith("/", StringComparison.Ordinal) &&
        !returnUrl.StartsWith("//", StringComparison.Ordinal)
            ? returnUrl
            : "/";
}

public sealed class LoginForm
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string ReturnUrl { get; init; } = "/";
}
