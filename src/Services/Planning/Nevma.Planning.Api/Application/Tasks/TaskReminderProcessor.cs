using Nevma.Contracts.Integration;

namespace Nevma.Planning.Api.Application.Tasks;

public sealed class TaskReminderProcessor(
    ITaskRepository repository,
    IPlanningEventOutbox outbox,
    IPlanningUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<int> ProcessDueAsync(
        int batchSize = 100,
        CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        var tasks = await repository.ListDueRemindersAsync(now, batchSize, cancellationToken);
        foreach (var task in tasks)
        {
            task.MarkReminderDispatched(now);
            outbox.Add(new TaskReminderDueIntegrationEvent(
                Guid.NewGuid(),
                task.Id,
                task.OwnerId,
                task.Title,
                task.DueAt,
                now));
        }

        if (tasks.Count == 0)
            return 0;

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return tasks.Count;
        }
        catch (PlanningConcurrencyException)
        {
            return 0;
        }
    }
}
