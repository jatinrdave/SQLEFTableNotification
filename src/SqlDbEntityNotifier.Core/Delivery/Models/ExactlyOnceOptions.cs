namespace SqlDbEntityNotifier.Core.Delivery.Models;

/// <summary>
/// Configuration options for exactly-once delivery semantics.
/// </summary>
public sealed class ExactlyOnceOptions
{
    /// <summary>
    /// Gets or sets whether exactly-once delivery is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the delivery guarantee level.
    /// </summary>
    public DeliveryGuarantee Guarantee { get; set; } = DeliveryGuarantee.ExactlyOnce;

    /// <summary>
    /// Gets or sets the idempotency configuration.
    /// </summary>
    public IdempotencyOptions Idempotency { get; set; } = new();

    /// <summary>
    /// Gets or sets the deduplication configuration.
    /// </summary>
    public DeduplicationOptions Deduplication { get; set; } = new();

    /// <summary>
    /// Gets or sets the acknowledgment configuration.
    /// </summary>
    public AcknowledgmentOptions Acknowledgment { get; set; } = new();

    /// <summary>
    /// Gets or sets the retry configuration.
    /// </summary>
    public DeliveryRetryOptions Retry { get; set; } = new();

    /// <summary>
    /// Gets or sets the monitoring configuration.
    /// </summary>
    public DeliveryMonitoringOptions Monitoring { get; set; } = new();
}

/// <summary>
/// Idempotency configuration for exactly-once delivery.
/// </summary>
public sealed class IdempotencyOptions
{
    /// <summary>
    /// Gets or sets whether idempotency is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the idempotency key generation strategy.
    /// </summary>
    public IdempotencyKeyStrategy KeyStrategy { get; set; } = IdempotencyKeyStrategy.Composite;

    /// <summary>
    /// Gets or sets the idempotency key TTL in seconds.
    /// </summary>
    public int KeyTtlSeconds { get; set; } = 3600; // 1 hour

    /// <summary>
    /// Gets or sets the maximum number of idempotency keys to store.
    /// </summary>
    public int MaxKeys { get; set; } = 100000;

    /// <summary>
    /// Gets or sets the idempotency key cleanup interval in minutes.
    /// </summary>
    public int CleanupIntervalMinutes { get; set; } = 60;
}

/// <summary>
/// Deduplication configuration for exactly-once delivery.
/// </summary>
public sealed class DeduplicationOptions
{
    /// <summary>
    /// Gets or sets whether deduplication is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the deduplication window in seconds.
    /// </summary>
    public int WindowSeconds { get; set; } = 300; // 5 minutes

    /// <summary>
    /// Gets or sets the deduplication algorithm.
    /// </summary>
    public DeduplicationAlgorithm Algorithm { get; set; } = DeduplicationAlgorithm.ContentHash;

    /// <summary>
    /// Gets or sets the maximum number of deduplication entries.
    /// </summary>
    public int MaxEntries { get; set; } = 1000000;

    /// <summary>
    /// Gets or sets the deduplication cleanup interval in minutes.
    /// </summary>
    public int CleanupIntervalMinutes { get; set; } = 30;
}

/// <summary>
/// Acknowledgment configuration for exactly-once delivery.
/// </summary>
public sealed class AcknowledgmentOptions
{
    /// <summary>
    /// Gets or sets whether acknowledgments are required.
    /// </summary>
    public bool Required { get; set; } = true;

    /// <summary>
    /// Gets or sets the acknowledgment timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the maximum number of acknowledgment retries.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Gets or sets the acknowledgment retry delay in seconds.
    /// </summary>
    public int RetryDelaySeconds { get; set; } = 5;

    /// <summary>
    /// Gets or sets the acknowledgment strategy.
    /// </summary>
    public AcknowledgmentStrategy Strategy { get; set; } = AcknowledgmentStrategy.AtLeastOnce;
}

/// <summary>
/// Delivery retry configuration.
/// </summary>
public sealed class DeliveryRetryOptions
{
    /// <summary>
    /// Gets or sets whether retries are enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of retry attempts.
    /// </summary>
    public int MaxAttempts { get; set; } = 3;

    /// <summary>
    /// Gets or sets the initial retry delay in seconds.
    /// </summary>
    public int InitialDelaySeconds { get; set; } = 1;

    /// <summary>
    /// Gets or sets the maximum retry delay in seconds.
    /// </summary>
    public int MaxDelaySeconds { get; set; } = 60;

    /// <summary>
    /// Gets or sets the retry backoff multiplier.
    /// </summary>
    public double BackoffMultiplier { get; set; } = 2.0;

    /// <summary>
    /// Gets or sets the retry strategy.
    /// </summary>
    public RetryStrategy Strategy { get; set; } = RetryStrategy.ExponentialBackoff;
}

/// <summary>
/// Delivery monitoring configuration.
/// </summary>
public sealed class DeliveryMonitoringOptions
{
    /// <summary>
    /// Gets or sets whether delivery monitoring is enabled.
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
    /// Gets or sets whether to generate alerts for delivery issues.
    /// </summary>
    public bool GenerateAlerts { get; set; } = true;

    /// <summary>
    /// Gets or sets the alert thresholds.
    /// </summary>
    public DeliveryAlertThresholds AlertThresholds { get; set; } = new();
}

/// <summary>
/// Delivery alert thresholds.
/// </summary>
public sealed class DeliveryAlertThresholds
{
    /// <summary>
    /// Gets or sets the threshold for failed deliveries per minute.
    /// </summary>
    public int FailedDeliveriesPerMinute { get; set; } = 10;

    /// <summary>
    /// Gets or sets the threshold for duplicate deliveries per minute.
    /// </summary>
    public int DuplicateDeliveriesPerMinute { get; set; } = 5;

    /// <summary>
    /// Gets or sets the threshold for acknowledgment timeouts per minute.
    /// </summary>
    public int AcknowledgmentTimeoutsPerMinute { get; set; } = 5;

    /// <summary>
    /// Gets or sets the threshold for delivery latency in milliseconds.
    /// </summary>
    public long MaxDeliveryLatencyMs { get; set; } = 5000; // 5 seconds
}

/// <summary>
/// Delivery guarantee levels.
/// </summary>
public enum DeliveryGuarantee
{
    /// <summary>
    /// At most once delivery (no duplicates, may lose messages).
    /// </summary>
    AtMostOnce,

    /// <summary>
    /// At least once delivery (no message loss, may have duplicates).
    /// </summary>
    AtLeastOnce,

    /// <summary>
    /// Exactly once delivery (no duplicates, no message loss).
    /// </summary>
    ExactlyOnce
}

/// <summary>
/// Idempotency key generation strategies.
/// </summary>
public enum IdempotencyKeyStrategy
{
    /// <summary>
    /// Use a composite key based on multiple fields.
    /// </summary>
    Composite,

    /// <summary>
    /// Use the event offset as the key.
    /// </summary>
    Offset,

    /// <summary>
    /// Use a hash of the event content.
    /// </summary>
    ContentHash,

    /// <summary>
    /// Use a custom key generator.
    /// </summary>
    Custom
}

/// <summary>
/// Deduplication algorithms.
/// </summary>
public enum DeduplicationAlgorithm
{
    /// <summary>
    /// Use content hash for deduplication.
    /// </summary>
    ContentHash,

    /// <summary>
    /// Use event ID for deduplication.
    /// </summary>
    EventId,

    /// <summary>
    /// Use composite key for deduplication.
    /// </summary>
    CompositeKey,

    /// <summary>
    /// Use timestamp and content for deduplication.
    /// </summary>
    TimestampAndContent
}

/// <summary>
/// Acknowledgment strategies.
/// </summary>
public enum AcknowledgmentStrategy
{
    /// <summary>
    /// At least once acknowledgment.
    /// </summary>
    AtLeastOnce,

    /// <summary>
    /// Exactly once acknowledgment.
    /// </summary>
    ExactlyOnce,

    /// <summary>
    /// Fire and forget (no acknowledgment).
    /// </summary>
    FireAndForget
}

/// <summary>
/// Retry strategies.
/// </summary>
public enum RetryStrategy
{
    /// <summary>
    /// Fixed delay between retries.
    /// </summary>
    FixedDelay,

    /// <summary>
    /// Exponential backoff between retries.
    /// </summary>
    ExponentialBackoff,

    /// <summary>
    /// Linear backoff between retries.
    /// </summary>
    LinearBackoff,

    /// <summary>
    /// Custom retry strategy.
    /// </summary>
    Custom
}