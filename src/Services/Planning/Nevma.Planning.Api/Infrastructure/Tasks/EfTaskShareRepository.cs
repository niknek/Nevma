using Microsoft.EntityFrameworkCore;
using Nevma.Planning.Api.Application.Tasks;
using Nevma.Planning.Api.Domain.Tasks;
using Nevma.Planning.Api.Infrastructure.Persistence;

namespace Nevma.Planning.Api.Infrastructure.Tasks;

public sealed class EfTaskShareRepository(PlanningDbContext dbContext) : ITaskShareRepository
{
    public Task<TaskShare?> GetAsync(
        Guid taskId,
        Guid userId,
        CancellationToken cancellationToken = default) =>
        dbContext.TaskShares.SingleOrDefaultAsync(
            item => item.TaskId == taskId && item.UserId == userId,
            cancellationToken);

    public async Task AddAsync(TaskShare share, CancellationToken cancellationToken = default) =>
        await dbContext.TaskShares.AddAsync(share, cancellationToken);

    public void Remove(TaskShare share) => dbContext.TaskShares.Remove(share);

    public async Task<IReadOnlyList<TaskShare>> ListAsync(
        Guid taskId,
        CancellationToken cancellationToken = default) =>
        await dbContext.TaskShares
            .AsNoTracking()
            .Where(item => item.TaskId == taskId)
            .OrderBy(item => item.SharedAt)
            .ToArrayAsync(cancellationToken);

    public async Task<IReadOnlyList<SharedTaskAccess>> ListSharedAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var rows = await (
            from share in dbContext.TaskShares.AsNoTracking()
            join task in dbContext.Tasks.AsNoTracking() on share.TaskId equals task.Id
            where share.UserId == userId && task.DeletedAt == null
            orderby task.Status, task.DueAt
            select new { Task = task, Share = share })
            .ToArrayAsync(cancellationToken);
        return rows.Select(row => new SharedTaskAccess(row.Task, row.Share)).ToArray();
    }
}
