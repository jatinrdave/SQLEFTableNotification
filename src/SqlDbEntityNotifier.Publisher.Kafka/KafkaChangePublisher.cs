using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using SqlDbEntityNotifier.Core.Interfaces;
using SqlDbEntityNotifier.Core.Models;
using SqlDbEntityNotifier.Publisher.Kafka.Models;

namespace SqlDbEntityNotifier.Publisher.Kafka;

/// <summary>
/// Kafka publisher implementation for change events.
/// </summary>
public class KafkaChangePublisher : IChangePublisher
{
    private readonly KafkaPublisherOptions _options;
    private readonly ILogger<KafkaChangePublisher> _logger;
    private readonly ISerializer _serializer;
    private readonly IProducer<Null, string> _producer;
    private readonly IAsyncPolicy _retryPolicy;

    /// <summary>
    /// Initializes a new instance of the KafkaChangePublisher class.
    /// </summary>
    public KafkaChangePublisher(
        IOptions<KafkaPublisherOptions> options,
        ILogger<KafkaChangePublisher> logger,
        ISerializer serializer)
    {
        _options = options.Value;
        _logger = logger;
        _serializer = serializer;

        // Create Kafka producer configuration
        var config = new ProducerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            ClientId = _options.ClientId,
            Acks = _options.Acks,
            RetryBackoffMs = _options.Retry.BackoffSeconds * 1000,
            MessageSendMaxRetries = _options.Retry.Enabled ? _options.Retry.MaxRetries : 0,
            BatchSize = _options.Batch.Enabled ? _options.Batch.Size : 0,
            LingerMs = _options.Batch.Enabled ? _options.Batch.LingerMs : 0,
            EnableIdempotence = true,
            MaxInFlight = 5
        };

        // Add additional properties
        foreach (var prop in _options.AdditionalProperties)
        {
            config.Set(prop.Key, prop.Value);
        }

        _producer = new ProducerBuilder<Null, string>(config)
            .SetErrorHandler((_, error) => _logger.LogError("Kafka producer error: {Error}", error))
            .SetLogHandler((_, logMessage) => _logger.LogDebug("Kafka log: {Message}", logMessage.Message))
            .Build();

        // Create retry policy
        _retryPolicy = Policy
            .Handle<KafkaException>()
            .Or<ProduceException<Null, string>>()
            .WaitAndRetryAsync(
                _options.Retry.MaxRetries,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(_options.Retry.BackoffSeconds, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    _logger.LogWarning("Kafka publish retry {RetryCount} after {Delay}ms", retryCount, timespan.TotalMilliseconds);
                });
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
            var topic = GetTopicName(changeEvent);
            var payload = _serializer.Serialize(changeEvent);

            var message = new Message<Null, string>
            {
                Value = payload,
                Headers = new Headers
                {
                    { "source", System.Text.Encoding.UTF8.GetBytes(changeEvent.Source) },
                    { "schema", System.Text.Encoding.UTF8.GetBytes(changeEvent.Schema) },
                    { "table", System.Text.Encoding.UTF8.GetBytes(changeEvent.Table) },
                    { "operation", System.Text.Encoding.UTF8.GetBytes(changeEvent.Operation) },
                    { "offset", System.Text.Encoding.UTF8.GetBytes(changeEvent.Offset) },
                    { "timestamp", System.Text.Encoding.UTF8.GetBytes(changeEvent.TimestampUtc.ToString("O")) }
                }
            };

            var result = await _producer.ProduceAsync(topic, message, cancellationToken);

            if (result.Status == PersistenceStatus.Persisted)
            {
                _logger.LogDebug("Successfully published change event to topic {Topic} at offset {Offset}", 
                    topic, result.Offset);
            }
            else
            {
                _logger.LogWarning("Failed to persist change event to topic {Topic}. Status: {Status}", 
                    topic, result.Status);
            }
        }
        catch (ProduceException<Null, string> ex)
        {
            _logger.LogError(ex, "Failed to publish change event to Kafka. Topic: {Topic}, Error: {Error}", 
                GetTopicName(changeEvent), ex.Error);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error publishing change event to Kafka");
            throw;
        }
    }

    private string GetTopicName(ChangeEvent changeEvent)
    {
        try
        {
            return _options.TopicFormat
                .Replace("{source}", changeEvent.Source)
                .Replace("{schema}", changeEvent.Schema)
                .Replace("{table}", changeEvent.Table);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to format topic name, using default topic");
            return _options.DefaultTopic;
        }
    }

    /// <summary>
    /// Disposes the Kafka producer.
    /// </summary>
    public void Dispose()
    {
        _producer?.Dispose();
    }
}