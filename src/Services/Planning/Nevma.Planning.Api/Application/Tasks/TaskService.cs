using Nevma.Contracts.Planning;
using DomainPriority = Nevma.Planning.Api.Domain.Tasks.TaskPriority;
using DomainRecurrence = Nevma.Planning.Api.Domain.Tasks.TaskRecurrence;
using DomainStatus = Nevma.Planning.Api.Domain.Tasks.TaskStatus;
using ContractStatus = Nevma.Contracts.Planning.TaskStatus;

namespace Nevma.Planning.Api.Application.Tasks;

public sealed class TaskService(
    ITaskRepository repository,
    IPlanningUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<CreateTaskResult> CreateAsync(
        Guid ownerId,
        CreateTaskRequest request,
        CancellationToken cancellationToken = default)
    {
        var errors = Validate(request, timeProvider.GetUtcNow());
        if (errors.Count > 0)
            return CreateTaskResult.Failure(errors);

        var task = Domain.Tasks.TaskItem.Create(
            ownerId,
            request.Title,
            request.Notes,
            request.DueAt,
            (DomainPriority)(int)request.Priority,
            request.ReminderAt,
            (DomainRecurrence)(int)request.Recurrence,
            request.RecurrenceInterval,
            request.RecurrenceEndsAt,
            timeProvider.GetUtcNow());
        await repository.AddAsync(task, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return CreateTaskResult.Success(ToResponse(task));
    }

    public async Task<IReadOnlyList<TaskResponse>> ListAsync(
        Guid ownerId,
        TaskFilter filter,
        DateTimeOffset dayStart,
        CancellationToken cancellationToken = default)
    {
        var tasks = await repository.ListAsync(ownerId, filter, dayStart, cancellationToken);
        return tasks.Select(ToResponse).ToArray();
    }

    public async Task<CompleteTaskResult> CompleteAsync(
        Guid id,
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        var task = await repository.GetAccessibleAsync(id, ownerId, requireEdit: true, cancellationToken);
        if (task is null)
            return new CompleteTaskResult.NotFound();
        if (!task.Complete(timeProvider.GetUtcNow()))
            return new CompleteTaskResult.AlreadyCompleted();

        var nextOccurrence = task.CreateNextOccurrence(timeProvider.GetUtcNow());
        if (nextOccurrence is not null)
            await repository.AddAsync(nextOccurrence, cancellationToken);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (PlanningConcurrencyException)
        {
            return new CompleteTaskResult.AlreadyCompleted();
        }

        return new CompleteTaskResult.Completed(
            ToResponse(task),
            nextOccurrence is null ? null : ToResponse(nextOccurrence));
    }

    public async Task<ChangeTaskResult> UpdateAsync(
        Guid id,
        Guid ownerId,
        UpdateTaskRequest request,
        CancellationToken cancellationToken = default)
    {
        var errors = Validate(
            request.Title,
            request.Notes,
            request.DueAt,
            request.ReminderAt,
            request.Recurrence,
            request.RecurrenceInterval,
            request.RecurrenceEndsAt,
            timeProvider.GetUtcNow());
        if (errors.Count > 0)
            return new ChangeTaskResult.ValidationFailed(errors);

        var task = await repository.GetAccessibleAsync(id, ownerId, requireEdit: true, cancellationToken);
        if (task is null)
            return new ChangeTaskResult.NotFound();
        if (task.Status == DomainStatus.Completed)
            return new ChangeTaskResult.Conflict("Completed tasks cannot be edited.");

        task.Update(
            request.Title,
            request.Notes,
            request.DueAt,
            (DomainPriority)(int)request.Priority,
            request.ReminderAt,
            (DomainRecurrence)(int)request.Recurrence,
            request.RecurrenceInterval,
            request.RecurrenceEndsAt,
            timeProvider.GetUtcNow());
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (PlanningConcurrencyException)
        {
            return new ChangeTaskResult.Conflict("Task changed during this update.");
        }
        return new ChangeTaskResult.Updated(ToResponse(task));
    }

    public async Task<DeleteTaskResult> DeleteAsync(
        Guid id,
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        var task = await repository.GetAsync(id, ownerId, cancellationToken);
        if (task is null)
            return DeleteTaskResult.NotFound;
        if (!task.Delete(timeProvider.GetUtcNow()))
            return DeleteTaskResult.NotFound;

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (PlanningConcurrencyException)
        {
            return DeleteTaskResult.Conflict;
        }
        return DeleteTaskResult.Deleted;
    }

    private static Dictionary<string, string[]> Validate(CreateTaskRequest request, DateTimeOffset now) =>
        Validate(
            request.Title,
            request.Notes,
            request.DueAt,
            request.ReminderAt,
            request.Recurrence,
            request.RecurrenceInterval,
            request.RecurrenceEndsAt,
            now);

    private static Dictionary<string, string[]> Validate(
        string title,
        string? notes,
        DateTimeOffset? dueAt,
        DateTimeOffset? reminderAt,
        TaskRecurrence recurrence,
        int recurrenceInterval,
        DateTimeOffset? recurrenceEndsAt,
        DateTimeOffset now)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(title))
            errors[nameof(CreateTaskRequest.Title)] = ["Title is required."];
        else if (title.Length > 200)
            errors[nameof(CreateTaskRequest.Title)] = ["Title cannot exceed 200 characters."];
        if (notes?.Length > 4_000)
            errors[nameof(CreateTaskRequest.Notes)] = ["Notes cannot exceed 4000 characters."];
        if (reminderAt <= now)
            errors[nameof(CreateTaskRequest.ReminderAt)] = ["Reminder time must be in the future."];
        if (dueAt is not null && reminderAt > dueAt)
            errors[nameof(CreateTaskRequest.ReminderAt)] = ["Reminder time cannot be after the due time."];
        if (recurrence != TaskRecurrence.None && dueAt is null)
            errors[nameof(CreateTaskRequest.DueAt)] = ["A due time is required for recurring tasks."];
        if (recurrenceInterval is < 1 or > 365)
            errors[nameof(CreateTaskRequest.RecurrenceInterval)] = ["Recurrence interval must be between 1 and 365."];
        if (recurrenceEndsAt is not null && dueAt is not null && recurrenceEndsAt < dueAt)
            errors[nameof(CreateTaskRequest.RecurrenceEndsAt)] = ["Recurrence end cannot be before the first due time."];
        return errors;
    }

    internal static TaskResponse ToResponse(Domain.Tasks.TaskItem task) =>
        new(
            task.Id,
            task.Title,
            task.Notes,
            task.DueAt,
            (TaskPriority)(int)task.Priority,
            (ContractStatus)(int)task.Status,
            task.ReminderAt,
            (TaskRecurrence)(int)task.Recurrence,
            task.RecurrenceInterval,
            task.RecurrenceEndsAt,
            task.CreatedAt,
            task.CompletedAt,
            task.UpdatedAt);
}

public sealed record CreateTaskResult(
    TaskResponse? Task,
    IReadOnlyDictionary<string, string[]> Errors)
{
    public bool IsSuccess => Task is not null;

    public static CreateTaskResult Success(TaskResponse task) =>
        new(task, new Dictionary<string, string[]>());

    public static CreateTaskResult Failure(IReadOnlyDictionary<string, string[]> errors) =>
        new(null, errors);
}

public abstract record CompleteTaskResult
{
    public sealed record Completed(TaskResponse Task, TaskResponse? NextOccurrence) : CompleteTaskResult;
    public sealed record NotFound : CompleteTaskResult;
    public sealed record AlreadyCompleted : CompleteTaskResult;
}

public abstract record ChangeTaskResult
{
    public sealed record Updated(TaskResponse Task) : ChangeTaskResult;
    public sealed record NotFound : ChangeTaskResult;
    public sealed record Conflict(string Message) : ChangeTaskResult;
    public sealed record ValidationFailed(IReadOnlyDictionary<string, string[]> Errors) : ChangeTaskResult;
}

public enum DeleteTaskResult
{
    Deleted,
    NotFound,
    Conflict
}
