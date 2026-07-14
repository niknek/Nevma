using Nevma.Identity.Api.Domain.Users;

namespace Nevma.Identity.Api.Application.Users;

public interface IUserPrivacyRepository
{
    Task<UserSettings?> GetSettingsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddSettingsAsync(UserSettings settings, CancellationToken cancellationToken = default);
    Task<bool> IsBlockedAsync(Guid firstUserId, Guid secondUserId, CancellationToken cancellationToken = default);
    Task<BlockedUser?> GetBlockAsync(Guid blockerId, Guid blockedId, CancellationToken cancellationToken = default);
    Task AddBlockAsync(BlockedUser block, CancellationToken cancellationToken = default);
    void RemoveBlock(BlockedUser block);
    Task AddReportAsync(UserReport report, CancellationToken cancellationToken = default);
}
