using Microsoft.EntityFrameworkCore;
using Nevma.Contracts.Planning;
using Nevma.Planning.Api.Application.Tasks;
using Nevma.Planning.Api.Domain.Tasks;
using Nevma.Planning.Api.Infrastructure.Persistence;
using DomainStatus = Nevma.Planning.Api.Domain.Tasks.TaskStatus;
using DomainPriority = Nevma.Planning.Api.Domain.Tasks.TaskPriority;

namespace Nevma.Planning.Api.Infrastructure.Tasks;

public sealed class EfTaskRepository(PlanningDbContext dbContext) : ITaskRepository
{
    public async Task AddAsync(TaskItem task, CancellationToken cancellationToken = default) =>
        await dbContext.Tasks.AddAsync(task, cancellationToken);

    public void Remove(TaskItem task) => dbContext.Tasks.Remove(task);

    public Task<TaskItem?> GetAsync(
        Guid id,
        Guid ownerId,
        CancellationToken cancellationToken = default) =>
        dbContext.Tasks.SingleOrDefaultAsync(
            task => task.Id == id && task.OwnerId == ownerId && task.DeletedAt == null,
            cancellationToken);

    public Task<TaskItem?> GetAccessibleAsync(
        Guid id,
        Guid userId,
        bool requireEdit,
        CancellationToken cancellationToken = default) =>
        dbContext.Tasks.SingleOrDefaultAsync(task =>
            task.Id == id && task.DeletedAt == null &&
            (task.OwnerId == userId || dbContext.TaskShares.Any(share =>
                share.TaskId == task.Id && share.UserId == userId && (!requireEdit || share.CanEdit))),
            cancellationToken);

    public async Task<IReadOnlyList<TaskItem>> ListAsync(
        Guid ownerId,
        TaskFilter filter,
        DateTimeOffset dayStart,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Tasks.AsNoTracking().Where(task =>
            task.OwnerId == ownerId && task.DeletedAt == null);
        query = filter switch
        {
            TaskFilter.Urgent => query.Where(task =>
                task.Status == DomainStatus.Active && task.Priority == DomainPriority.Urgent),
            TaskFilter.Today => query.Where(task =>
                task.Status == DomainStatus.Active &&
                task.DueAt >= dayStart && task.DueAt < dayStart.AddDays(1)),
            TaskFilter.Upcoming => query.Where(task =>
                task.Status == DomainStatus.Active && task.DueAt >= dayStart.AddDays(1)),
            TaskFilter.Completed => query.Where(task => task.Status == DomainStatus.Completed),
            _ => query
        };

        return await query
            .OrderBy(task => task.Status)
            .ThenByDescending(task => task.Priority)
            .ThenBy(task => task.DueAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TaskItem>> ListDueRemindersAsync(
        DateTimeOffset now,
        int batchSize,
        CancellationToken cancellationToken = default) =>
        await dbContext.Tasks
            .Where(task =>
                task.DeletedAt == null &&
                task.Status == DomainStatus.Active &&
                task.ReminderAt != null &&
                task.ReminderAt <= now &&
                task.ReminderDispatchedAt == null)
            .OrderBy(task => task.ReminderAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
}
