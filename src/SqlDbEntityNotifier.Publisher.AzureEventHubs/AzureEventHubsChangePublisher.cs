using System.Text;
using Azure.Core;
using Azure.Identity;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using SqlDbEntityNotifier.Core.Interfaces;
using SqlDbEntityNotifier.Core.Models;
using SqlDbEntityNotifier.Publisher.AzureEventHubs.Models;

namespace SqlDbEntityNotifier.Publisher.AzureEventHubs;

/// <summary>
/// Azure Event Hubs publisher implementation for change events.
/// </summary>
public class AzureEventHubsChangePublisher : IChangePublisher, IAsyncDisposable
{
    private readonly AzureEventHubsPublisherOptions _options;
    private readonly ILogger<AzureEventHubsChangePublisher> _logger;
    private readonly ISerializer _serializer;
    private readonly EventHubProducerClient _producerClient;
    private readonly IAsyncPolicy _retryPolicy;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the AzureEventHubsChangePublisher class.
    /// </summary>
    public AzureEventHubsChangePublisher(
        IOptions<AzureEventHubsPublisherOptions> options,
        ILogger<AzureEventHubsChangePublisher> logger,
        ISerializer serializer)
    {
        _options = options.Value;
        _logger = logger;
        _serializer = serializer;

        // Create producer client based on authentication type
        _producerClient = CreateProducerClient();

        // Set up retry policy
        _retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                _options.Retry.MaxRetries,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(_options.Retry.BackoffSeconds, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    _logger.LogWarning("Azure Event Hubs publish retry {RetryCount} after {Delay}ms", retryCount, timespan.TotalMilliseconds);
                });

        _logger.LogInformation("Azure Event Hubs publisher initialized for Event Hub: {EventHubName}", _options.EventHubName);
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
        if (_options.Batching.Enabled)
        {
            await PublishBatchInternalAsync(changeEvents, cancellationToken);
        }
        else
        {
            var tasks = changeEvents.Select(ev => PublishAsync(ev, cancellationToken));
            await Task.WhenAll(tasks);
        }
    }

    private async Task PublishInternalAsync(ChangeEvent changeEvent, CancellationToken cancellationToken)
    {
        try
        {
            var eventData = CreateEventData(changeEvent);
            var partitionKey = GetPartitionKey(changeEvent);

            using var eventBatch = await _producerClient.CreateBatchAsync(new CreateBatchOptions
            {
                PartitionKey = partitionKey,
                MaximumSizeInBytes = _options.Batching.MaxSizeBytes
            }, cancellationToken);

            if (!eventBatch.TryAdd(eventData))
            {
                throw new InvalidOperationException("Event data could not be added to the batch");
            }

            await _producerClient.SendAsync(eventBatch, cancellationToken);

            _logger.LogDebug("Successfully published change event to Azure Event Hubs. EventHub: {EventHubName}, PartitionKey: {PartitionKey}", 
                _options.EventHubName, partitionKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish change event to Azure Event Hubs. EventHub: {EventHubName}", _options.EventHubName);
            throw;
        }
    }

    private async Task PublishBatchInternalAsync(IEnumerable<ChangeEvent> changeEvents, CancellationToken cancellationToken)
    {
        try
        {
            var eventDataList = changeEvents.Select(CreateEventData).ToList();
            var batches = new List<EventDataBatch>();

            using var eventBatch = await _producerClient.CreateBatchAsync(new CreateBatchOptions
            {
                MaximumSizeInBytes = _options.Batching.MaxSizeBytes
            }, cancellationToken);

            foreach (var eventData in eventDataList)
            {
                if (!eventBatch.TryAdd(eventData))
                {
                    // Current batch is full, send it and create a new one
                    batches.Add(eventBatch);
                    var newBatch = await _producerClient.CreateBatchAsync(new CreateBatchOptions
                    {
                        MaximumSizeInBytes = _options.Batching.MaxSizeBytes
                    }, cancellationToken);
                    
                    if (!newBatch.TryAdd(eventData))
                    {
                        throw new InvalidOperationException("Event data could not be added to a new batch");
                    }
                }
            }

            // Send all batches
            var sendTasks = batches.Select(batch => _producerClient.SendAsync(batch, cancellationToken));
            await Task.WhenAll(sendTasks);

            _logger.LogDebug("Successfully published {Count} change events to Azure Event Hubs in batches", eventDataList.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish batch of change events to Azure Event Hubs");
            throw;
        }
    }

    private EventData CreateEventData(ChangeEvent changeEvent)
    {
        var payload = _serializer.Serialize(changeEvent);
        var eventData = new EventData(Encoding.UTF8.GetBytes(payload));

        // Set event properties
        eventData.Properties["source"] = changeEvent.Source;
        eventData.Properties["schema"] = changeEvent.Schema;
        eventData.Properties["table"] = changeEvent.Table;
        eventData.Properties["operation"] = changeEvent.Operation;
        eventData.Properties["offset"] = changeEvent.Offset;
        eventData.Properties["timestamp"] = changeEvent.TimestampUtc.ToString("O");
        eventData.Properties["content-type"] = _serializer.ContentType;

        // Add metadata properties
        foreach (var metadata in changeEvent.Metadata)
        {
            eventData.Properties[$"metadata_{metadata.Key}"] = metadata.Value;
        }

        return eventData;
    }

    private string GetPartitionKey(ChangeEvent changeEvent)
    {
        try
        {
            return _options.PartitionKeyFormat
                .Replace("{source}", changeEvent.Source)
                .Replace("{schema}", changeEvent.Schema)
                .Replace("{table}", changeEvent.Table);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to format partition key, using default partition key");
            return _options.DefaultPartitionKey;
        }
    }

    private EventHubProducerClient CreateProducerClient()
    {
        var clientOptions = new EventHubProducerClientOptions
        {
            Identifier = _options.Producer.Identifier,
            ConnectionOptions = new EventHubConnectionOptions
            {
                IdleTimeout = TimeSpan.FromSeconds(_options.Producer.ConnectionIdleTimeoutSeconds)
            }
        };

        return _options.Authentication.Type switch
        {
            AuthenticationType.ConnectionString => new EventHubProducerClient(_options.ConnectionString, _options.EventHubName, clientOptions),
            AuthenticationType.ServicePrincipal => CreateServicePrincipalClient(clientOptions),
            AuthenticationType.ManagedIdentity => CreateManagedIdentityClient(clientOptions),
            _ => throw new ArgumentException($"Unsupported authentication type: {_options.Authentication.Type}")
        };
    }

    private EventHubProducerClient CreateServicePrincipalClient(EventHubProducerClientOptions clientOptions)
    {
        var credential = new ClientSecretCredential(
            _options.Authentication.TenantId,
            _options.Authentication.ClientId,
            _options.Authentication.ClientSecret);

        return new EventHubProducerClient(_options.FullyQualifiedNamespace, _options.EventHubName, credential, clientOptions);
    }

    private EventHubProducerClient CreateManagedIdentityClient(EventHubProducerClientOptions clientOptions)
    {
        var credential = string.IsNullOrEmpty(_options.Authentication.ManagedIdentityClientId)
            ? new DefaultAzureCredential()
            : new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                ManagedIdentityClientId = _options.Authentication.ManagedIdentityClientId
            });

        return new EventHubProducerClient(_options.FullyQualifiedNamespace, _options.EventHubName, credential, clientOptions);
    }

    /// <summary>
    /// Disposes the Azure Event Hubs producer client.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            try
            {
                await _producerClient.DisposeAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing Azure Event Hubs producer client");
            }
            finally
            {
                _disposed = true;
            }
        }
    }
}