using SqlDbEntityNotifier.Core.Models;

namespace SqlDbEntityNotifier.Core.BulkOperations;

/// <summary>
/// Configuration options for bulk operation detection.
/// </summary>
public sealed class BulkOperationDetectorOptions
{
    /// <summary>
    /// Gets or sets whether bulk operation detection is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the minimum number of rows to consider an operation as bulk.
    /// </summary>
    public int MinRowCount { get; set; } = 2;

    /// <summary>
    /// Gets or sets the maximum batch size before forcing batch completion.
    /// </summary>
    public int MaxBatchSize { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the batch timeout in seconds.
    /// </summary>
    public int BatchTimeoutSeconds { get; set; } = 5;

    /// <summary>
    /// Gets or sets the maximum number of sample rows to include in bulk events.
    /// </summary>
    public int MaxSampleSize { get; set; } = 10;

    /// <summary>
    /// Gets or sets the list of tables to include in bulk operation detection (empty means all tables).
    /// </summary>
    public IList<string> IncludedTables { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets the list of tables to exclude from bulk operation detection.
    /// </summary>
    public IList<string> ExcludedTables { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets the list of bulk operation types to exclude.
    /// </summary>
    public IList<BulkOperationType> ExcludedOperations { get; set; } = new List<BulkOperationType>();

    /// <summary>
    /// Gets or sets whether to include SQL statements in bulk operation events.
    /// </summary>
    public bool IncludeSqlStatements { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to include sample data in bulk operation events.
    /// </summary>
    public bool IncludeSampleData { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to group bulk operations by transaction.
    /// </summary>
    public bool GroupByTransaction { get; set; } = true;

    /// <summary>
    /// Gets or sets the performance monitoring configuration.
    /// </summary>
    public PerformanceMonitoringOptions PerformanceMonitoring { get; set; } = new();
}

/// <summary>
/// Performance monitoring configuration for bulk operations.
/// </summary>
public sealed class PerformanceMonitoringOptions
{
    /// <summary>
    /// Gets or sets whether to enable performance monitoring.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the threshold for slow bulk operations in milliseconds.
    /// </summary>
    public long SlowOperationThresholdMs { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the threshold for large bulk operations (row count).
    /// </summary>
    public int LargeOperationThreshold { get; set; } = 10000;

    /// <summary>
    /// Gets or sets whether to generate alerts for slow operations.
    /// </summary>
    public bool AlertOnSlowOperations { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to generate alerts for large operations.
    /// </summary>
    public bool AlertOnLargeOperations { get; set; } = true;
}