using Microsoft.EntityFrameworkCore;
using Nevma.Commands.Api.Application;
using Nevma.Commands.Api.Domain;

namespace Nevma.Commands.Api.Infrastructure.Persistence;

public sealed class EfCommandRepository(CommandsDbContext dbContext) : ICommandRepository
{
    public async Task AddAsync(CommandRequest command, CancellationToken cancellationToken = default) =>
        await dbContext.Commands.AddAsync(command, cancellationToken);
    public Task<CommandRequest?> GetAsync(Guid id, Guid userId, CancellationToken cancellationToken = default) =>
        dbContext.Commands.SingleOrDefaultAsync(command => command.Id == id && command.UserId == userId, cancellationToken);
    public Task<CommandRequest?> FindByKeyAsync(Guid userId, string key, CancellationToken cancellationToken = default) =>
        dbContext.Commands.SingleOrDefaultAsync(command => command.UserId == userId && command.IdempotencyKey == key, cancellationToken);
}
