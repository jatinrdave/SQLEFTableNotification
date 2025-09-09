namespace SqlDbEntityNotifier.Publisher.Kafka.Models;

/// <summary>
/// Configuration options for the Kafka publisher.
/// </summary>
public sealed class KafkaPublisherOptions
{
    /// <summary>
    /// Gets or sets the Kafka bootstrap servers.
    /// </summary>
    public string BootstrapServers { get; set; } = "localhost:9092";

    /// <summary>
    /// Gets or sets the topic name format. Use placeholders: {source}, {schema}, {table}.
    /// </summary>
    public string TopicFormat { get; set; } = "{source}.{schema}.{table}";

    /// <summary>
    /// Gets or sets the default topic name if format cannot be applied.
    /// </summary>
    public string DefaultTopic { get; set; } = "sqldb-changes";

    /// <summary>
    /// Gets or sets the client ID for the Kafka producer.
    /// </summary>
    public string ClientId { get; set; } = "sqldb-notifier";

    /// <summary>
    /// Gets or sets the acknowledgment level (0, 1, or all).
    /// </summary>
    public string Acks { get; set; } = "all";

    /// <summary>
    /// Gets or sets the retry configuration.
    /// </summary>
    public RetryOptions Retry { get; set; } = new();

    /// <summary>
    /// Gets or sets the batch configuration.
    /// </summary>
    public BatchOptions Batch { get; set; } = new();

    /// <summary>
    /// Gets or sets additional producer configuration properties.
    /// </summary>
    public IDictionary<string, string> AdditionalProperties { get; set; } = new Dictionary<string, string>();
}

/// <summary>
/// Retry configuration for Kafka publisher.
/// </summary>
public sealed class RetryOptions
{
    /// <summary>
    /// Gets or sets the maximum number of retries.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Gets or sets the retry backoff in seconds.
    /// </summary>
    public int BackoffSeconds { get; set; } = 2;

    /// <summary>
    /// Gets or sets whether to enable retries.
    /// </summary>
    public bool Enabled { get; set; } = true;
}

/// <summary>
/// Batch configuration for Kafka publisher.
/// </summary>
public sealed class BatchOptions
{
    /// <summary>
    /// Gets or sets the batch size in bytes.
    /// </summary>
    public int Size { get; set; } = 16384;

    /// <summary>
    /// Gets or sets the linger time in milliseconds.
    /// </summary>
    public int LingerMs { get; set; } = 5;

    /// <summary>
    /// Gets or sets whether to enable batching.
    /// </summary>
    public bool Enabled { get; set; } = true;
}