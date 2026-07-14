using Microsoft.EntityFrameworkCore;
using Nevma.Identity.Api.Application.Users;
using Nevma.Identity.Api.Domain.Users;
using Nevma.Identity.Api.Infrastructure.Persistence;
using Nevma.Identity.Api.Infrastructure.Users;
using Nevma.Contracts.Identity;

namespace Nevma.Identity.Tests;

public sealed class UserServiceTests
{
    [Fact]
    public async Task Get_returns_the_persisted_public_profile()
    {
        await using var context = CreateContext();
        var profile = User.Create(Guid.NewGuid(), "Nikos", null, DateTimeOffset.UtcNow);
        context.Profiles.Add(profile);
        await context.SaveChangesAsync();
        var service = new UserService(new EfUserRepository(context), context);

        var result = await service.GetByIdAsync(profile.Id);

        Assert.NotNull(result);
        Assert.Equal("Nikos", result.DisplayName);
    }

    [Fact]
    public async Task User_can_update_their_public_profile()
    {
        await using var context = CreateContext();
        var profile = User.Create(Guid.NewGuid(), "Old", null, DateTimeOffset.UtcNow);
        context.Profiles.Add(profile);
        await context.SaveChangesAsync();
        var service = new UserService(new EfUserRepository(context), context);

        var result = await service.UpdateAsync(
            profile.Id,
            new UpdateProfileRequest("New name", "https://cdn.nevma.test/avatar.jpg"));

        var updated = Assert.IsType<UpdateProfileResult.Updated>(result);
        Assert.Equal("New name", updated.User.DisplayName);
        Assert.Equal("https://cdn.nevma.test/avatar.jpg", updated.User.AvatarUrl);
    }

    private static IdentityDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase($"identity-{Guid.NewGuid():N}")
            .Options;

        return new IdentityDbContext(options);
    }
}
