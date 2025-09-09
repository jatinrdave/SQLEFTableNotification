namespace SqlDbEntityNotifier.Adapters.Postgres.Models;

/// <summary>
/// Configuration options for the PostgreSQL adapter.
/// </summary>
public sealed class PostgresAdapterOptions
{
    /// <summary>
    /// Gets or sets the PostgreSQL connection string.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the logical replication slot name.
    /// </summary>
    public string SlotName { get; set; } = "sqldb_notifier_slot";

    /// <summary>
    /// Gets or sets the publication name for logical replication.
    /// </summary>
    public string PublicationName { get; set; } = "sqldb_notifier_pub";

    /// <summary>
    /// Gets or sets the maximum batch size for processing changes.
    /// </summary>
    public int MaxBatchSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets the polling interval in milliseconds.
    /// </summary>
    public int PollIntervalMs { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the plugin name for logical decoding (e.g., "wal2json", "pgoutput").
    /// </summary>
    public string Plugin { get; set; } = "wal2json";

    /// <summary>
    /// Gets or sets whether to include the 'before' data in change events.
    /// </summary>
    public bool IncludeBefore { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to include the 'after' data in change events.
    /// </summary>
    public bool IncludeAfter { get; set; } = true;

    /// <summary>
    /// Gets or sets the source identifier for this adapter.
    /// </summary>
    public string Source { get; set; } = "postgres";
}