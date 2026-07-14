using Nevma.Planning.Api.Application.Tasks;

namespace Nevma.Planning.Api.Infrastructure.Tasks;

public sealed class TaskReminderWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<TaskReminderWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        do
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var processor = scope.ServiceProvider.GetRequiredService<TaskReminderProcessor>();
                var processed = await processor.ProcessDueAsync(cancellationToken: stoppingToken);
                if (processed > 0)
                    logger.LogInformation("Queued {ReminderCount} task reminders.", processed);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Task reminder scan failed; it will be retried.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
