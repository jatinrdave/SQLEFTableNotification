namespace SqlDbEntityNotifier.Core.Transactional.Models;

/// <summary>
/// Represents a transactional group for exactly-once semantics.
/// </summary>
public sealed class TransactionalGroup
{
    /// <summary>
    /// Gets or sets the transaction ID.
    /// </summary>
    public string TransactionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the source database identifier.
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the tenant ID (if multi-tenant).
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// Gets or sets the transaction start timestamp.
    /// </summary>
    public DateTime StartTimestamp { get; set; }

    /// <summary>
    /// Gets or sets the transaction end timestamp.
    /// </summary>
    public DateTime? EndTimestamp { get; set; }

    /// <summary>
    /// Gets or sets the transaction status.
    /// </summary>
    public TransactionStatus Status { get; set; } = TransactionStatus.Active;

    /// <summary>
    /// Gets or sets the list of change events in this transaction.
    /// </summary>
    public IList<ChangeEvent> ChangeEvents { get; set; } = new List<ChangeEvent>();

    /// <summary>
    /// Gets or sets the transaction metadata.
    /// </summary>
    public IDictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Gets or sets the transaction sequence number.
    /// </summary>
    public long SequenceNumber { get; set; }

    /// <summary>
    /// Gets or sets the transaction checksum for integrity verification.
    /// </summary>
    public string? Checksum { get; set; }

    /// <summary>
    /// Gets or sets the retry count for failed transactions.
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// Gets or sets the last error message (if any).
    /// </summary>
    public string? LastError { get; set; }

    /// <summary>
    /// Gets or sets the transaction priority.
    /// </summary>
    public TransactionPriority Priority { get; set; } = TransactionPriority.Normal;

    /// <summary>
    /// Gets or sets the transaction timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 300; // 5 minutes

    /// <summary>
    /// Gets or sets whether this transaction requires exactly-once delivery.
    /// </summary>
    public bool RequiresExactlyOnce { get; set; } = true;

    /// <summary>
    /// Gets or sets the delivery attempts for this transaction.
    /// </summary>
    public IList<DeliveryAttempt> DeliveryAttempts { get; set; } = new List<DeliveryAttempt>();

    /// <summary>
    /// Gets the total number of change events in this transaction.
    /// </summary>
    public int EventCount => ChangeEvents.Count;

    /// <summary>
    /// Gets whether this transaction is completed.
    /// </summary>
    public bool IsCompleted => Status == TransactionStatus.Committed || Status == TransactionStatus.RolledBack;

    /// <summary>
    /// Gets whether this transaction is active.
    /// </summary>
    public bool IsActive => Status == TransactionStatus.Active;

    /// <summary>
    /// Gets the transaction duration.
    /// </summary>
    public TimeSpan? Duration => EndTimestamp.HasValue ? EndTimestamp.Value - StartTimestamp : null;
}

/// <summary>
/// Represents a delivery attempt for a transactional group.
/// </summary>
public sealed class DeliveryAttempt
{
    /// <summary>
    /// Gets or sets the attempt number.
    /// </summary>
    public int AttemptNumber { get; set; }

    /// <summary>
    /// Gets or sets the attempt timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the delivery status.
    /// </summary>
    public DeliveryStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the error message (if failed).
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the delivery duration.
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Gets or sets the publisher that handled this attempt.
    /// </summary>
    public string Publisher { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the delivery metadata.
    /// </summary>
    public IDictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
}

/// <summary>
/// Transaction status enumeration.
/// </summary>
public enum TransactionStatus
{
    /// <summary>
    /// Transaction is active and collecting events.
    /// </summary>
    Active,

    /// <summary>
    /// Transaction is being prepared for delivery.
    /// </summary>
    Preparing,

    /// <summary>
    /// Transaction is being delivered.
    /// </summary>
    Delivering,

    /// <summary>
    /// Transaction has been successfully committed.
    /// </summary>
    Committed,

    /// <summary>
    /// Transaction has been rolled back.
    /// </summary>
    RolledBack,

    /// <summary>
    /// Transaction delivery failed.
    /// </summary>
    Failed,

    /// <summary>
    /// Transaction has timed out.
    /// </summary>
    Timeout,

    /// <summary>
    /// Transaction is being retried.
    /// </summary>
    Retrying
}

/// <summary>
/// Delivery status enumeration.
/// </summary>
public enum DeliveryStatus
{
    /// <summary>
    /// Delivery attempt is pending.
    /// </summary>
    Pending,

    /// <summary>
    /// Delivery attempt is in progress.
    /// </summary>
    InProgress,

    /// <summary>
    /// Delivery attempt was successful.
    /// </summary>
    Successful,

    /// <summary>
    /// Delivery attempt failed.
    /// </summary>
    Failed,

    /// <summary>
    /// Delivery attempt timed out.
    /// </summary>
    Timeout,

    /// <summary>
    /// Delivery attempt was cancelled.
    /// </summary>
    Cancelled
}

/// <summary>
/// Transaction priority enumeration.
/// </summary>
public enum TransactionPriority
{
    /// <summary>
    /// Low priority transaction.
    /// </summary>
    Low = 1,

    /// <summary>
    /// Normal priority transaction.
    /// </summary>
    Normal = 2,

    /// <summary>
    /// High priority transaction.
    /// </summary>
    High = 3,

    /// <summary>
    /// Critical priority transaction.
    /// </summary>
    Critical = 4
}