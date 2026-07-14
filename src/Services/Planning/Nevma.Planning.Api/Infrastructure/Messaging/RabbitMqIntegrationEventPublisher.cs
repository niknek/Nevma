using System.Text;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Nevma.Planning.Api.Infrastructure.Messaging;

public sealed class RabbitMqIntegrationEventPublisher : IIntegrationEventPublisher, IAsyncDisposable
{
    private readonly MessageBrokerOptions _options;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private IConnection? _connection;
    private IChannel? _channel;

    public RabbitMqIntegrationEventPublisher(IOptions<MessageBrokerOptions> options)
    {
        _options = options.Value;
        if (string.IsNullOrWhiteSpace(_options.Uri))
            throw new InvalidOperationException("MessageBroker:Uri is required when the broker is enabled.");
    }

    public async Task PublishAsync(
        Guid eventId,
        string type,
        string payload,
        CancellationToken cancellationToken = default)
    {
        var channel = await GetChannelAsync(cancellationToken);
        var properties = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent,
            MessageId = eventId.ToString("N"),
            Type = type
        };
        await channel.BasicPublishAsync(
            _options.Exchange,
            type,
            mandatory: true,
            basicProperties: properties,
            body: Encoding.UTF8.GetBytes(payload),
            cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
            await _channel.DisposeAsync();
        if (_connection is not null)
            await _connection.DisposeAsync();
        _connectionLock.Dispose();
    }

    private async Task<IChannel> GetChannelAsync(CancellationToken cancellationToken)
    {
        if (_channel is { IsOpen: true })
            return _channel;

        await _connectionLock.WaitAsync(cancellationToken);
        try
        {
            if (_channel is { IsOpen: true })
                return _channel;

            var factory = new ConnectionFactory
            {
                Uri = new Uri(_options.Uri!, UriKind.Absolute),
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(5)
            };
            _connection = await factory.CreateConnectionAsync(
                "nevma-planning-outbox",
                cancellationToken);
            _channel = await _connection.CreateChannelAsync(
                new CreateChannelOptions(
                    publisherConfirmationsEnabled: true,
                    publisherConfirmationTrackingEnabled: true),
                cancellationToken);
            await _channel.ExchangeDeclareAsync(
                _options.Exchange,
                ExchangeType.Topic,
                durable: true,
                autoDelete: false,
                cancellationToken: cancellationToken);
            return _channel;
        }
        finally
        {
            _connectionLock.Release();
        }
    }
}
