using Nevma.Contracts.Identity;

namespace Nevma.Identity.Api.Application.Authentication;

public interface IRegistrationService
{
    Task<RegistrationResult> RegisterAsync(
        RegisterUserRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record RegistrationResult(
    UserSummary? User,
    IReadOnlyDictionary<string, string[]> Errors)
{
    public bool IsSuccess => User is not null;

    public static RegistrationResult Success(UserSummary user) =>
        new(user, new Dictionary<string, string[]>());

    public static RegistrationResult Failure(string field, params string[] errors) =>
        new(null, new Dictionary<string, string[]> { [field] = errors });
}
