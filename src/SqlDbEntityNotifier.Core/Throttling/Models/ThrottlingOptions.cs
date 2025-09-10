namespace SqlDbEntityNotifier.Core.Throttling.Models;

/// <summary>
/// Configuration options for throttling and rate limiting.
/// </summary>
public sealed class ThrottlingOptions
{
    /// <summary>
    /// Gets or sets whether throttling is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the global throttling configuration.
    /// </summary>
    public GlobalThrottlingOptions Global { get; set; } = new();

    /// <summary>
    /// Gets or sets the per-tenant throttling configuration.
    /// </summary>
    public PerTenantThrottlingOptions PerTenant { get; set; } = new();

    /// <summary>
    /// Gets or sets the throttling algorithm configuration.
    /// </summary>
    public ThrottlingAlgorithmOptions Algorithm { get; set; } = new();

    /// <summary>
    /// Gets or sets the throttling monitoring configuration.
    /// </summary>
    public ThrottlingMonitoringOptions Monitoring { get; set; } = new();
}

/// <summary>
/// Global throttling configuration.
/// </summary>
public sealed class GlobalThrottlingOptions
{
    /// <summary>
    /// Gets or sets the maximum number of events per second globally.
    /// </summary>
    public int MaxEventsPerSecond { get; set; } = 10000;

    /// <summary>
    /// Gets or sets the maximum number of concurrent connections globally.
    /// </summary>
    public int MaxConcurrentConnections { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the maximum number of concurrent subscriptions globally.
    /// </summary>
    public int MaxConcurrentSubscriptions { get; set; } = 500;

    /// <summary>
    /// Gets or sets the maximum memory usage in MB globally.
    /// </summary>
    public long MaxMemoryUsageMb { get; set; } = 8192; // 8 GB

    /// <summary>
    /// Gets or sets the maximum CPU usage percentage globally.
    /// </summary>
    public double MaxCpuUsage { get; set; } = 0.8; // 80%

    /// <summary>
    /// Gets or sets the burst allowance multiplier.
    /// </summary>
    public double BurstMultiplier { get; set; } = 2.0;
}

/// <summary>
/// Per-tenant throttling configuration.
/// </summary>
public sealed class PerTenantThrottlingOptions
{
    /// <summary>
    /// Gets or sets the default maximum number of events per second per tenant.
    /// </summary>
    public int DefaultMaxEventsPerSecond { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the default maximum number of concurrent connections per tenant.
    /// </summary>
    public int DefaultMaxConcurrentConnections { get; set; } = 100;

    /// <summary>
    /// Gets or sets the default maximum number of concurrent subscriptions per tenant.
    /// </summary>
    public int DefaultMaxConcurrentSubscriptions { get; set; } = 50;

    /// <summary>
    /// Gets or sets the default maximum memory usage in MB per tenant.
    /// </summary>
    public long DefaultMaxMemoryUsageMb { get; set; } = 512; // 512 MB

    /// <summary>
    /// Gets or sets the default maximum CPU usage percentage per tenant.
    /// </summary>
    public double DefaultMaxCpuUsage { get; set; } = 0.1; // 10%

    /// <summary>
    /// Gets or sets the default burst allowance multiplier per tenant.
    /// </summary>
    public double DefaultBurstMultiplier { get; set; } = 1.5;

    /// <summary>
    /// Gets or sets the tenant-specific throttling configurations.
    /// </summary>
    public IDictionary<string, TenantThrottlingConfig> TenantConfigs { get; set; } = new Dictionary<string, TenantThrottlingConfig>();
}

/// <summary>
/// Tenant-specific throttling configuration.
/// </summary>
public sealed class TenantThrottlingConfig
{
    /// <summary>
    /// Gets or sets the maximum number of events per second for this tenant.
    /// </summary>
    public int MaxEventsPerSecond { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of concurrent connections for this tenant.
    /// </summary>
    public int MaxConcurrentConnections { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of concurrent subscriptions for this tenant.
    /// </summary>
    public int MaxConcurrentSubscriptions { get; set; }

    /// <summary>
    /// Gets or sets the maximum memory usage in MB for this tenant.
    /// </summary>
    public long MaxMemoryUsageMb { get; set; }

    /// <summary>
    /// Gets or sets the maximum CPU usage percentage for this tenant.
    /// </summary>
    public double MaxCpuUsage { get; set; }

    /// <summary>
    /// Gets or sets the burst allowance multiplier for this tenant.
    /// </summary>
    public double BurstMultiplier { get; set; }

    /// <summary>
    /// Gets or sets the priority level for this tenant.
    /// </summary>
    public TenantPriority Priority { get; set; } = TenantPriority.Normal;
}

/// <summary>
/// Throttling algorithm configuration.
/// </summary>
public sealed class ThrottlingAlgorithmOptions
{
    /// <summary>
    /// Gets or sets the throttling algorithm type.
    /// </summary>
    public ThrottlingAlgorithmType AlgorithmType { get; set; } = ThrottlingAlgorithmType.TokenBucket;

    /// <summary>
    /// Gets or sets the window size in seconds for sliding window algorithms.
    /// </summary>
    public int WindowSizeSeconds { get; set; } = 60;

    /// <summary>
    /// Gets or sets the number of windows for sliding window algorithms.
    /// </summary>
    public int NumberOfWindows { get; set; } = 10;

    /// <summary>
    /// Gets or sets the bucket size for token bucket algorithms.
    /// </summary>
    public int BucketSize { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the refill rate for token bucket algorithms.
    /// </summary>
    public int RefillRate { get; set; } = 100;

    /// <summary>
    /// Gets or sets the refill interval in milliseconds for token bucket algorithms.
    /// </summary>
    public int RefillIntervalMs { get; set; } = 1000;
}

/// <summary>
/// Throttling monitoring configuration.
/// </summary>
public sealed class ThrottlingMonitoringOptions
{
    /// <summary>
    /// Gets or sets whether throttling monitoring is enabled.
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
    /// Gets or sets whether to generate alerts for throttling violations.
    /// </summary>
    public bool GenerateAlerts { get; set; } = true;

    /// <summary>
    /// Gets or sets the alert thresholds.
    /// </summary>
    public ThrottlingAlertThresholds AlertThresholds { get; set; } = new();
}

/// <summary>
/// Throttling alert thresholds.
/// </summary>
public sealed class ThrottlingAlertThresholds
{
    /// <summary>
    /// Gets or sets the threshold for throttling violations per minute.
    /// </summary>
    public int ViolationsPerMinute { get; set; } = 10;

    /// <summary>
    /// Gets or sets the threshold for consecutive throttling violations.
    /// </summary>
    public int ConsecutiveViolations { get; set; } = 5;

    /// <summary>
    /// Gets or sets the threshold for resource usage percentage.
    /// </summary>
    public double ResourceUsageThreshold { get; set; } = 0.8; // 80%
}

/// <summary>
/// Throttling algorithm types.
/// </summary>
public enum ThrottlingAlgorithmType
{
    /// <summary>
    /// Token bucket algorithm.
    /// </summary>
    TokenBucket,

    /// <summary>
    /// Sliding window algorithm.
    /// </summary>
    SlidingWindow,

    /// <summary>
    /// Fixed window algorithm.
    /// </summary>
    FixedWindow,

    /// <summary>
    /// Leaky bucket algorithm.
    /// </summary>
    LeakyBucket
}

/// <summary>
/// Tenant priority levels.
/// </summary>
public enum TenantPriority
{
    /// <summary>
    /// Low priority tenant.
    /// </summary>
    Low = 1,

    /// <summary>
    /// Normal priority tenant.
    /// </summary>
    Normal = 2,

    /// <summary>
    /// High priority tenant.
    /// </summary>
    High = 3,

    /// <summary>
    /// Critical priority tenant.
    /// </summary>
    Critical = 4
}