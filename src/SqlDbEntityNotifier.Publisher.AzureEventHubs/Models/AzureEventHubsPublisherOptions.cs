namespace SqlDbEntityNotifier.Publisher.AzureEventHubs.Models;

/// <summary>
/// Configuration options for the Azure Event Hubs publisher.
/// </summary>
public sealed class AzureEventHubsPublisherOptions
{
    /// <summary>
    /// Gets or sets the Event Hubs connection string.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Event Hub name.
    /// </summary>
    public string EventHubName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the fully qualified namespace of the Event Hub.
    /// </summary>
    public string FullyQualifiedNamespace { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the authentication configuration.
    /// </summary>
    public AuthenticationOptions Authentication { get; set; } = new();

    /// <summary>
    /// Gets or sets the batching configuration.
    /// </summary>
    public BatchingOptions Batching { get; set; } = new();

    /// <summary>
    /// Gets or sets the retry configuration.
    /// </summary>
    public RetryOptions Retry { get; set; } = new();

    /// <summary>
    /// Gets or sets the partition key format. Use placeholders: {source}, {schema}, {table}.
    /// </summary>
    public string PartitionKeyFormat { get; set; } = "{source}.{schema}.{table}";

    /// <summary>
    /// Gets or sets the default partition key if format cannot be applied.
    /// </summary>
    public string DefaultPartitionKey { get; set; } = "default";

    /// <summary>
    /// Gets or sets whether to enable idempotent publishing.
    /// </summary>
    public bool EnableIdempotentPublishing { get; set; } = true;

    /// <summary>
    /// Gets or sets the producer options.
    /// </summary>
    public ProducerOptions Producer { get; set; } = new();
}

/// <summary>
/// Authentication configuration for Azure Event Hubs.
/// </summary>
public sealed class AuthenticationOptions
{
    /// <summary>
    /// Gets or sets the authentication type.
    /// </summary>
    public AuthenticationType Type { get; set; } = AuthenticationType.ConnectionString;

    /// <summary>
    /// Gets or sets the client ID for service principal authentication.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the client secret for service principal authentication.
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the tenant ID for service principal authentication.
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the managed identity client ID.
    /// </summary>
    public string ManagedIdentityClientId { get; set; } = string.Empty;
}

/// <summary>
/// Authentication types for Azure Event Hubs.
/// </summary>
public enum AuthenticationType
{
    /// <summary>
    /// Use connection string authentication.
    /// </summary>
    ConnectionString,

    /// <summary>
    /// Use service principal authentication.
    /// </summary>
    ServicePrincipal,

    /// <summary>
    /// Use managed identity authentication.
    /// </summary>
    ManagedIdentity
}

/// <summary>
/// Batching configuration for Azure Event Hubs.
/// </summary>
public sealed class BatchingOptions
{
    /// <summary>
    /// Gets or sets the maximum number of events in a batch.
    /// </summary>
    public int MaxEventCount { get; set; } = 100;

    /// <summary>
    /// Gets or sets the maximum wait time for batching in milliseconds.
    /// </summary>
    public int MaxWaitTimeMs { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the maximum size of a batch in bytes.
    /// </summary>
    public int MaxSizeBytes { get; set; } = 1048576; // 1MB

    /// <summary>
    /// Gets or sets whether to enable batching.
    /// </summary>
    public bool Enabled { get; set; } = true;
}

/// <summary>
/// Retry configuration for Azure Event Hubs.
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
/// Producer configuration for Azure Event Hubs.
/// </summary>
public sealed class ProducerOptions
{
    /// <summary>
    /// Gets or sets the producer identifier.
    /// </summary>
    public string Identifier { get; set; } = "sqldb-notifier";

    /// <summary>
    /// Gets or sets the connection idle timeout in seconds.
    /// </summary>
    public int ConnectionIdleTimeoutSeconds { get; set; } = 300;

    /// <summary>
    /// Gets or sets the retry policy.
    /// </summary>
    public string RetryPolicy { get; set; } = "ExponentialBackoff";
}