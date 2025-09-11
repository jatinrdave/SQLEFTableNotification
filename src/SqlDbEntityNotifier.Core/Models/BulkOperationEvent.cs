using System.Text.Json;

namespace SqlDbEntityNotifier.Core.Models;

/// <summary>
/// Represents a bulk operation event that affects multiple rows.
/// </summary>
public sealed class BulkOperationEvent
{
    /// <summary>
    /// Gets the database source identifier.
    /// </summary>
    public string Source { get; init; } = string.Empty;

    /// <summary>
    /// Gets the database schema name.
    /// </summary>
    public string Schema { get; init; } = string.Empty;

    /// <summary>
    /// Gets the table name that was affected.
    /// </summary>
    public string Table { get; init; } = string.Empty;

    /// <summary>
    /// Gets the bulk operation type: BULK_INSERT, BULK_UPDATE, or BULK_DELETE.
    /// </summary>
    public BulkOperationType OperationType { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the bulk operation occurred.
    /// </summary>
    public DateTime TimestampUtc { get; init; }

    /// <summary>
    /// Gets the adapter-specific offset token for replay purposes.
    /// </summary>
    public string Offset { get; init; } = string.Empty;

    /// <summary>
    /// Gets the number of rows affected by the bulk operation.
    /// </summary>
    public int AffectedRowCount { get; init; }

    /// <summary>
    /// Gets the batch identifier for grouping related bulk operations.
    /// </summary>
    public string BatchId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the transaction identifier if the bulk operation was part of a transaction.
    /// </summary>
    public string? TransactionId { get; init; }

    /// <summary>
    /// Gets the SQL statement that caused the bulk operation (if available).
    /// </summary>
    public string? SqlStatement { get; init; }

    /// <summary>
    /// Gets the execution duration of the bulk operation in milliseconds.
    /// </summary>
    public long ExecutionDurationMs { get; init; }

    /// <summary>
    /// Gets sample data from the bulk operation (first few rows).
    /// </summary>
    public IList<JsonElement> SampleData { get; init; } = new List<JsonElement>();

    /// <summary>
    /// Gets additional metadata about the bulk operation.
    /// </summary>
    public IDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();

    /// <summary>
    /// Gets the operation type as a string.
    /// </summary>
    public string Operation => OperationType.ToString();

    /// <summary>
    /// Creates a new BulkOperationEvent with the specified properties.
    /// </summary>
    public static BulkOperationEvent Create(
        string source,
        string schema,
        string table,
        BulkOperationType operationType,
        int affectedRowCount,
        string offset,
        string batchId = "",
        string? transactionId = null,
        string? sqlStatement = null,
        long executionDurationMs = 0,
        IList<JsonElement>? sampleData = null,
        IDictionary<string, string>? metadata = null)
    {
        return new BulkOperationEvent
        {
            Source = source,
            Schema = schema,
            Table = table,
            OperationType = operationType,
            TimestampUtc = DateTime.UtcNow,
            Offset = offset,
            AffectedRowCount = affectedRowCount,
            BatchId = batchId,
            TransactionId = transactionId,
            SqlStatement = sqlStatement,
            ExecutionDurationMs = executionDurationMs,
            SampleData = sampleData ?? new List<JsonElement>(),
            Metadata = metadata ?? new Dictionary<string, string>()
        };
    }

    /// <summary>
    /// Converts the bulk operation event to a standard change event for compatibility.
    /// </summary>
    public ChangeEvent ToChangeEvent()
    {
        var bulkData = new
        {
            OperationType = OperationType.ToString(),
            AffectedRowCount = AffectedRowCount,
            BatchId = BatchId,
            TransactionId = TransactionId,
            SqlStatement = SqlStatement,
            ExecutionDurationMs = ExecutionDurationMs,
            SampleData = SampleData
        };

        return ChangeEvent.Create(
            Source,
            Schema,
            Table,
            Operation,
            Offset,
            null,
            JsonSerializer.SerializeToElement(bulkData),
            Metadata);
    }
}

/// <summary>
/// Types of bulk operations.
/// </summary>
public enum BulkOperationType
{
    /// <summary>
    /// Bulk insert operation.
    /// </summary>
    BULK_INSERT,

    /// <summary>
    /// Bulk update operation.
    /// </summary>
    BULK_UPDATE,

    /// <summary>
    /// Bulk delete operation.
    /// </summary>
    BULK_DELETE
}