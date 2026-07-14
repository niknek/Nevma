using Nevma.Contracts.Planning;
using DomainPriority = Nevma.Planning.Api.Domain.Tasks.TaskPriority;
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
        var task = await repository.GetAsync(id, ownerId, cancellationToken);
        if (task is null)
            return new CompleteTaskResult.NotFound();
        if (!task.Complete(timeProvider.GetUtcNow()))
            return new CompleteTaskResult.AlreadyCompleted();

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (PlanningConcurrencyException)
        {
            return new CompleteTaskResult.AlreadyCompleted();
        }

        return new CompleteTaskResult.Completed(ToResponse(task));
    }

    private static Dictionary<string, string[]> Validate(CreateTaskRequest request, DateTimeOffset now)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.Title))
            errors[nameof(request.Title)] = ["Title is required."];
        else if (request.Title.Length > 200)
            errors[nameof(request.Title)] = ["Title cannot exceed 200 characters."];
        if (request.Notes?.Length > 4_000)
            errors[nameof(request.Notes)] = ["Notes cannot exceed 4000 characters."];
        if (request.ReminderAt <= now)
            errors[nameof(request.ReminderAt)] = ["Reminder time must be in the future."];
        if (request.DueAt is not null && request.ReminderAt > request.DueAt)
            errors[nameof(request.ReminderAt)] = ["Reminder time cannot be after the due time."];
        return errors;
    }

    private static TaskResponse ToResponse(Domain.Tasks.TaskItem task) =>
        new(
            task.Id,
            task.Title,
            task.Notes,
            task.DueAt,
            (TaskPriority)(int)task.Priority,
            (ContractStatus)(int)task.Status,
            task.ReminderAt,
            task.CreatedAt,
            task.CompletedAt);
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
    public sealed record Completed(TaskResponse Task) : CompleteTaskResult;
    public sealed record NotFound : CompleteTaskResult;
    public sealed record AlreadyCompleted : CompleteTaskResult;
}
