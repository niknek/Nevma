using Nevma.Identity.Api.Application.Sessions;
using OpenIddict.Abstractions;

namespace Nevma.Identity.Api.Infrastructure.Authentication;

public sealed class OpenIddictSessionTokenRevoker(
    IOpenIddictTokenManager tokenManager,
    IOpenIddictAuthorizationManager authorizationManager) : ISessionTokenRevoker
{
    public async Task RevokeAsync(
        string authorizationId,
        CancellationToken cancellationToken = default)
    {
        await tokenManager.RevokeByAuthorizationIdAsync(authorizationId, cancellationToken);
        var authorization = await authorizationManager.FindByIdAsync(authorizationId, cancellationToken);
        if (authorization is not null)
            await authorizationManager.TryRevokeAsync(authorization, cancellationToken);
    }
}
