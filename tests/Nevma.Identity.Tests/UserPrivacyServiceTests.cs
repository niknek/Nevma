using Microsoft.EntityFrameworkCore;
using Nevma.Contracts.Identity;
using Nevma.Identity.Api.Application.Users;
using Nevma.Identity.Api.Domain.Users;
using Nevma.Identity.Api.Infrastructure.Persistence;
using Nevma.Identity.Api.Infrastructure.Users;

namespace Nevma.Identity.Tests;

public sealed class UserPrivacyServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 15, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task User_can_block_unblock_and_report_another_user()
    {
        await using var context = CreateContext();
        var first = User.Create(Guid.NewGuid(), "First", null, Now);
        var second = User.Create(Guid.NewGuid(), "Second", null, Now);
        context.Profiles.AddRange(first, second);
        await context.SaveChangesAsync();
        var service = CreateService(context);

        Assert.True(await service.BlockAsync(first.Id, second.Id));
        Assert.True(await service.IsBlockedAsync(first.Id, second.Id));
        var report = await service.ReportAsync(
            first.Id,
            second.Id,
            new ReportUserRequest("Abuse", "Repeated unwanted contact"));
        Assert.IsType<ReportUserResult.Accepted>(report);
        Assert.True(await service.UnblockAsync(first.Id, second.Id));
        Assert.False(await service.IsBlockedAsync(first.Id, second.Id));
    }

    [Fact]
    public async Task Settings_validate_and_persist_the_time_zone()
    {
        await using var context = CreateContext();
        var user = User.Create(Guid.NewGuid(), "User", null, Now);
        context.Profiles.Add(user);
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var result = await service.UpdateSettingsAsync(
            user.Id,
            new UpdateUserSettingsRequest("UTC", "el-GR", false));

        var updated = Assert.IsType<SettingsUpdateResult.Updated>(result);
        Assert.Equal("el-GR", updated.Settings.Locale);
        Assert.False(updated.Settings.AllowPresence);
    }

    private static UserPrivacyService CreateService(IdentityDbContext context) =>
        new(
            new EfUserPrivacyRepository(context),
            new EfUserRepository(context),
            context,
            new FixedTimeProvider());

    private static IdentityDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase($"privacy-{Guid.NewGuid():N}")
            .Options);

    private sealed class FixedTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Now;
    }
}
