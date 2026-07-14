namespace Nevma.Planning.Api.Domain.Tasks;

public sealed class TaskItem
{
    private TaskItem(
        Guid id,
        Guid ownerId,
        string title,
        string? notes,
        DateTimeOffset? dueAt,
        TaskPriority priority,
        TaskStatus status,
        DateTimeOffset? reminderAt,
        DateTimeOffset createdAt,
        DateTimeOffset? completedAt)
    {
        Id = id;
        OwnerId = ownerId;
        Title = title;
        Notes = notes;
        DueAt = dueAt;
        Priority = priority;
        Status = status;
        ReminderAt = reminderAt;
        CreatedAt = createdAt;
        CompletedAt = completedAt;
    }

    public Guid Id { get; }
    public Guid OwnerId { get; }
    public string Title { get; }
    public string? Notes { get; }
    public DateTimeOffset? DueAt { get; }
    public TaskPriority Priority { get; }
    public TaskStatus Status { get; private set; }
    public DateTimeOffset? ReminderAt { get; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset? CompletedAt { get; private set; }

    public static TaskItem Create(
        Guid ownerId,
        string title,
        string? notes,
        DateTimeOffset? dueAt,
        TaskPriority priority,
        DateTimeOffset? reminderAt,
        DateTimeOffset createdAt) =>
        new(
            Guid.NewGuid(),
            ownerId,
            title.Trim(),
            notes?.Trim(),
            dueAt,
            priority,
            TaskStatus.Active,
            reminderAt,
            createdAt,
            null);

    public bool Complete(DateTimeOffset completedAt)
    {
        if (Status == TaskStatus.Completed)
            return false;

        Status = TaskStatus.Completed;
        CompletedAt = completedAt;
        return true;
    }
}

public enum TaskPriority
{
    Low,
    Normal,
    High,
    Urgent
}

public enum TaskStatus
{
    Active,
    Completed
}
