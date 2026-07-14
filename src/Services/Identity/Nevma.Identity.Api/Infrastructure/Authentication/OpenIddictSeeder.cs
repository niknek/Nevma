using OpenIddict.Abstractions;

namespace Nevma.Identity.Api.Infrastructure.Authentication;

public sealed class OpenIddictSeeder(
    IServiceProvider serviceProvider,
    IConfiguration configuration) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!configuration.GetValue("Authentication:SeedDevelopmentClient", false))
            return;

        await using var scope = serviceProvider.CreateAsyncScope();
        await SeedScopeAsync(scope.ServiceProvider, cancellationToken);
        await SeedClientAsync(scope.ServiceProvider, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static async Task SeedScopeAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var manager = services.GetRequiredService<IOpenIddictScopeManager>();
        if (await manager.FindByNameAsync("nevma_api", cancellationToken) is not null)
            return;

        await manager.CreateAsync(new OpenIddictScopeDescriptor
        {
            Name = "nevma_api",
            DisplayName = "Nevma API",
            Resources = { "nevma_api" }
        }, cancellationToken);
    }

    private async Task SeedClientAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var manager = services.GetRequiredService<IOpenIddictApplicationManager>();
        const string clientId = "nevma-mobile";
        if (await manager.FindByClientIdAsync(clientId, cancellationToken) is not null)
            return;

        var redirectUri = configuration["Authentication:MobileRedirectUri"]
            ?? throw new InvalidOperationException("Authentication:MobileRedirectUri is required when seeding the client.");
        var postLogoutRedirectUri = configuration["Authentication:MobilePostLogoutRedirectUri"]
            ?? throw new InvalidOperationException(
                "Authentication:MobilePostLogoutRedirectUri is required when seeding the client.");

        await manager.CreateAsync(new OpenIddictApplicationDescriptor
        {
            ApplicationType = OpenIddictConstants.ApplicationTypes.Native,
            ClientId = clientId,
            ClientType = OpenIddictConstants.ClientTypes.Public,
            ConsentType = OpenIddictConstants.ConsentTypes.Implicit,
            DisplayName = "Nevma Mobile",
            RedirectUris = { new Uri(redirectUri, UriKind.Absolute) },
            PostLogoutRedirectUris = { new Uri(postLogoutRedirectUri, UriKind.Absolute) },
            Permissions =
            {
                OpenIddictConstants.Permissions.Endpoints.Authorization,
                OpenIddictConstants.Permissions.Endpoints.EndSession,
                OpenIddictConstants.Permissions.Endpoints.Token,
                OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
                OpenIddictConstants.Permissions.GrantTypes.RefreshToken,
                OpenIddictConstants.Permissions.ResponseTypes.Code,
                OpenIddictConstants.Permissions.Scopes.Email,
                OpenIddictConstants.Permissions.Scopes.Profile,
                OpenIddictConstants.Permissions.Prefixes.Scope + "nevma_api"
            },
            Requirements =
            {
                OpenIddictConstants.Requirements.Features.ProofKeyForCodeExchange
            }
        }, cancellationToken);
    }
}
