using Microsoft.EntityFrameworkCore;
using Nevma.Identity.Api.Domain.Users;
using Nevma.Identity.Api.Infrastructure.Persistence;
using Nevma.Identity.Api.Infrastructure.Users;

namespace Nevma.Identity.Tests;

public sealed class EfUserRepositoryTests
{
    [Fact]
    public async Task User_is_persisted_and_loaded_by_id()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase($"identity-{Guid.NewGuid():N}")
            .Options;

        Guid userId;
        await using (var writeContext = new IdentityDbContext(options))
        {
            var repository = new EfUserRepository(writeContext);
            var user = User.Create(Guid.NewGuid(), "Nikos", null, DateTimeOffset.UtcNow);
            userId = user.Id;

            await repository.AddAsync(user);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = new IdentityDbContext(options);
        var persistedUser = await new EfUserRepository(readContext).GetByIdAsync(userId);

        Assert.NotNull(persistedUser);
        Assert.Equal("Nikos", persistedUser.DisplayName);
    }
}
