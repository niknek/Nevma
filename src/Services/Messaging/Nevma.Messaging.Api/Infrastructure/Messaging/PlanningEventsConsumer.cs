using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Nevma.Contracts.Integration;
using Nevma.Messaging.Api.Application.Integration;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Nevma.Messaging.Api.Infrastructure.Messaging;

public sealed class PlanningEventsConsumer(
    IServiceScopeFactory scopeFactory,
    IOptions<MessageBrokerOptions> options,
    ILogger<PlanningEventsConsumer> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };
    private readonly MessageBrokerOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(_options.Uri))
            throw new InvalidOperationException("MessageBroker:Uri is required when the broker is enabled.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConsumeAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Planning event consumer disconnected; retrying.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task ConsumeAsync(CancellationToken cancellationToken)
    {
        var factory = new ConnectionFactory
        {
            Uri = new Uri(_options.Uri!, UriKind.Absolute),
            AutomaticRecoveryEnabled = true,
            TopologyRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(5),
            ConsumerDispatchConcurrency = 1
        };
        await using var connection = await factory.CreateConnectionAsync(
            "nevma-messaging-planning-events",
            cancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
        await ConfigureTopologyAsync(channel, cancellationToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, delivery) =>
            await HandleDeliveryAsync(channel, delivery, cancellationToken);
        await channel.BasicConsumeAsync(
            _options.Queue,
            autoAck: false,
            consumer,
            cancellationToken);

        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
    }

    private async Task ConfigureTopologyAsync(
        IChannel channel,
        CancellationToken cancellationToken)
    {
        var deadLetterExchange = $"{_options.Exchange}.dead-letter";
        var deadLetterQueue = $"{_options.Queue}.dead-letter";
        await channel.ExchangeDeclareAsync(
            _options.Exchange,
            ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);
        await channel.ExchangeDeclareAsync(
            deadLetterExchange,
            ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);
        await channel.QueueDeclareAsync(
            deadLetterQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancellationToken);
        await channel.QueueBindAsync(
            deadLetterQueue,
            deadLetterExchange,
            "#",
            cancellationToken: cancellationToken);

        var arguments = new Dictionary<string, object?>
        {
            ["x-queue-type"] = "quorum",
            ["x-dead-letter-exchange"] = deadLetterExchange
        };
        await channel.QueueDeclareAsync(
            _options.Queue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments,
            cancellationToken: cancellationToken);
        await channel.QueueBindAsync(
            _options.Queue,
            _options.Exchange,
            PlanningIntegrationEventTypes.MeetingInvitationChanged,
            cancellationToken: cancellationToken);
        await channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: 10,
            global: false,
            cancellationToken);
    }

    private async Task HandleDeliveryAsync(
        IChannel channel,
        BasicDeliverEventArgs delivery,
        CancellationToken cancellationToken)
    {
        try
        {
            if (delivery.BasicProperties.Type != PlanningIntegrationEventTypes.MeetingInvitationChanged)
                throw new InvalidDataException("Unsupported integration event type.");

            var body = delivery.Body.ToArray();
            var integrationEvent = JsonSerializer.Deserialize<MeetingInvitationChangedIntegrationEvent>(
                body,
                SerializerOptions) ?? throw new InvalidDataException("Integration event payload is empty.");
            if (!string.Equals(
                delivery.BasicProperties.MessageId,
                integrationEvent.EventId.ToString("N"),
                StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("Integration event identifier does not match its envelope.");
            }

            await using var scope = scopeFactory.CreateAsyncScope();
            var handler = scope.ServiceProvider.GetRequiredService<PlanningEventHandler>();
            await handler.HandleAsync(integrationEvent, cancellationToken);
            await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, cancellationToken);
        }
        catch (Exception exception) when (exception is JsonException or InvalidDataException)
        {
            logger.LogWarning(
                exception,
                "Invalid planning event {MessageId} was moved to the dead-letter queue.",
                delivery.BasicProperties.MessageId);
            await channel.BasicRejectAsync(delivery.DeliveryTag, requeue: false, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Planning event {MessageId} failed and will be retried.",
                delivery.BasicProperties.MessageId);
            await channel.BasicNackAsync(
                delivery.DeliveryTag,
                multiple: false,
                requeue: true,
                cancellationToken);
        }
    }
}
