using Microsoft.AspNetCore.Identity;
using Nevma.Contracts.Identity;
using Nevma.Identity.Api.Application.Authentication;
using Nevma.Identity.Api.Infrastructure.Persistence;

namespace Nevma.Identity.Api.Infrastructure.Authentication;

public sealed class MultiFactorAuthenticationService(UserManager<IdentityAccount> userManager)
    : IMultiFactorAuthenticationService
{
    private const int RecoveryCodeCount = 10;

    public async Task<MfaOperationResult<MfaStatusResponse>> GetStatusAsync(Guid userId)
    {
        var account = await userManager.FindByIdAsync(userId.ToString());
        if (account is null)
            return MfaOperationResult<MfaStatusResponse>.Failure(MfaError.AccountNotFound);

        return MfaOperationResult<MfaStatusResponse>.Success(new MfaStatusResponse(
            await userManager.GetTwoFactorEnabledAsync(account),
            await userManager.CountRecoveryCodesAsync(account)));
    }

    public async Task<MfaOperationResult<MfaSetupResponse>> BeginSetupAsync(Guid userId)
    {
        var account = await userManager.FindByIdAsync(userId.ToString());
        if (account is null)
            return MfaOperationResult<MfaSetupResponse>.Failure(MfaError.AccountNotFound);
        if (await userManager.GetTwoFactorEnabledAsync(account))
            return MfaOperationResult<MfaSetupResponse>.Failure(MfaError.AlreadyEnabled);

        var key = await userManager.GetAuthenticatorKeyAsync(account);
        if (string.IsNullOrWhiteSpace(key))
        {
            var reset = await userManager.ResetAuthenticatorKeyAsync(account);
            if (!reset.Succeeded)
                throw new InvalidOperationException("The authenticator key could not be created.");
            key = await userManager.GetAuthenticatorKeyAsync(account);
        }

        if (string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException("The authenticator key could not be loaded.");

        var label = account.Email ?? account.UserName ?? account.Id.ToString();
        var uri = $"otpauth://totp/{Uri.EscapeDataString($"Nevma:{label}")}" +
                  $"?secret={Uri.EscapeDataString(key)}&issuer=Nevma&digits=6";
        return MfaOperationResult<MfaSetupResponse>.Success(new MfaSetupResponse(key, uri));
    }

    public async Task<MfaOperationResult<MfaRecoveryCodesResponse>> EnableAsync(Guid userId, string code)
    {
        var account = await userManager.FindByIdAsync(userId.ToString());
        if (account is null)
            return MfaOperationResult<MfaRecoveryCodesResponse>.Failure(MfaError.AccountNotFound);
        if (await userManager.GetTwoFactorEnabledAsync(account))
            return MfaOperationResult<MfaRecoveryCodesResponse>.Failure(MfaError.AlreadyEnabled);
        if (string.IsNullOrWhiteSpace(await userManager.GetAuthenticatorKeyAsync(account)))
            return MfaOperationResult<MfaRecoveryCodesResponse>.Failure(MfaError.SetupRequired);
        if (!await VerifyCodeAsync(account, code))
            return MfaOperationResult<MfaRecoveryCodesResponse>.Failure(MfaError.InvalidCode);

        var enabled = await userManager.SetTwoFactorEnabledAsync(account, true);
        if (!enabled.Succeeded)
            throw new InvalidOperationException("Multi-factor authentication could not be enabled.");
        await userManager.UpdateSecurityStampAsync(account);

        var recoveryCodes = await userManager.GenerateNewTwoFactorRecoveryCodesAsync(account, RecoveryCodeCount);
        return MfaOperationResult<MfaRecoveryCodesResponse>.Success(
            new MfaRecoveryCodesResponse(recoveryCodes?.ToArray() ?? []));
    }

    public async Task<MfaOperationResult<MfaRecoveryCodesResponse>> RegenerateRecoveryCodesAsync(
        Guid userId,
        string code)
    {
        var account = await userManager.FindByIdAsync(userId.ToString());
        if (account is null)
            return MfaOperationResult<MfaRecoveryCodesResponse>.Failure(MfaError.AccountNotFound);
        if (!await userManager.GetTwoFactorEnabledAsync(account))
            return MfaOperationResult<MfaRecoveryCodesResponse>.Failure(MfaError.NotEnabled);
        if (!await VerifyCodeAsync(account, code))
            return MfaOperationResult<MfaRecoveryCodesResponse>.Failure(MfaError.InvalidCode);

        var recoveryCodes = await userManager.GenerateNewTwoFactorRecoveryCodesAsync(account, RecoveryCodeCount);
        return MfaOperationResult<MfaRecoveryCodesResponse>.Success(
            new MfaRecoveryCodesResponse(recoveryCodes?.ToArray() ?? []));
    }

    public async Task<MfaOperationResult> DisableAsync(Guid userId, string code)
    {
        var account = await userManager.FindByIdAsync(userId.ToString());
        if (account is null)
            return MfaOperationResult.Failure(MfaError.AccountNotFound);
        if (!await userManager.GetTwoFactorEnabledAsync(account))
            return MfaOperationResult.Failure(MfaError.NotEnabled);
        if (!await VerifyCodeAsync(account, code))
            return MfaOperationResult.Failure(MfaError.InvalidCode);

        var disabled = await userManager.SetTwoFactorEnabledAsync(account, false);
        if (!disabled.Succeeded)
            throw new InvalidOperationException("Multi-factor authentication could not be disabled.");
        var reset = await userManager.ResetAuthenticatorKeyAsync(account);
        if (!reset.Succeeded)
            throw new InvalidOperationException("The authenticator key could not be reset.");
        await userManager.GenerateNewTwoFactorRecoveryCodesAsync(account, 0);
        await userManager.UpdateSecurityStampAsync(account);
        return MfaOperationResult.Success();
    }

    private Task<bool> VerifyCodeAsync(IdentityAccount account, string code) =>
        userManager.VerifyTwoFactorTokenAsync(
            account,
            TokenOptions.DefaultAuthenticatorProvider,
            NormalizeCode(code));

    private static string NormalizeCode(string code) =>
        code.Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal);
}
