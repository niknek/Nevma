namespace Nevma.Contracts.Planning;

public sealed record CreateTaskRequest(
    string Title,
    string? Notes,
    DateTimeOffset? DueAt,
    TaskPriority Priority,
    DateTimeOffset? ReminderAt);

public sealed record TaskResponse(
    Guid Id,
    string Title,
    string? Notes,
    DateTimeOffset? DueAt,
    TaskPriority Priority,
    TaskStatus Status,
    DateTimeOffset? ReminderAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt);

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

public enum TaskFilter
{
    All,
    Urgent,
    Today,
    Upcoming,
    Completed
}
