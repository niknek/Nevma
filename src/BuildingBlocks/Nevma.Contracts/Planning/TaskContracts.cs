namespace Nevma.Contracts.Planning;

public sealed record CreateTaskRequest(
    string Title,
    string? Notes,
    DateTimeOffset? DueAt,
    TaskPriority Priority,
    DateTimeOffset? ReminderAt,
    TaskRecurrence Recurrence = TaskRecurrence.None,
    int RecurrenceInterval = 1,
    DateTimeOffset? RecurrenceEndsAt = null);

public sealed record UpdateTaskRequest(
    string Title,
    string? Notes,
    DateTimeOffset? DueAt,
    TaskPriority Priority,
    DateTimeOffset? ReminderAt,
    TaskRecurrence Recurrence = TaskRecurrence.None,
    int RecurrenceInterval = 1,
    DateTimeOffset? RecurrenceEndsAt = null);

public sealed record TaskResponse(
    Guid Id,
    string Title,
    string? Notes,
    DateTimeOffset? DueAt,
    TaskPriority Priority,
    TaskStatus Status,
    DateTimeOffset? ReminderAt,
    TaskRecurrence Recurrence,
    int RecurrenceInterval,
    DateTimeOffset? RecurrenceEndsAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt,
    DateTimeOffset? UpdatedAt);

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

public enum TaskFilter
{
    All,
    Urgent,
    Today,
    Upcoming,
    Completed
}
