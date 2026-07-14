using Nevma.Contracts.Identity;
using Nevma.Identity.Api.Domain.Users;

namespace Nevma.Identity.Api.Application.Users;

public sealed class UserService(
    IUserRepository repository,
    IIdentityUnitOfWork unitOfWork)
{
    public async Task<UserSummary?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await repository.GetByIdAsync(id, cancellationToken);
        return user is null ? null : ToSummary(user);
    }

    public async Task<UpdateProfileResult> UpdateAsync(
        Guid userId,
        UpdateProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.DisplayName) || request.DisplayName.Length > 100)
            errors[nameof(request.DisplayName)] = ["Display name must contain 1 to 100 characters."];
        if (request.AvatarUrl?.Length > 2_048 ||
            (request.AvatarUrl is not null && !Uri.TryCreate(request.AvatarUrl, UriKind.Absolute, out _)))
            errors[nameof(request.AvatarUrl)] = ["Avatar URL must be a valid absolute URL."];
        if (errors.Count > 0)
            return new UpdateProfileResult.Invalid(errors);

        var user = await repository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
            return new UpdateProfileResult.NotFound();
        user.Update(request.DisplayName, request.AvatarUrl);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new UpdateProfileResult.Updated(ToSummary(user));
    }

    private static UserSummary ToSummary(User user) =>
        new(user.Id, user.DisplayName, user.AvatarUrl);
}

public abstract record UpdateProfileResult
{
    public sealed record Updated(UserSummary User) : UpdateProfileResult;
    public sealed record NotFound : UpdateProfileResult;
    public sealed record Invalid(IReadOnlyDictionary<string, string[]> Errors) : UpdateProfileResult;
}
