using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SqlDbEntityNotifier.Core.Interfaces;
using SqlDbEntityNotifier.Core.Models;
using SqlDbEntityNotifier.Publisher.RabbitMQ.Models;

namespace SqlDbEntityNotifier.Publisher.RabbitMQ;

/// <summary>
/// RabbitMQ publisher implementation for change events.
/// </summary>
public class RabbitMQChangePublisher : IChangePublisher, IDisposable
{
    private readonly RabbitMQPublisherOptions _options;
    private readonly ILogger<RabbitMQChangePublisher> _logger;
    private readonly ISerializer _serializer;
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly IAsyncPolicy _retryPolicy;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the RabbitMQChangePublisher class.
    /// </summary>
    public RabbitMQChangePublisher(
        IOptions<RabbitMQPublisherOptions> options,
        ILogger<RabbitMQChangePublisher> logger,
        ISerializer serializer)
    {
        _options = options.Value;
        _logger = logger;
        _serializer = serializer;

        // Create connection factory
        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            VirtualHost = _options.VirtualHost,
            UserName = _options.UserName,
            Password = _options.Password,
            RequestedConnectionTimeout = TimeSpan.FromSeconds(_options.Connection.TimeoutSeconds),
            RequestedHeartbeat = TimeSpan.FromSeconds(_options.Connection.HeartbeatSeconds),
            AutomaticRecoveryEnabled = _options.Connection.AutomaticRecoveryEnabled,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(_options.Connection.NetworkRecoveryIntervalSeconds),
            RequestedChannelMax = (ushort)_options.Connection.MaxChannels
        };

        // Add custom properties
        foreach (var prop in _options.ConnectionProperties)
        {
            factory.ClientProperties[prop.Key] = prop.Value;
        }

        // Create connection and channel
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        // Declare exchange
        _channel.ExchangeDeclare(
            exchange: _options.ExchangeName,
            type: _options.ExchangeType,
            durable: _options.ExchangeDurable);

        // Set up retry policy
        _retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                _options.Retry.MaxRetries,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(_options.Retry.BackoffSeconds, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    _logger.LogWarning("RabbitMQ publish retry {RetryCount} after {Delay}ms", retryCount, timespan.TotalMilliseconds);
                });

        _logger.LogInformation("RabbitMQ publisher initialized for exchange: {ExchangeName}", _options.ExchangeName);
    }

    /// <inheritdoc />
    public async Task PublishAsync(ChangeEvent changeEvent, CancellationToken cancellationToken = default)
    {
        if (!_options.Retry.Enabled)
        {
            await PublishInternalAsync(changeEvent, cancellationToken);
            return;
        }

        await _retryPolicy.ExecuteAsync(async (ct) =>
        {
            await PublishInternalAsync(changeEvent, ct);
        }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task PublishBatchAsync(IEnumerable<ChangeEvent> changeEvents, CancellationToken cancellationToken = default)
    {
        var tasks = changeEvents.Select(ev => PublishAsync(ev, cancellationToken));
        await Task.WhenAll(tasks);
    }

    private async Task PublishInternalAsync(ChangeEvent changeEvent, CancellationToken cancellationToken)
    {
        try
        {
            var routingKey = GetRoutingKey(changeEvent);
            var payload = _serializer.Serialize(changeEvent);
            var body = Encoding.UTF8.GetBytes(payload);

            var properties = _channel.CreateBasicProperties();
            properties.Persistent = _options.PersistentMessages;
            properties.Timestamp = new AmqpTimestamp(((DateTimeOffset)changeEvent.TimestampUtc).ToUnixTimeSeconds());
            properties.MessageId = Guid.NewGuid().ToString();
            properties.CorrelationId = changeEvent.Offset;

            // Add custom headers
            properties.Headers = new Dictionary<string, object>
            {
                ["source"] = changeEvent.Source,
                ["schema"] = changeEvent.Schema,
                ["table"] = changeEvent.Table,
                ["operation"] = changeEvent.Operation,
                ["offset"] = changeEvent.Offset
            };

            // Add metadata headers
            foreach (var metadata in changeEvent.Metadata)
            {
                properties.Headers[$"metadata_{metadata.Key}"] = metadata.Value;
            }

            _channel.BasicPublish(
                exchange: _options.ExchangeName,
                routingKey: routingKey,
                basicProperties: properties,
                body: body);

            _logger.LogDebug("Successfully published change event to RabbitMQ. Exchange: {Exchange}, RoutingKey: {RoutingKey}", 
                _options.ExchangeName, routingKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish change event to RabbitMQ. Exchange: {Exchange}", _options.ExchangeName);
            throw;
        }
    }

    private string GetRoutingKey(ChangeEvent changeEvent)
    {
        try
        {
            return _options.RoutingKeyFormat
                .Replace("{source}", changeEvent.Source)
                .Replace("{schema}", changeEvent.Schema)
                .Replace("{table}", changeEvent.Table)
                .Replace("{operation}", changeEvent.Operation.ToLowerInvariant());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to format routing key, using default routing key");
            return _options.DefaultRoutingKey;
        }
    }

    /// <summary>
    /// Disposes the RabbitMQ connection and channel.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            try
            {
                _channel?.Close();
                _channel?.Dispose();
                _connection?.Close();
                _connection?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing RabbitMQ resources");
            }
            finally
            {
                _disposed = true;
            }
        }
    }
}