namespace Nevma.Contracts.Identity;

public sealed record MfaStatusResponse(bool IsEnabled, int RecoveryCodesLeft);

public sealed record MfaSetupResponse(string SharedKey, string AuthenticatorUri);

public sealed record MfaCodeRequest(string Code);

public sealed record MfaRecoveryCodesResponse(IReadOnlyList<string> RecoveryCodes);
