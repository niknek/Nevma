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
        TaskRecurrence recurrence,
        int recurrenceInterval,
        DateTimeOffset? recurrenceEndsAt,
        DateTimeOffset createdAt,
        DateTimeOffset? completedAt,
        DateTimeOffset? updatedAt,
        DateTimeOffset? reminderDispatchedAt,
        DateTimeOffset? deletedAt)
    {
        Id = id;
        OwnerId = ownerId;
        Title = title;
        Notes = notes;
        DueAt = dueAt;
        Priority = priority;
        Status = status;
        ReminderAt = reminderAt;
        Recurrence = recurrence;
        RecurrenceInterval = recurrenceInterval;
        RecurrenceEndsAt = recurrenceEndsAt;
        CreatedAt = createdAt;
        CompletedAt = completedAt;
        UpdatedAt = updatedAt;
        ReminderDispatchedAt = reminderDispatchedAt;
        DeletedAt = deletedAt;
    }

    public Guid Id { get; }
    public Guid OwnerId { get; }
    public string Title { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset? DueAt { get; private set; }
    public TaskPriority Priority { get; private set; }
    public TaskStatus Status { get; private set; }
    public DateTimeOffset? ReminderAt { get; private set; }
    public TaskRecurrence Recurrence { get; private set; }
    public int RecurrenceInterval { get; private set; }
    public DateTimeOffset? RecurrenceEndsAt { get; private set; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public DateTimeOffset? ReminderDispatchedAt { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    public static TaskItem Create(
        Guid ownerId,
        string title,
        string? notes,
        DateTimeOffset? dueAt,
        TaskPriority priority,
        DateTimeOffset? reminderAt,
        TaskRecurrence recurrence,
        int recurrenceInterval,
        DateTimeOffset? recurrenceEndsAt,
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
            recurrence,
            recurrenceInterval,
            recurrenceEndsAt,
            createdAt,
            null,
            null,
            null,
            null);

    public void Update(
        string title,
        string? notes,
        DateTimeOffset? dueAt,
        TaskPriority priority,
        DateTimeOffset? reminderAt,
        TaskRecurrence recurrence,
        int recurrenceInterval,
        DateTimeOffset? recurrenceEndsAt,
        DateTimeOffset updatedAt)
    {
        Title = title.Trim();
        Notes = notes?.Trim();
        DueAt = dueAt;
        Priority = priority;
        ReminderAt = reminderAt;
        Recurrence = recurrence;
        RecurrenceInterval = recurrenceInterval;
        RecurrenceEndsAt = recurrenceEndsAt;
        ReminderDispatchedAt = null;
        UpdatedAt = updatedAt;
    }

    public bool Complete(DateTimeOffset completedAt)
    {
        if (Status == TaskStatus.Completed)
            return false;

        Status = TaskStatus.Completed;
        CompletedAt = completedAt;
        UpdatedAt = completedAt;
        return true;
    }

    public void MarkReminderDispatched(DateTimeOffset dispatchedAt) =>
        ReminderDispatchedAt = dispatchedAt;

    public bool Delete(DateTimeOffset deletedAt)
    {
        if (DeletedAt is not null)
            return false;
        DeletedAt = deletedAt;
        UpdatedAt = deletedAt;
        return true;
    }

    public TaskItem? CreateNextOccurrence(DateTimeOffset now)
    {
        if (Recurrence == TaskRecurrence.None || DueAt is null)
            return null;

        var nextDueAt = Recurrence switch
        {
            TaskRecurrence.Daily => DueAt.Value.AddDays(RecurrenceInterval),
            TaskRecurrence.Weekly => DueAt.Value.AddDays(7 * RecurrenceInterval),
            TaskRecurrence.Monthly => DueAt.Value.AddMonths(RecurrenceInterval),
            _ => DueAt.Value
        };
        if (RecurrenceEndsAt is not null && nextDueAt > RecurrenceEndsAt)
            return null;

        var reminderLead = DueAt.Value - ReminderAt;
        var nextReminderAt = ReminderAt is null ? null : nextDueAt - reminderLead;
        return Create(
            OwnerId,
            Title,
            Notes,
            nextDueAt,
            Priority,
            nextReminderAt,
            Recurrence,
            RecurrenceInterval,
            RecurrenceEndsAt,
            now);
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

public enum TaskRecurrence
{
    None,
    Daily,
    Weekly,
    Monthly
}
