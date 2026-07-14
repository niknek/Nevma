using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Nevma.Identity.Api.Application;
using Nevma.Identity.Api.Domain.Users;

namespace Nevma.Identity.Api.Infrastructure.Persistence;

public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options)
    : IdentityDbContext<IdentityAccount, IdentityRole<Guid>, Guid>(options), IIdentityUnitOfWork
{
    public DbSet<User> Profiles => Set<User>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema("identity");
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);
    }
}
