using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Nevma.Identity.Api.Domain.Users;
using OpenIddict.EntityFrameworkCore;

namespace Nevma.Identity.Api.Infrastructure.Persistence;

public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options)
    : IdentityDbContext<IdentityAccount, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<User> Profiles => Set<User>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema("identity");
        base.OnModelCreating(builder);
        builder.UseOpenIddict();
        builder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);
    }
}
