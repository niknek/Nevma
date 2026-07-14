using Microsoft.EntityFrameworkCore;
using Nevma.Identity.Api.Application.Users;
using Nevma.Identity.Api.Domain.Users;
using Nevma.Identity.Api.Infrastructure.Persistence;
using Nevma.Identity.Api.Infrastructure.Users;

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
        var service = new UserService(new EfUserRepository(context));

        var result = await service.GetByIdAsync(profile.Id);

        Assert.NotNull(result);
        Assert.Equal("Nikos", result.DisplayName);
    }

    private static IdentityDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase($"identity-{Guid.NewGuid():N}")
            .Options;

        return new IdentityDbContext(options);
    }
}
