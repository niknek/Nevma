using Microsoft.EntityFrameworkCore;
using Nevma.Identity.Api.Application.Users;
using Nevma.Identity.Api.Domain.Users;
using Nevma.Identity.Api.Infrastructure.Persistence;

namespace Nevma.Identity.Api.Infrastructure.Users;

public sealed class EfUserPrivacyRepository(IdentityDbContext dbContext) : IUserPrivacyRepository
{
    public Task<UserSettings?> GetSettingsAsync(Guid userId, CancellationToken cancellationToken = default) =>
        dbContext.UserSettings.SingleOrDefaultAsync(settings => settings.UserId == userId, cancellationToken);

    public async Task AddSettingsAsync(UserSettings settings, CancellationToken cancellationToken = default) =>
        await dbContext.UserSettings.AddAsync(settings, cancellationToken);

    public Task<bool> IsBlockedAsync(
        Guid firstUserId,
        Guid secondUserId,
        CancellationToken cancellationToken = default) =>
        dbContext.BlockedUsers.AnyAsync(block =>
            (block.BlockerId == firstUserId && block.BlockedId == secondUserId) ||
            (block.BlockerId == secondUserId && block.BlockedId == firstUserId),
            cancellationToken);

    public Task<BlockedUser?> GetBlockAsync(
        Guid blockerId,
        Guid blockedId,
        CancellationToken cancellationToken = default) =>
        dbContext.BlockedUsers.SingleOrDefaultAsync(
            block => block.BlockerId == blockerId && block.BlockedId == blockedId,
            cancellationToken);

    public async Task AddBlockAsync(BlockedUser block, CancellationToken cancellationToken = default) =>
        await dbContext.BlockedUsers.AddAsync(block, cancellationToken);

    public void RemoveBlock(BlockedUser block) => dbContext.BlockedUsers.Remove(block);

    public async Task AddReportAsync(UserReport report, CancellationToken cancellationToken = default) =>
        await dbContext.UserReports.AddAsync(report, cancellationToken);
}
