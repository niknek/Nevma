using Nevma.Contracts.Planning;
using Nevma.Planning.Api.Domain.Tasks;

namespace Nevma.Planning.Api.Application.Tasks;

public sealed class TaskSharingService(
    ITaskRepository taskRepository,
    ITaskShareRepository shareRepository,
    IPlanningUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<TaskShareResult> ShareAsync(
        Guid taskId,
        Guid ownerId,
        ShareTaskRequest request,
        CancellationToken cancellationToken = default)
    {
        var task = await taskRepository.GetAsync(taskId, ownerId, cancellationToken);
        if (task is null)
            return new TaskShareResult.NotFound();
        if (request.UserId == Guid.Empty || request.UserId == ownerId)
            return new TaskShareResult.Invalid("A task can only be shared with another user.");

        var share = await shareRepository.GetAsync(taskId, request.UserId, cancellationToken);
        if (share is null)
        {
            share = TaskShare.Create(taskId, request.UserId, request.CanEdit, timeProvider.GetUtcNow());
            await shareRepository.AddAsync(share, cancellationToken);
        }
        else
        {
            share.ChangePermission(request.CanEdit);
        }
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new TaskShareResult.Changed(ToResponse(share));
    }

    public async Task<IReadOnlyList<TaskShareResponse>?> ListCollaboratorsAsync(
        Guid taskId,
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        if (await taskRepository.GetAsync(taskId, ownerId, cancellationToken) is null)
            return null;
        return (await shareRepository.ListAsync(taskId, cancellationToken)).Select(ToResponse).ToArray();
    }

    public async Task<IReadOnlyList<SharedTaskResponse>> ListSharedAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        (await shareRepository.ListSharedAsync(userId, cancellationToken))
            .Select(access => new SharedTaskResponse(
                TaskService.ToResponse(access.Task),
                access.Task.OwnerId,
                access.Share.CanEdit,
                access.Share.SharedAt))
            .ToArray();

    public async Task<bool> RevokeAsync(
        Guid taskId,
        Guid ownerId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (await taskRepository.GetAsync(taskId, ownerId, cancellationToken) is null)
            return false;
        var share = await shareRepository.GetAsync(taskId, userId, cancellationToken);
        if (share is null)
            return false;
        shareRepository.Remove(share);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static TaskShareResponse ToResponse(TaskShare share) =>
        new(share.UserId, share.CanEdit, share.SharedAt);
}

public abstract record TaskShareResult
{
    public sealed record Changed(TaskShareResponse Share) : TaskShareResult;
    public sealed record Invalid(string Message) : TaskShareResult;
    public sealed record NotFound : TaskShareResult;
}
