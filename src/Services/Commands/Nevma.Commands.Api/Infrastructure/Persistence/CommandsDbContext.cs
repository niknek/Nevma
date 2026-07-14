using Microsoft.EntityFrameworkCore;
using Nevma.Commands.Api.Application;
using Nevma.Commands.Api.Domain;

namespace Nevma.Commands.Api.Infrastructure.Persistence;

public sealed class CommandsDbContext(DbContextOptions<CommandsDbContext> options)
    : DbContext(options), ICommandsUnitOfWork
{
    public DbSet<CommandRequest> Commands => Set<CommandRequest>();
    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema("commands");
        builder.ApplyConfigurationsFromAssembly(typeof(CommandsDbContext).Assembly);
    }
}
