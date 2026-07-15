using Nevma.Planning.Api.Domain.Tasks;

namespace Nevma.Planning.Api.Application.Tasks;

public interface ITaskShareRepository
{
    Task<TaskShare?> GetAsync(Guid taskId, Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(TaskShare share, CancellationToken cancellationToken = default);
    void Remove(TaskShare share);
    Task<IReadOnlyList<TaskShare>> ListAsync(Guid taskId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SharedTaskAccess>> ListSharedAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}

public sealed record SharedTaskAccess(TaskItem Task, TaskShare Share);
