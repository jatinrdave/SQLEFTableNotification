namespace SqlDbEntityNotifier.Adapters.Oracle.Models;

/// <summary>
/// Configuration options for Oracle adapter.
/// </summary>
public sealed class OracleAdapterOptions
{
    /// <summary>
    /// Gets or sets the database source identifier.
    /// </summary>
    public string Source { get; set; } = "oracle";

    /// <summary>
    /// Gets or sets the Oracle connection string.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the LogMiner configuration.
    /// </summary>
    public LogMinerOptions LogMiner { get; set; } = new();

    /// <summary>
    /// Gets or sets whether to include before data in change events.
    /// </summary>
    public bool IncludeBefore { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to include after data in change events.
    /// </summary>
    public bool IncludeAfter { get; set; } = true;

    /// <summary>
    /// Gets or sets the list of schemas to monitor (empty means all schemas).
    /// </summary>
    public IList<string> MonitoredSchemas { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets the list of schemas to exclude from monitoring.
    /// </summary>
    public IList<string> ExcludedSchemas { get; set; } = new List<string>
    {
        "SYS",
        "SYSTEM",
        "OUTLN",
        "DIP",
        "TSMSYS",
        "DBSNMP",
        "WMSYS",
        "EXFSYS",
        "CTXSYS",
        "XDB",
        "ANONYMOUS",
        "ORDSYS",
        "ORDPLUGINS",
        "SI_INFORMTN_SCHEMA",
        "MDSYS",
        "OLAPSYS",
        "MDDATA",
        "SPATIAL_CSW_ADMIN_USR",
        "SPATIAL_WFS_ADMIN_USR"
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
    /// Gets or sets the connection timeout in seconds.
    /// </summary>
    public uint ConnectionTimeout { get; set; } = 30;

    /// <summary>
    /// Gets or sets the command timeout in seconds.
    /// </summary>
    public uint CommandTimeout { get; set; } = 30;

    /// <summary>
    /// Gets or sets the retry configuration.
    /// </summary>
    public RetryOptions Retry { get; set; } = new();

    /// <summary>
    /// Gets or sets the polling configuration.
    /// </summary>
    public PollingOptions Polling { get; set; } = new();
}

/// <summary>
/// LogMiner configuration for Oracle CDC.
/// </summary>
public sealed class LogMinerOptions
{
    /// <summary>
    /// Gets or sets whether to use LogMiner for CDC.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the LogMiner dictionary type.
    /// </summary>
    public LogMinerDictionaryType DictionaryType { get; set; } = LogMinerDictionaryType.OnlineCatalog;

    /// <summary>
    /// Gets or sets the start SCN (System Change Number).
    /// </summary>
    public ulong? StartScn { get; set; }

    /// <summary>
    /// Gets or sets the end SCN (System Change Number).
    /// </summary>
    public ulong? EndScn { get; set; }

    /// <summary>
    /// Gets or sets the start time for LogMiner.
    /// </summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// Gets or sets the end time for LogMiner.
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Gets or sets the list of redo log files to mine.
    /// </summary>
    public IList<string> RedoLogFiles { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets the LogMiner options.
    /// </summary>
    public LogMinerOptionsFlags Options { get; set; } = LogMinerOptionsFlags.DictFromOnlineCatalog;

    /// <summary>
    /// Gets or sets the commit SCN window size.
    /// </summary>
    public uint CommitScnWindow { get; set; } = 1000;
}

/// <summary>
/// LogMiner dictionary types.
/// </summary>
public enum LogMinerDictionaryType
{
    /// <summary>
    /// Use online catalog as dictionary.
    /// </summary>
    OnlineCatalog,

    /// <summary>
    /// Use redo log files as dictionary.
    /// </summary>
    RedoLogFiles,

    /// <summary>
    /// Use dictionary file as dictionary.
    /// </summary>
    DictionaryFile
}

/// <summary>
/// LogMiner options flags.
/// </summary>
[Flags]
public enum LogMinerOptionsFlags
{
    /// <summary>
    /// No special options.
    /// </summary>
    None = 0,

    /// <summary>
    /// Use dictionary from online catalog.
    /// </summary>
    DictFromOnlineCatalog = 1,

    /// <summary>
    /// Use dictionary from redo log files.
    /// </summary>
    DictFromRedoLogFiles = 2,

    /// <summary>
    /// Use dictionary from dictionary file.
    /// </summary>
    DictFromDictionaryFile = 4,

    /// <summary>
    /// Include committed transactions only.
    /// </summary>
    CommittedDataOnly = 8,

    /// <summary>
    /// Include DDL statements.
    /// </summary>
    DdlDictTracking = 16,

    /// <summary>
    /// Include DDL statements in redo log files.
    /// </summary>
    DdlDictFromRedoLogs = 32,

    /// <summary>
    /// Include DDL statements in dictionary file.
    /// </summary>
    DdlDictFromDictionaryFile = 64,

    /// <summary>
    /// Include DDL statements in online catalog.
    /// </summary>
    DdlDictFromOnlineCatalog = 128,

    /// <summary>
    /// Include DDL statements in dictionary file.
    /// </summary>
    DdlDictFromDictionaryFile2 = 256,

    /// <summary>
    /// Include DDL statements in online catalog.
    /// </summary>
    DdlDictFromOnlineCatalog2 = 512,

    /// <summary>
    /// Include DDL statements in redo log files.
    /// </summary>
    DdlDictFromRedoLogs2 = 1024,

    /// <summary>
    /// Include DDL statements in dictionary file.
    /// </summary>
    DdlDictFromDictionaryFile3 = 2048,

    /// <summary>
    /// Include DDL statements in online catalog.
    /// </summary>
    DdlDictFromOnlineCatalog3 = 4096,

    /// <summary>
    /// Include DDL statements in redo log files.
    /// </summary>
    DdlDictFromRedoLogs3 = 8192
}

/// <summary>
/// Retry configuration for Oracle operations.
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

/// <summary>
/// Polling configuration for Oracle operations.
/// </summary>
public sealed class PollingOptions
{
    /// <summary>
    /// Gets or sets the polling interval in seconds.
    /// </summary>
    public int IntervalSeconds { get; set; } = 5;

    /// <summary>
    /// Gets or sets the maximum number of records to process per poll.
    /// </summary>
    public int MaxRecordsPerPoll { get; set; } = 1000;

    /// <summary>
    /// Gets or sets whether to enable polling.
    /// </summary>
    public bool Enabled { get; set; } = true;
}