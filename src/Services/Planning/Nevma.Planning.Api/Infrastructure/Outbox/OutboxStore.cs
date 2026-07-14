using Microsoft.EntityFrameworkCore;
using Nevma.Planning.Api.Infrastructure.Persistence;

namespace Nevma.Planning.Api.Infrastructure.Outbox;

public sealed class OutboxStore(PlanningDbContext dbContext)
{
    public async Task<IReadOnlyList<OutboxMessage>> ClaimBatchAsync(
        DateTimeOffset now,
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        var candidateIds = await dbContext.OutboxMessages
            .AsNoTracking()
            .Where(message =>
                message.ProcessedAt == null &&
                message.NextAttemptAt <= now &&
                (message.LockedUntil == null || message.LockedUntil <= now))
            .OrderBy(message => message.OccurredAt)
            .Select(message => message.Id)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
        if (candidateIds.Count == 0)
            return [];

        var claimId = Guid.NewGuid();
        var lockedUntil = now.AddMinutes(1);
        foreach (var candidateId in candidateIds)
        {
            await dbContext.OutboxMessages
                .Where(message =>
                    message.Id == candidateId &&
                    message.ProcessedAt == null &&
                    message.NextAttemptAt <= now &&
                    (message.LockedUntil == null || message.LockedUntil <= now))
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(message => message.LockId, claimId)
                        .SetProperty(message => message.LockedUntil, lockedUntil),
                    cancellationToken);
        }

        return await dbContext.OutboxMessages
            .Where(message => message.LockId == claimId)
            .OrderBy(message => message.OccurredAt)
            .ToListAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
