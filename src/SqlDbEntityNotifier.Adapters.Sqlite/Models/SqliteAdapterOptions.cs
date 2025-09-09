namespace SqlDbEntityNotifier.Adapters.Sqlite.Models;

/// <summary>
/// Configuration options for the SQLite adapter.
/// </summary>
public sealed class SqliteAdapterOptions
{
    /// <summary>
    /// Gets or sets the SQLite database file path.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the connection string for the SQLite database.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the change log table.
    /// </summary>
    public string ChangeTable { get; set; } = "change_log";

    /// <summary>
    /// Gets or sets the polling interval in milliseconds.
    /// </summary>
    public int PollIntervalMs { get; set; } = 500;

    /// <summary>
    /// Gets or sets the maximum batch size for processing changes.
    /// </summary>
    public int MaxBatchSize { get; set; } = 100;

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
    public string Source { get; set; } = "sqlite";

    /// <summary>
    /// Gets or sets whether to automatically create the change log table and triggers.
    /// </summary>
    public bool AutoCreateChangeLog { get; set; } = true;
}