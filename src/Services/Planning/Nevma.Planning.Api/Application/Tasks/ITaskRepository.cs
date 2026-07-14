using Nevma.Contracts.Planning;
using Nevma.Planning.Api.Domain.Tasks;

namespace Nevma.Planning.Api.Application.Tasks;

public interface ITaskRepository
{
    Task AddAsync(TaskItem task, CancellationToken cancellationToken = default);
    void Remove(TaskItem task);
    Task<TaskItem?> GetAsync(Guid id, Guid ownerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaskItem>> ListAsync(
        Guid ownerId,
        TaskFilter filter,
        DateTimeOffset dayStart,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaskItem>> ListDueRemindersAsync(
        DateTimeOffset now,
        int batchSize,
        CancellationToken cancellationToken = default);
}
