using Microsoft.EntityFrameworkCore;
using Nevma.Contracts.Identity;
using Nevma.Identity.Api.Application.Users;
using Nevma.Identity.Api.Infrastructure.Persistence;
using Nevma.Identity.Api.Infrastructure.Users;

namespace Nevma.Identity.Tests;

public sealed class UserServiceTests
{
    [Fact]
    public async Task Create_persists_a_trimmed_user_profile()
    {
        await using var context = CreateContext();
        var repository = new EfUserRepository(context);
        var service = new UserService(repository, context, TimeProvider.System);

        var result = await service.CreateAsync(new CreateUserRequest("  Nikos  ", null));

        Assert.True(result.IsSuccess);
        Assert.Equal("Nikos", result.User!.DisplayName);
        Assert.Equal(1, await context.Profiles.CountAsync());
    }

    [Fact]
    public async Task Empty_display_name_is_rejected_without_a_database_write()
    {
        await using var context = CreateContext();
        var repository = new EfUserRepository(context);
        var service = new UserService(repository, context, TimeProvider.System);

        var result = await service.CreateAsync(new CreateUserRequest("  ", null));

        Assert.False(result.IsSuccess);
        Assert.Empty(context.ChangeTracker.Entries());
        Assert.Empty(context.Profiles);
    }

    private static IdentityDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase($"identity-{Guid.NewGuid():N}")
            .Options;

        return new IdentityDbContext(options);
    }
}
