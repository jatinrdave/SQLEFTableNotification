using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SqlDbEntityNotifier.Core.Interfaces;
using SqlDbEntityNotifier.Core.Models;

namespace SqlDbEntityNotifier.Core.BulkOperations;

/// <summary>
/// Detects and processes bulk operations from database changes.
/// </summary>
public class BulkOperationDetector
{
    private readonly ILogger<BulkOperationDetector> _logger;
    private readonly BulkOperationDetectorOptions _options;
    private readonly IChangePublisher _changePublisher;
    private readonly Dictionary<string, BulkOperationBatch> _activeBatches;
    private readonly Timer _batchTimeoutTimer;

    /// <summary>
    /// Initializes a new instance of the BulkOperationDetector class.
    /// </summary>
    public BulkOperationDetector(
        ILogger<BulkOperationDetector> logger,
        IOptions<BulkOperationDetectorOptions> options,
        IChangePublisher changePublisher)
    {
        _logger = logger;
        _options = options.Value;
        _changePublisher = changePublisher;
        _activeBatches = new Dictionary<string, BulkOperationBatch>();

        // Start batch timeout timer
        _batchTimeoutTimer = new Timer(ProcessTimeoutBatches, null, 
            TimeSpan.FromSeconds(_options.BatchTimeoutSeconds), 
            TimeSpan.FromSeconds(_options.BatchTimeoutSeconds));
    }

    /// <summary>
    /// Processes a change event to detect bulk operations.
    /// </summary>
    /// <param name="changeEvent">The change event to process.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the processing operation.</returns>
    public async Task ProcessChangeEventAsync(ChangeEvent changeEvent, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return;
        }

        try
        {
            // Check if this is a bulk operation
            if (IsBulkOperation(changeEvent))
            {
                await ProcessBulkOperationAsync(changeEvent, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing change event for bulk operation detection: {Table}", changeEvent.Table);
        }
    }

    /// <summary>
    /// Manually triggers a bulk operation event.
    /// </summary>
    /// <param name="bulkEvent">The bulk operation event.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the publishing operation.</returns>
    public async Task PublishBulkOperationAsync(BulkOperationEvent bulkEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            // Apply filters
            if (!ShouldPublishBulkOperation(bulkEvent))
            {
                _logger.LogDebug("Bulk operation filtered out: {Operation} on {Table}", bulkEvent.Operation, bulkEvent.Table);
                return;
            }

            // Convert to change event and publish
            var changeEvent = bulkEvent.ToChangeEvent();
            await _changePublisher.PublishAsync(changeEvent, cancellationToken);

            _logger.LogInformation("Published bulk operation: {Operation} on {Table} affecting {Count} rows", 
                bulkEvent.Operation, bulkEvent.Table, bulkEvent.AffectedRowCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing bulk operation: {Operation} on {Table}", bulkEvent.Operation, bulkEvent.Table);
            throw;
        }
    }

    private bool IsBulkOperation(ChangeEvent changeEvent)
    {
        // Check if this change event indicates a bulk operation
        // This could be based on:
        // 1. Metadata indicating bulk operation
        // 2. Timing patterns (multiple changes in short time)
        // 3. Transaction patterns
        // 4. SQL statement analysis

        if (changeEvent.Metadata.TryGetValue("bulk_operation", out var bulkFlag))
        {
            return bool.TryParse(bulkFlag, out var isBulk) && isBulk;
        }

        if (changeEvent.Metadata.TryGetValue("affected_rows", out var rowCountStr))
        {
            if (int.TryParse(rowCountStr, out var rowCount) && rowCount > 1)
            {
                return true;
            }
        }

        return false;
    }

    private async Task ProcessBulkOperationAsync(ChangeEvent changeEvent, CancellationToken cancellationToken)
    {
        var batchKey = GetBatchKey(changeEvent);
        
        if (!_activeBatches.TryGetValue(batchKey, out var batch))
        {
            batch = new BulkOperationBatch
            {
                Source = changeEvent.Source,
                Schema = changeEvent.Schema,
                Table = changeEvent.Table,
                OperationType = GetBulkOperationType(changeEvent),
                BatchId = Guid.NewGuid().ToString(),
                StartTime = changeEvent.TimestampUtc,
                TransactionId = changeEvent.Metadata.GetValueOrDefault("transaction_id")
            };
            _activeBatches[batchKey] = batch;
        }

        // Add change event to batch
        batch.ChangeEvents.Add(changeEvent);
        batch.LastUpdateTime = changeEvent.TimestampUtc;

        // Check if batch is complete
        if (IsBatchComplete(batch))
        {
            await FinalizeBatchAsync(batch, cancellationToken);
            _activeBatches.Remove(batchKey);
        }
    }

    private string GetBatchKey(ChangeEvent changeEvent)
    {
        // Group by source, schema, table, operation, and transaction
        var transactionId = changeEvent.Metadata.GetValueOrDefault("transaction_id", "");
        return $"{changeEvent.Source}:{changeEvent.Schema}:{changeEvent.Table}:{changeEvent.Operation}:{transactionId}";
    }

    private BulkOperationType GetBulkOperationType(ChangeEvent changeEvent)
    {
        return changeEvent.Operation.ToUpperInvariant() switch
        {
            "INSERT" => BulkOperationType.BULK_INSERT,
            "UPDATE" => BulkOperationType.BULK_UPDATE,
            "DELETE" => BulkOperationType.BULK_DELETE,
            _ => throw new ArgumentException($"Unknown operation type: {changeEvent.Operation}")
        };
    }

    private bool IsBatchComplete(BulkOperationBatch batch)
    {
        // Batch is complete if:
        // 1. It has reached the maximum batch size
        // 2. It has been inactive for the timeout period
        // 3. A transaction has ended (if applicable)

        if (batch.ChangeEvents.Count >= _options.MaxBatchSize)
        {
            return true;
        }

        var timeSinceLastUpdate = DateTime.UtcNow - batch.LastUpdateTime;
        if (timeSinceLastUpdate.TotalSeconds > _options.BatchTimeoutSeconds)
        {
            return true;
        }

        return false;
    }

    private async Task FinalizeBatchAsync(BulkOperationBatch batch, CancellationToken cancellationToken)
    {
        try
        {
            var executionDuration = (batch.LastUpdateTime - batch.StartTime).TotalMilliseconds;
            var sampleData = batch.ChangeEvents.Take(_options.MaxSampleSize).Select(ce => ce.After ?? ce.Before).Where(je => je.HasValue).Cast<JsonElement>().ToList();

            var bulkEvent = BulkOperationEvent.Create(
                batch.Source,
                batch.Schema,
                batch.Table,
                batch.OperationType,
                batch.ChangeEvents.Count,
                batch.ChangeEvents.Last().Offset,
                batch.BatchId,
                batch.TransactionId,
                null, // SQL statement not available in this context
                (long)executionDuration,
                sampleData,
                new Dictionary<string, string>
                {
                    ["batch_size"] = batch.ChangeEvents.Count.ToString(),
                    ["execution_duration_ms"] = executionDuration.ToString("F0"),
                    ["start_time"] = batch.StartTime.ToString("O"),
                    ["end_time"] = batch.LastUpdateTime.ToString("O")
                });

            await PublishBulkOperationAsync(bulkEvent, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finalizing bulk operation batch: {BatchId}", batch.BatchId);
        }
    }

    private bool ShouldPublishBulkOperation(BulkOperationEvent bulkEvent)
    {
        // Apply filtering rules
        if (_options.MinRowCount > 0 && bulkEvent.AffectedRowCount < _options.MinRowCount)
        {
            return false;
        }

        if (_options.ExcludedTables.Contains(bulkEvent.Table, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        if (_options.IncludedTables.Any() && !_options.IncludedTables.Contains(bulkEvent.Table, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        if (_options.ExcludedOperations.Contains(bulkEvent.OperationType))
        {
            return false;
        }

        return true;
    }

    private void ProcessTimeoutBatches(object? state)
    {
        try
        {
            var timeoutBatches = _activeBatches.Values
                .Where(batch => (DateTime.UtcNow - batch.LastUpdateTime).TotalSeconds > _options.BatchTimeoutSeconds)
                .ToList();

            foreach (var batch in timeoutBatches)
            {
                var batchKey = GetBatchKey(batch.ChangeEvents.First());
                FinalizeBatchAsync(batch, CancellationToken.None).GetAwaiter().GetResult();
                _activeBatches.Remove(batchKey);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing timeout batches");
        }
    }

    /// <summary>
    /// Disposes the bulk operation detector.
    /// </summary>
    public void Dispose()
    {
        _batchTimeoutTimer?.Dispose();
    }
}

/// <summary>
/// Represents a batch of related change events that form a bulk operation.
/// </summary>
internal sealed class BulkOperationBatch
{
    public string Source { get; set; } = string.Empty;
    public string Schema { get; set; } = string.Empty;
    public string Table { get; set; } = string.Empty;
    public BulkOperationType OperationType { get; set; }
    public string BatchId { get; set; } = string.Empty;
    public string? TransactionId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime LastUpdateTime { get; set; }
    public IList<ChangeEvent> ChangeEvents { get; set; } = new List<ChangeEvent>();
}