namespace SqlDbEntityNotifier.Publisher.RabbitMQ.Models;

/// <summary>
/// Configuration options for the RabbitMQ publisher.
/// </summary>
public sealed class RabbitMQPublisherOptions
{
    /// <summary>
    /// Gets or sets the RabbitMQ connection string or host name.
    /// </summary>
    public string HostName { get; set; } = "localhost";

    /// <summary>
    /// Gets or sets the RabbitMQ port.
    /// </summary>
    public int Port { get; set; } = 5672;

    /// <summary>
    /// Gets or sets the virtual host.
    /// </summary>
    public string VirtualHost { get; set; } = "/";

    /// <summary>
    /// Gets or sets the username for authentication.
    /// </summary>
    public string UserName { get; set; } = "guest";

    /// <summary>
    /// Gets or sets the password for authentication.
    /// </summary>
    public string Password { get; set; } = "guest";

    /// <summary>
    /// Gets or sets the exchange name.
    /// </summary>
    public string ExchangeName { get; set; } = "sqldb.changes";

    /// <summary>
    /// Gets or sets the exchange type (direct, topic, fanout, headers).
    /// </summary>
    public string ExchangeType { get; set; } = "topic";

    /// <summary>
    /// Gets or sets whether the exchange is durable.
    /// </summary>
    public bool ExchangeDurable { get; set; } = true;

    /// <summary>
    /// Gets or sets the routing key format. Use placeholders: {source}, {schema}, {table}, {operation}.
    /// </summary>
    public string RoutingKeyFormat { get; set; } = "{source}.{schema}.{table}.{operation}";

    /// <summary>
    /// Gets or sets the default routing key if format cannot be applied.
    /// </summary>
    public string DefaultRoutingKey { get; set; } = "sqldb.changes";

    /// <summary>
    /// Gets or sets whether messages should be persistent.
    /// </summary>
    public bool PersistentMessages { get; set; } = true;

    /// <summary>
    /// Gets or sets the retry configuration.
    /// </summary>
    public RetryOptions Retry { get; set; } = new();

    /// <summary>
    /// Gets or sets the connection configuration.
    /// </summary>
    public ConnectionOptions Connection { get; set; } = new();

    /// <summary>
    /// Gets or sets additional connection properties.
    /// </summary>
    public IDictionary<string, object> ConnectionProperties { get; set; } = new Dictionary<string, object>();
}

/// <summary>
/// Retry configuration for RabbitMQ publisher.
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
/// Connection configuration for RabbitMQ publisher.
/// </summary>
public sealed class ConnectionOptions
{
    /// <summary>
    /// Gets or sets the connection timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the heartbeat interval in seconds.
    /// </summary>
    public int HeartbeatSeconds { get; set; } = 60;

    /// <summary>
    /// Gets or sets whether to use automatic recovery.
    /// </summary>
    public bool AutomaticRecoveryEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the network recovery interval in seconds.
    /// </summary>
    public int NetworkRecoveryIntervalSeconds { get; set; } = 5;

    /// <summary>
    /// Gets or sets the maximum number of channels per connection.
    /// </summary>
    public int MaxChannels { get; set; } = 200;
}