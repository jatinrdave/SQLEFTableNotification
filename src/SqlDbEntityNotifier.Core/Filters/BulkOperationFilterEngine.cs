using System.Linq.Expressions;
using Microsoft.Extensions.Logging;
using SqlDbEntityNotifier.Core.Models;

namespace SqlDbEntityNotifier.Core.Filters;

/// <summary>
/// Filter engine for bulk operation events with LINQ-like expressions.
/// </summary>
public class BulkOperationFilterEngine
{
    private readonly ILogger<BulkOperationFilterEngine> _logger;
    private readonly Dictionary<string, Func<BulkOperationEvent, bool>> _compiledFilters;

    /// <summary>
    /// Initializes a new instance of the BulkOperationFilterEngine class.
    /// </summary>
    public BulkOperationFilterEngine(ILogger<BulkOperationFilterEngine> logger)
    {
        _logger = logger;
        _compiledFilters = new Dictionary<string, Func<BulkOperationEvent, bool>>();
    }

    /// <summary>
    /// Compiles a filter expression for bulk operations.
    /// </summary>
    /// <param name="filterExpression">The filter expression.</param>
    /// <returns>A compiled filter function.</returns>
    public Func<BulkOperationEvent, bool> CompileFilter(Expression<Func<BulkOperationEvent, bool>> filterExpression)
    {
        try
        {
            var compiled = filterExpression.Compile();
            _logger.LogDebug("Compiled bulk operation filter: {Expression}", filterExpression.ToString());
            return compiled;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error compiling bulk operation filter: {Expression}", filterExpression.ToString());
            throw;
        }
    }

    /// <summary>
    /// Applies a filter to a collection of bulk operation events.
    /// </summary>
    /// <param name="bulkEvents">The bulk operation events to filter.</param>
    /// <param name="filter">The filter function.</param>
    /// <returns>The filtered bulk operation events.</returns>
    public IEnumerable<BulkOperationEvent> ApplyFilter(IEnumerable<BulkOperationEvent> bulkEvents, Func<BulkOperationEvent, bool> filter)
    {
        try
        {
            return bulkEvents.Where(filter);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying bulk operation filter");
            throw;
        }
    }

    /// <summary>
    /// Creates a filter for bulk operations by table name.
    /// </summary>
    /// <param name="tableName">The table name to filter by.</param>
    /// <returns>A filter function.</returns>
    public Func<BulkOperationEvent, bool> CreateTableFilter(string tableName)
    {
        return CompileFilter(be => be.Table == tableName);
    }

    /// <summary>
    /// Creates a filter for bulk operations by operation type.
    /// </summary>
    /// <param name="operationType">The operation type to filter by.</param>
    /// <returns>A filter function.</returns>
    public Func<BulkOperationEvent, bool> CreateOperationTypeFilter(BulkOperationType operationType)
    {
        return CompileFilter(be => be.OperationType == operationType);
    }

    /// <summary>
    /// Creates a filter for bulk operations by minimum row count.
    /// </summary>
    /// <param name="minRowCount">The minimum number of affected rows.</param>
    /// <returns>A filter function.</returns>
    public Func<BulkOperationEvent, bool> CreateMinRowCountFilter(int minRowCount)
    {
        return CompileFilter(be => be.AffectedRowCount >= minRowCount);
    }

    /// <summary>
    /// Creates a filter for bulk operations by maximum row count.
    /// </summary>
    /// <param name="maxRowCount">The maximum number of affected rows.</param>
    /// <returns>A filter function.</returns>
    public Func<BulkOperationEvent, bool> CreateMaxRowCountFilter(int maxRowCount)
    {
        return CompileFilter(be => be.AffectedRowCount <= maxRowCount);
    }

    /// <summary>
    /// Creates a filter for bulk operations by execution duration.
    /// </summary>
    /// <param name="minDurationMs">The minimum execution duration in milliseconds.</param>
    /// <returns>A filter function.</returns>
    public Func<BulkOperationEvent, bool> CreateMinDurationFilter(long minDurationMs)
    {
        return CompileFilter(be => be.ExecutionDurationMs >= minDurationMs);
    }

    /// <summary>
    /// Creates a filter for bulk operations by source.
    /// </summary>
    /// <param name="source">The source to filter by.</param>
    /// <returns>A filter function.</returns>
    public Func<BulkOperationEvent, bool> CreateSourceFilter(string source)
    {
        return CompileFilter(be => be.Source == source);
    }

    /// <summary>
    /// Creates a filter for bulk operations by schema.
    /// </summary>
    /// <param name="schema">The schema to filter by.</param>
    /// <returns>A filter function.</returns>
    public Func<BulkOperationEvent, bool> CreateSchemaFilter(string schema)
    {
        return CompileFilter(be => be.Schema == schema);
    }

    /// <summary>
    /// Creates a filter for bulk operations by transaction ID.
    /// </summary>
    /// <param name="transactionId">The transaction ID to filter by.</param>
    /// <returns>A filter function.</returns>
    public Func<BulkOperationEvent, bool> CreateTransactionFilter(string transactionId)
    {
        return CompileFilter(be => be.TransactionId == transactionId);
    }

    /// <summary>
    /// Creates a filter for bulk operations by batch ID.
    /// </summary>
    /// <param name="batchId">The batch ID to filter by.</param>
    /// <returns>A filter function.</returns>
    public Func<BulkOperationEvent, bool> CreateBatchFilter(string batchId)
    {
        return CompileFilter(be => be.BatchId == batchId);
    }

    /// <summary>
    /// Creates a filter for bulk operations by time range.
    /// </summary>
    /// <param name="startTime">The start time.</param>
    /// <param name="endTime">The end time.</param>
    /// <returns>A filter function.</returns>
    public Func<BulkOperationEvent, bool> CreateTimeRangeFilter(DateTime startTime, DateTime endTime)
    {
        return CompileFilter(be => be.TimestampUtc >= startTime && be.TimestampUtc <= endTime);
    }

    /// <summary>
    /// Creates a complex filter combining multiple conditions.
    /// </summary>
    /// <param name="tableName">The table name (optional).</param>
    /// <param name="operationType">The operation type (optional).</param>
    /// <param name="minRowCount">The minimum row count (optional).</param>
    /// <param name="maxRowCount">The maximum row count (optional).</param>
    /// <param name="minDurationMs">The minimum duration in milliseconds (optional).</param>
    /// <param name="source">The source (optional).</param>
    /// <param name="schema">The schema (optional).</param>
    /// <returns>A filter function.</returns>
    public Func<BulkOperationEvent, bool> CreateComplexFilter(
        string? tableName = null,
        BulkOperationType? operationType = null,
        int? minRowCount = null,
        int? maxRowCount = null,
        long? minDurationMs = null,
        string? source = null,
        string? schema = null)
    {
        return CompileFilter(be =>
            (tableName == null || be.Table == tableName) &&
            (operationType == null || be.OperationType == operationType) &&
            (minRowCount == null || be.AffectedRowCount >= minRowCount) &&
            (maxRowCount == null || be.AffectedRowCount <= maxRowCount) &&
            (minDurationMs == null || be.ExecutionDurationMs >= minDurationMs) &&
            (source == null || be.Source == source) &&
            (schema == null || be.Schema == schema));
    }

    /// <summary>
    /// Creates a filter for high-impact bulk operations.
    /// </summary>
    /// <param name="minRowCount">The minimum row count for high-impact operations.</param>
    /// <param name="minDurationMs">The minimum duration for high-impact operations.</param>
    /// <returns>A filter function.</returns>
    public Func<BulkOperationEvent, bool> CreateHighImpactFilter(int minRowCount = 1000, long minDurationMs = 5000)
    {
        return CompileFilter(be => be.AffectedRowCount >= minRowCount || be.ExecutionDurationMs >= minDurationMs);
    }

    /// <summary>
    /// Creates a filter for bulk operations that might need attention.
    /// </summary>
    /// <param name="maxRowCount">The maximum row count for normal operations.</param>
    /// <param name="maxDurationMs">The maximum duration for normal operations.</param>
    /// <returns>A filter function.</returns>
    public Func<BulkOperationEvent, bool> CreateAttentionFilter(int maxRowCount = 10000, long maxDurationMs = 30000)
    {
        return CompileFilter(be => be.AffectedRowCount > maxRowCount || be.ExecutionDurationMs > maxDurationMs);
    }

    /// <summary>
    /// Gets statistics about bulk operations.
    /// </summary>
    /// <param name="bulkEvents">The bulk operation events to analyze.</param>
    /// <returns>Statistics about the bulk operations.</returns>
    public BulkOperationStatistics GetStatistics(IEnumerable<BulkOperationEvent> bulkEvents)
    {
        var events = bulkEvents.ToList();
        
        return new BulkOperationStatistics
        {
            TotalOperations = events.Count,
            TotalAffectedRows = events.Sum(e => e.AffectedRowCount),
            AverageAffectedRows = events.Any() ? events.Average(e => e.AffectedRowCount) : 0,
            MaxAffectedRows = events.Any() ? events.Max(e => e.AffectedRowCount) : 0,
            MinAffectedRows = events.Any() ? events.Min(e => e.AffectedRowCount) : 0,
            AverageExecutionDuration = events.Any() ? events.Average(e => e.ExecutionDurationMs) : 0,
            MaxExecutionDuration = events.Any() ? events.Max(e => e.ExecutionDurationMs) : 0,
            MinExecutionDuration = events.Any() ? events.Min(e => e.ExecutionDurationMs) : 0,
            OperationsByType = events.GroupBy(e => e.OperationType).ToDictionary(g => g.Key, g => g.Count()),
            OperationsByTable = events.GroupBy(e => e.Table).ToDictionary(g => g.Key, g => g.Count()),
            OperationsBySource = events.GroupBy(e => e.Source).ToDictionary(g => g.Key, g => g.Count())
        };
    }
}

/// <summary>
/// Statistics about bulk operations.
/// </summary>
public sealed class BulkOperationStatistics
{
    /// <summary>
    /// Gets or sets the total number of operations.
    /// </summary>
    public int TotalOperations { get; set; }

    /// <summary>
    /// Gets or sets the total number of affected rows.
    /// </summary>
    public int TotalAffectedRows { get; set; }

    /// <summary>
    /// Gets or sets the average number of affected rows per operation.
    /// </summary>
    public double AverageAffectedRows { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of affected rows in a single operation.
    /// </summary>
    public int MaxAffectedRows { get; set; }

    /// <summary>
    /// Gets or sets the minimum number of affected rows in a single operation.
    /// </summary>
    public int MinAffectedRows { get; set; }

    /// <summary>
    /// Gets or sets the average execution duration in milliseconds.
    /// </summary>
    public double AverageExecutionDuration { get; set; }

    /// <summary>
    /// Gets or sets the maximum execution duration in milliseconds.
    /// </summary>
    public long MaxExecutionDuration { get; set; }

    /// <summary>
    /// Gets or sets the minimum execution duration in milliseconds.
    /// </summary>
    public long MinExecutionDuration { get; set; }

    /// <summary>
    /// Gets or sets the count of operations by type.
    /// </summary>
    public IDictionary<BulkOperationType, int> OperationsByType { get; set; } = new Dictionary<BulkOperationType, int>();

    /// <summary>
    /// Gets or sets the count of operations by table.
    /// </summary>
    public IDictionary<string, int> OperationsByTable { get; set; } = new Dictionary<string, int>();

    /// <summary>
    /// Gets or sets the count of operations by source.
    /// </summary>
    public IDictionary<string, int> OperationsBySource { get; set; } = new Dictionary<string, int>();
}