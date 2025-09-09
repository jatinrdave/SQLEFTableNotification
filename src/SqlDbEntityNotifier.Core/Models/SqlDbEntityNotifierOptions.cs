namespace SqlDbEntityNotifier.Core.Models;

/// <summary>
/// Configuration options for SQLDBEntityNotifier.
/// </summary>
public sealed class SqlDbEntityNotifierOptions
{
    /// <summary>
    /// Gets or sets the default batch size for processing events.
    /// </summary>
    public int DefaultBatchSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets the default flush interval in milliseconds.
    /// </summary>
    public int DefaultFlushIntervalMs { get; set; } = 500;

    /// <summary>
    /// Gets or sets the default maximum degree of parallelism.
    /// </summary>
    public int DefaultMaxDegreeOfParallelism { get; set; } = Environment.ProcessorCount;

    /// <summary>
    /// Gets or sets the default serializer type.
    /// </summary>
    public string DefaultSerializer { get; set; } = "json";

    /// <summary>
    /// Gets or sets whether to enable metrics collection.
    /// </summary>
    public bool EnableMetrics { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable health checks.
    /// </summary>
    public bool EnableHealthChecks { get; set; } = true;
}