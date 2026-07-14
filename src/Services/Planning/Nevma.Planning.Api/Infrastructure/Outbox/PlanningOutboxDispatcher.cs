using Nevma.Planning.Api.Infrastructure.Messaging;

namespace Nevma.Planning.Api.Infrastructure.Outbox;

public sealed class PlanningOutboxDispatcher(
    IServiceScopeFactory scopeFactory,
    IIntegrationEventPublisher publisher,
    TimeProvider timeProvider,
    ILogger<PlanningOutboxDispatcher> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var processedAny = false;
            try
            {
                processedAny = await ProcessBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Planning outbox batch processing failed.");
            }

            await Task.Delay(
                processedAny ? TimeSpan.FromMilliseconds(100) : TimeSpan.FromSeconds(1),
                stoppingToken);
        }
    }

    private async Task<bool> ProcessBatchAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<OutboxStore>();
        var messages = await store.ClaimBatchAsync(
            timeProvider.GetUtcNow(),
            batchSize: 20,
            cancellationToken);

        foreach (var message in messages)
        {
            try
            {
                await publisher.PublishAsync(
                    message.Id,
                    message.Type,
                    message.Payload,
                    cancellationToken);
                message.MarkProcessed(timeProvider.GetUtcNow());
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                message.MarkFailed(exception.GetType().Name, timeProvider.GetUtcNow());
                logger.LogWarning(
                    exception,
                    "Planning integration event {EventId} could not be published.",
                    message.Id);
            }

            await store.SaveChangesAsync(cancellationToken);
        }

        return messages.Count > 0;
    }
}
