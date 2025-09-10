namespace SqlDbEntityNotifier.Core.Transactional.Models;

/// <summary>
/// Configuration options for transactional grouping.
/// </summary>
public sealed class TransactionalGroupOptions
{
    /// <summary>
    /// Gets or sets whether transactional grouping is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of concurrent transactions.
    /// </summary>
    public int MaxConcurrentTransactions { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the default transaction timeout in seconds.
    /// </summary>
    public int DefaultTimeoutSeconds { get; set; } = 300; // 5 minutes

    /// <summary>
    /// Gets or sets the maximum number of events per transaction.
    /// </summary>
    public int MaxEventsPerTransaction { get; set; } = 10000;

    /// <summary>
    /// Gets or sets whether to require exactly-once delivery.
    /// </summary>
    public bool RequireExactlyOnce { get; set; } = true;

    /// <summary>
    /// Gets or sets the transaction retention period in days.
    /// </summary>
    public int RetentionDays { get; set; } = 30;

    /// <summary>
    /// Gets or sets the cleanup interval in minutes.
    /// </summary>
    public int CleanupIntervalMinutes { get; set; } = 60;

    /// <summary>
    /// Gets or sets the timeout processing interval in minutes.
    /// </summary>
    public int TimeoutProcessingIntervalMinutes { get; set; } = 5;

    /// <summary>
    /// Gets or sets the maximum retry attempts for failed transactions.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Gets or sets the retry delay in seconds.
    /// </summary>
    public int RetryDelaySeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the retry backoff multiplier.
    /// </summary>
    public double RetryBackoffMultiplier { get; set; } = 2.0;

    /// <summary>
    /// Gets or sets the maximum retry delay in seconds.
    /// </summary>
    public int MaxRetryDelaySeconds { get; set; } = 300; // 5 minutes

    /// <summary>
    /// Gets or sets whether to enable transaction checksums.
    /// </summary>
    public bool EnableChecksums { get; set; } = true;

    /// <summary>
    /// Gets or sets the checksum algorithm.
    /// </summary>
    public ChecksumAlgorithm ChecksumAlgorithm { get; set; } = ChecksumAlgorithm.SHA256;

    /// <summary>
    /// Gets or sets the transaction batching configuration.
    /// </summary>
    public TransactionBatchingOptions Batching { get; set; } = new();

    /// <summary>
    /// Gets or sets the transaction monitoring configuration.
    /// </summary>
    public TransactionMonitoringOptions Monitoring { get; set; } = new();

    /// <summary>
    /// Gets or sets the transaction validation configuration.
    /// </summary>
    public TransactionValidationOptions Validation { get; set; } = new();
}

/// <summary>
/// Transaction batching configuration.
/// </summary>
public sealed class TransactionBatchingOptions
{
    /// <summary>
    /// Gets or sets whether transaction batching is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum batch size.
    /// </summary>
    public int MaxBatchSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets the batch timeout in seconds.
    /// </summary>
    public int BatchTimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// Gets or sets the maximum batch wait time in seconds.
    /// </summary>
    public int MaxBatchWaitSeconds { get; set; } = 30;
}

/// <summary>
/// Transaction monitoring configuration.
/// </summary>
public sealed class TransactionMonitoringOptions
{
    /// <summary>
    /// Gets or sets whether transaction monitoring is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the monitoring interval in seconds.
    /// </summary>
    public int MonitoringIntervalSeconds { get; set; } = 60;

    /// <summary>
    /// Gets or sets whether to collect detailed metrics.
    /// </summary>
    public bool CollectDetailedMetrics { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to generate alerts for transaction issues.
    /// </summary>
    public bool GenerateAlerts { get; set; } = true;

    /// <summary>
    /// Gets or sets the alert thresholds.
    /// </summary>
    public TransactionAlertThresholds AlertThresholds { get; set; } = new();
}

/// <summary>
/// Transaction validation configuration.
/// </summary>
public sealed class TransactionValidationOptions
{
    /// <summary>
    /// Gets or sets whether transaction validation is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to validate transaction integrity.
    /// </summary>
    public bool ValidateIntegrity { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to validate transaction size.
    /// </summary>
    public bool ValidateSize { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to validate transaction timeout.
    /// </summary>
    public bool ValidateTimeout { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to validate transaction sequence.
    /// </summary>
    public bool ValidateSequence { get; set; } = true;
}

/// <summary>
/// Transaction alert thresholds.
/// </summary>
public sealed class TransactionAlertThresholds
{
    /// <summary>
    /// Gets or sets the threshold for failed transactions per minute.
    /// </summary>
    public int FailedTransactionsPerMinute { get; set; } = 10;

    /// <summary>
    /// Gets or sets the threshold for timed out transactions per minute.
    /// </summary>
    public int TimedOutTransactionsPerMinute { get; set; } = 5;

    /// <summary>
    /// Gets or sets the threshold for active transactions.
    /// </summary>
    public int MaxActiveTransactions { get; set; } = 500;

    /// <summary>
    /// Gets or sets the threshold for transaction duration in seconds.
    /// </summary>
    public int MaxTransactionDurationSeconds { get; set; } = 600; // 10 minutes
}

/// <summary>
/// Checksum algorithms for transaction integrity.
/// </summary>
public enum ChecksumAlgorithm
{
    /// <summary>
    /// MD5 checksum algorithm.
    /// </summary>
    MD5,

    /// <summary>
    /// SHA1 checksum algorithm.
    /// </summary>
    SHA1,

    /// <summary>
    /// SHA256 checksum algorithm.
    /// </summary>
    SHA256,

    /// <summary>
    /// SHA512 checksum algorithm.
    /// </summary>
    SHA512
}