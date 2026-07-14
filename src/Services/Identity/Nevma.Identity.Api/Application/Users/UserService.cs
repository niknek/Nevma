using Nevma.Contracts.Identity;
using Nevma.Identity.Api.Domain.Users;

namespace Nevma.Identity.Api.Application.Users;

public sealed class UserService(
    IUserRepository repository)
{
    public async Task<UserSummary?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await repository.GetByIdAsync(id, cancellationToken);
        return user is null ? null : ToSummary(user);
    }

    private static UserSummary ToSummary(User user) =>
        new(user.Id, user.DisplayName, user.AvatarUrl);
}
