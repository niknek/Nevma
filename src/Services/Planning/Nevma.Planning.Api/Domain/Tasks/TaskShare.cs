namespace Nevma.Planning.Api.Domain.Tasks;

public sealed class TaskShare
{
    private TaskShare() { }

    private TaskShare(Guid taskId, Guid userId, bool canEdit, DateTimeOffset sharedAt)
    {
        TaskId = taskId;
        UserId = userId;
        CanEdit = canEdit;
        SharedAt = sharedAt;
    }

    public Guid TaskId { get; private set; }
    public Guid UserId { get; private set; }
    public bool CanEdit { get; private set; }
    public DateTimeOffset SharedAt { get; private set; }

    public static TaskShare Create(Guid taskId, Guid userId, bool canEdit, DateTimeOffset sharedAt)
    {
        if (taskId == Guid.Empty || userId == Guid.Empty)
            throw new ArgumentException("Task and user identifiers are required.");
        return new TaskShare(taskId, userId, canEdit, sharedAt);
    }

    public void ChangePermission(bool canEdit) => CanEdit = canEdit;
}
