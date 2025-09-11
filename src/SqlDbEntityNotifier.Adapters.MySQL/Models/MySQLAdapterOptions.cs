namespace SqlDbEntityNotifier.Adapters.MySQL.Models;

/// <summary>
/// Configuration options for MySQL adapter.
/// </summary>
public sealed class MySQLAdapterOptions
{
    /// <summary>
    /// Gets or sets the database source identifier.
    /// </summary>
    public string Source { get; set; } = "mysql";

    /// <summary>
    /// Gets or sets the MySQL connection string.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the server ID for replication.
    /// </summary>
    public uint ServerId { get; set; } = 1;

    /// <summary>
    /// Gets or sets the binary log file name to start from.
    /// </summary>
    public string? BinLogFileName { get; set; }

    /// <summary>
    /// Gets or sets the binary log position to start from.
    /// </summary>
    public uint BinLogPosition { get; set; } = 4;

    /// <summary>
    /// Gets or sets the heartbeat interval in seconds.
    /// </summary>
    public uint HeartbeatInterval { get; set; } = 30;

    /// <summary>
    /// Gets or sets whether to include before data in change events.
    /// </summary>
    public bool IncludeBefore { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to include after data in change events.
    /// </summary>
    public bool IncludeAfter { get; set; } = true;

    /// <summary>
    /// Gets or sets the list of databases to monitor (empty means all databases).
    /// </summary>
    public IList<string> MonitoredDatabases { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets the list of databases to exclude from monitoring.
    /// </summary>
    public IList<string> ExcludedDatabases { get; set; } = new List<string>
    {
        "information_schema",
        "performance_schema",
        "mysql",
        "sys"
    };

    /// <summary>
    /// Gets or sets the list of tables to monitor (empty means all tables).
    /// </summary>
    public IList<string> MonitoredTables { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets the list of tables to exclude from monitoring.
    /// </summary>
    public IList<string> ExcludedTables { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets the SSL configuration.
    /// </summary>
    public SslOptions Ssl { get; set; } = new();

    /// <summary>
    /// Gets or sets the connection timeout in seconds.
    /// </summary>
    public uint ConnectionTimeout { get; set; } = 30;

    /// <summary>
    /// Gets or sets the command timeout in seconds.
    /// </summary>
    public uint CommandTimeout { get; set; } = 30;

    /// <summary>
    /// Gets or sets whether to use GTID (Global Transaction Identifier).
    /// </summary>
    public bool UseGtid { get; set; } = false;

    /// <summary>
    /// Gets or sets the GTID set to start from.
    /// </summary>
    public string? GtidSet { get; set; }

    /// <summary>
    /// Gets or sets the retry configuration.
    /// </summary>
    public RetryOptions Retry { get; set; } = new();
}

/// <summary>
/// SSL configuration for MySQL connection.
/// </summary>
public sealed class SslOptions
{
    /// <summary>
    /// Gets or sets whether SSL is required.
    /// </summary>
    public bool Required { get; set; } = false;

    /// <summary>
    /// Gets or sets the SSL mode.
    /// </summary>
    public SslMode Mode { get; set; } = SslMode.Preferred;

    /// <summary>
    /// Gets or sets the path to the SSL certificate file.
    /// </summary>
    public string? CertificateFile { get; set; }

    /// <summary>
    /// Gets or sets the path to the SSL key file.
    /// </summary>
    public string? KeyFile { get; set; }

    /// <summary>
    /// Gets or sets the path to the SSL CA file.
    /// </summary>
    public string? CaFile { get; set; }
}

/// <summary>
/// SSL modes for MySQL connection.
/// </summary>
public enum SslMode
{
    /// <summary>
    /// SSL is disabled.
    /// </summary>
    Disabled,

    /// <summary>
    /// SSL is preferred but not required.
    /// </summary>
    Preferred,

    /// <summary>
    /// SSL is required.
    /// </summary>
    Required,

    /// <summary>
    /// SSL is required and certificate verification is disabled.
    /// </summary>
    RequiredNoVerify
}

/// <summary>
/// Retry configuration for MySQL operations.
/// </summary>
public sealed class RetryOptions
{
    /// <summary>
    /// Gets or sets the maximum number of retries.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Gets or sets the retry delay in milliseconds.
    /// </summary>
    public int DelayMs { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the maximum retry delay in milliseconds.
    /// </summary>
    public int MaxDelayMs { get; set; } = 10000;

    /// <summary>
    /// Gets or sets the retry backoff multiplier.
    /// </summary>
    public double BackoffMultiplier { get; set; } = 2.0;

    /// <summary>
    /// Gets or sets whether to enable retries.
    /// </summary>
    public bool Enabled { get; set; } = true;
}