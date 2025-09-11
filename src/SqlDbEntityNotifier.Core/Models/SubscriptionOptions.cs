using System.Linq.Expressions;

namespace SqlDbEntityNotifier.Core.Models;

/// <summary>
/// Configuration options for entity change subscriptions.
/// </summary>
public sealed class SubscriptionOptions
{
    /// <summary>
    /// Gets or sets the database source to monitor.
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the schema name to monitor.
    /// </summary>
    public string Schema { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the table name to monitor.
    /// </summary>
    public string Table { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the filter expression to apply to change events.
    /// </summary>
    public Expression<Func<ChangeEvent, bool>>? Filter { get; set; }

    /// <summary>
    /// Gets or sets the batch size for processing events.
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets the flush interval in milliseconds.
    /// </summary>
    public int FlushIntervalMs { get; set; } = 500;

    /// <summary>
    /// Gets or sets whether to include the 'before' data in change events.
    /// </summary>
    public bool IncludeBefore { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to include the 'after' data in change events.
    /// </summary>
    public bool IncludeAfter { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum degree of parallelism for processing.
    /// </summary>
    public int MaxDegreeOfParallelism { get; set; } = Environment.ProcessorCount;
}