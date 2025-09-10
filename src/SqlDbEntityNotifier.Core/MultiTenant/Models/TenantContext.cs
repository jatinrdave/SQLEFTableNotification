namespace SqlDbEntityNotifier.Core.MultiTenant.Models;

/// <summary>
/// Represents a tenant context for multi-tenant operations.
/// </summary>
public sealed class TenantContext
{
    /// <summary>
    /// Gets or sets the tenant identifier.
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the tenant name.
    /// </summary>
    public string TenantName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the tenant configuration.
    /// </summary>
    public TenantConfiguration Configuration { get; set; } = new();

    /// <summary>
    /// Gets or sets the tenant metadata.
    /// </summary>
    public IDictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Gets or sets the tenant creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the tenant last updated timestamp.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets whether the tenant is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the tenant isolation level.
    /// </summary>
    public TenantIsolationLevel IsolationLevel { get; set; } = TenantIsolationLevel.Database;

    /// <summary>
    /// Gets or sets the tenant resource limits.
    /// </summary>
    public TenantResourceLimits ResourceLimits { get; set; } = new();
}

/// <summary>
/// Tenant configuration for multi-tenant operations.
/// </summary>
public sealed class TenantConfiguration
{
    /// <summary>
    /// Gets or sets the database connection string for the tenant.
    /// </summary>
    public string? DatabaseConnectionString { get; set; }

    /// <summary>
    /// Gets or sets the database adapter type for the tenant.
    /// </summary>
    public string? DatabaseAdapterType { get; set; }

    /// <summary>
    /// Gets or sets the publisher configuration for the tenant.
    /// </summary>
    public PublisherConfiguration Publisher { get; set; } = new();

    /// <summary>
    /// Gets or sets the serializer configuration for the tenant.
    /// </summary>
    public SerializerConfiguration Serializer { get; set; } = new();

    /// <summary>
    /// Gets or sets the filtering configuration for the tenant.
    /// </summary>
    public FilteringConfiguration Filtering { get; set; } = new();

    /// <summary>
    /// Gets or sets the monitoring configuration for the tenant.
    /// </summary>
    public MonitoringConfiguration Monitoring { get; set; } = new();
}

/// <summary>
/// Publisher configuration for a tenant.
/// </summary>
public sealed class PublisherConfiguration
{
    /// <summary>
    /// Gets or sets the publisher type.
    /// </summary>
    public string Type { get; set; } = "Kafka";

    /// <summary>
    /// Gets or sets the publisher settings.
    /// </summary>
    public IDictionary<string, string> Settings { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Gets or sets the topic prefix for the tenant.
    /// </summary>
    public string TopicPrefix { get; set; } = string.Empty;
}

/// <summary>
/// Serializer configuration for a tenant.
/// </summary>
public sealed class SerializerConfiguration
{
    /// <summary>
    /// Gets or sets the serializer type.
    /// </summary>
    public string Type { get; set; } = "Json";

    /// <summary>
    /// Gets or sets the serializer settings.
    /// </summary>
    public IDictionary<string, string> Settings { get; set; } = new Dictionary<string, string>();
}

/// <summary>
/// Filtering configuration for a tenant.
/// </summary>
public sealed class FilteringConfiguration
{
    /// <summary>
    /// Gets or sets the list of tables to monitor.
    /// </summary>
    public IList<string> MonitoredTables { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets the list of tables to exclude.
    /// </summary>
    public IList<string> ExcludedTables { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets the list of schemas to monitor.
    /// </summary>
    public IList<string> MonitoredSchemas { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets the list of schemas to exclude.
    /// </summary>
    public IList<string> ExcludedSchemas { get; set; } = new List<string>();
}

/// <summary>
/// Monitoring configuration for a tenant.
/// </summary>
public sealed class MonitoringConfiguration
{
    /// <summary>
    /// Gets or sets whether monitoring is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the metrics collection interval in seconds.
    /// </summary>
    public int MetricsIntervalSeconds { get; set; } = 60;

    /// <summary>
    /// Gets or sets the health check interval in seconds.
    /// </summary>
    public int HealthCheckIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the alerting configuration.
    /// </summary>
    public AlertingConfiguration Alerting { get; set; } = new();
}

/// <summary>
/// Alerting configuration for a tenant.
/// </summary>
public sealed class AlertingConfiguration
{
    /// <summary>
    /// Gets or sets whether alerting is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the alert thresholds.
    /// </summary>
    public AlertThresholds Thresholds { get; set; } = new();

    /// <summary>
    /// Gets or sets the notification channels.
    /// </summary>
    public IList<NotificationChannel> NotificationChannels { get; set; } = new List<NotificationChannel>();
}

/// <summary>
/// Alert thresholds for a tenant.
/// </summary>
public sealed class AlertThresholds
{
    /// <summary>
    /// Gets or sets the maximum error rate threshold.
    /// </summary>
    public double MaxErrorRate { get; set; } = 0.05; // 5%

    /// <summary>
    /// Gets or sets the maximum latency threshold in milliseconds.
    /// </summary>
    public long MaxLatencyMs { get; set; } = 5000; // 5 seconds

    /// <summary>
    /// Gets or sets the maximum memory usage threshold in MB.
    /// </summary>
    public long MaxMemoryUsageMb { get; set; } = 1024; // 1 GB

    /// <summary>
    /// Gets or sets the maximum CPU usage threshold.
    /// </summary>
    public double MaxCpuUsage { get; set; } = 0.8; // 80%
}

/// <summary>
/// Notification channel for alerts.
/// </summary>
public sealed class NotificationChannel
{
    /// <summary>
    /// Gets or sets the channel type.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the channel configuration.
    /// </summary>
    public IDictionary<string, string> Configuration { get; set; } = new Dictionary<string, string>();
}

/// <summary>
/// Tenant resource limits.
/// </summary>
public sealed class TenantResourceLimits
{
    /// <summary>
    /// Gets or sets the maximum number of concurrent connections.
    /// </summary>
    public int MaxConnections { get; set; } = 100;

    /// <summary>
    /// Gets or sets the maximum number of events per second.
    /// </summary>
    public int MaxEventsPerSecond { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the maximum number of subscriptions.
    /// </summary>
    public int MaxSubscriptions { get; set; } = 50;

    /// <summary>
    /// Gets or sets the maximum storage size in MB.
    /// </summary>
    public long MaxStorageSizeMb { get; set; } = 10240; // 10 GB

    /// <summary>
    /// Gets or sets the maximum memory usage in MB.
    /// </summary>
    public long MaxMemoryUsageMb { get; set; } = 512; // 512 MB
}

/// <summary>
/// Tenant isolation levels.
/// </summary>
public enum TenantIsolationLevel
{
    /// <summary>
    /// Shared database with tenant-specific schemas.
    /// </summary>
    Schema,

    /// <summary>
    /// Separate database per tenant.
    /// </summary>
    Database,

    /// <summary>
    /// Separate database server per tenant.
    /// </summary>
    Server,

    /// <summary>
    /// Separate application instance per tenant.
    /// </summary>
    Instance
}