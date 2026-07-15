using Nevma.Contracts.Identity;

namespace Nevma.Identity.Api.Application.Authentication;

public interface IMultiFactorAuthenticationService
{
    Task<MfaOperationResult<MfaStatusResponse>> GetStatusAsync(Guid userId);
    Task<MfaOperationResult<MfaSetupResponse>> BeginSetupAsync(Guid userId);
    Task<MfaOperationResult<MfaRecoveryCodesResponse>> EnableAsync(Guid userId, string code);
    Task<MfaOperationResult<MfaRecoveryCodesResponse>> RegenerateRecoveryCodesAsync(Guid userId, string code);
    Task<MfaOperationResult> DisableAsync(Guid userId, string code);
}

public enum MfaError
{
    None,
    AccountNotFound,
    AlreadyEnabled,
    NotEnabled,
    SetupRequired,
    InvalidCode
}

public sealed record MfaOperationResult(MfaError Error)
{
    public bool IsSuccess => Error == MfaError.None;

    public static MfaOperationResult Success() => new(MfaError.None);
    public static MfaOperationResult Failure(MfaError error) => new(error);
}

public sealed record MfaOperationResult<T>(T? Value, MfaError Error)
{
    public bool IsSuccess => Error == MfaError.None;

    public static MfaOperationResult<T> Success(T value) => new(value, MfaError.None);
    public static MfaOperationResult<T> Failure(MfaError error) => new(default, error);
}
