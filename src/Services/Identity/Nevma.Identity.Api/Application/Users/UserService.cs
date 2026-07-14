using Nevma.Contracts.Identity;
using Nevma.Identity.Api.Application;
using Nevma.Identity.Api.Domain.Users;

namespace Nevma.Identity.Api.Application.Users;

public sealed class UserService(
    IUserRepository repository,
    IIdentityUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<UserSummary?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await repository.GetByIdAsync(id, cancellationToken);
        return user is null ? null : ToSummary(user);
    }

    public async Task<CreateUserResult> CreateAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.DisplayName))
        {
            return CreateUserResult.Failure(nameof(request.DisplayName), "Display name is required.");
        }

        var user = User.Create(request.DisplayName, request.AvatarUrl, timeProvider.GetUtcNow());
        await repository.AddAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return CreateUserResult.Success(ToSummary(user));
    }

    private static UserSummary ToSummary(User user) =>
        new(user.Id, user.DisplayName, user.AvatarUrl);
}

public sealed record CreateUserResult(UserSummary? User, IReadOnlyDictionary<string, string[]> Errors)
{
    public bool IsSuccess => User is not null;

    public static CreateUserResult Success(UserSummary user) =>
        new(user, new Dictionary<string, string[]>());

    public static CreateUserResult Failure(string field, string error) =>
        new(null, new Dictionary<string, string[]> { [field] = [error] });
}
