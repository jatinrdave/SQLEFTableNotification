namespace SqlDbEntityNotifier.Core.MultiTenant.Models;

/// <summary>
/// Configuration options for the tenant manager.
/// </summary>
public sealed class TenantManagerOptions
{
    /// <summary>
    /// Gets or sets whether multi-tenant support is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of concurrent tenants.
    /// </summary>
    public int MaxConcurrentTenants { get; set; } = 100;

    /// <summary>
    /// Gets or sets the default tenant isolation level.
    /// </summary>
    public TenantIsolationLevel DefaultIsolationLevel { get; set; } = TenantIsolationLevel.Database;

    /// <summary>
    /// Gets or sets the tenant cache configuration.
    /// </summary>
    public TenantCacheOptions Cache { get; set; } = new();

    /// <summary>
    /// Gets or sets the tenant validation configuration.
    /// </summary>
    public TenantValidationOptions Validation { get; set; } = new();

    /// <summary>
    /// Gets or sets the tenant monitoring configuration.
    /// </summary>
    public TenantMonitoringOptions Monitoring { get; set; } = new();
}

/// <summary>
/// Tenant cache configuration.
/// </summary>
public sealed class TenantCacheOptions
{
    /// <summary>
    /// Gets or sets whether tenant caching is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the cache TTL in seconds.
    /// </summary>
    public int TtlSeconds { get; set; } = 3600; // 1 hour

    /// <summary>
    /// Gets or sets the maximum number of tenants to cache.
    /// </summary>
    public int MaxTenants { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the cache eviction policy.
    /// </summary>
    public CacheEvictionPolicy EvictionPolicy { get; set; } = CacheEvictionPolicy.Lru;
}

/// <summary>
/// Tenant validation configuration.
/// </summary>
public sealed class TenantValidationOptions
{
    /// <summary>
    /// Gets or sets whether tenant validation is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to validate tenant resource limits.
    /// </summary>
    public bool ValidateResourceLimits { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to validate tenant configuration.
    /// </summary>
    public bool ValidateConfiguration { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to validate tenant permissions.
    /// </summary>
    public bool ValidatePermissions { get; set; } = true;
}

/// <summary>
/// Tenant monitoring configuration.
/// </summary>
public sealed class TenantMonitoringOptions
{
    /// <summary>
    /// Gets or sets whether tenant monitoring is enabled.
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
    /// Gets or sets whether to generate alerts for tenant issues.
    /// </summary>
    public bool GenerateAlerts { get; set; } = true;
}

/// <summary>
/// Cache eviction policies.
/// </summary>
public enum CacheEvictionPolicy
{
    /// <summary>
    /// Least Recently Used eviction policy.
    /// </summary>
    Lru,

    /// <summary>
    /// Least Frequently Used eviction policy.
    /// </summary>
    Lfu,

    /// <summary>
    /// First In First Out eviction policy.
    /// </summary>
    Fifo,

    /// <summary>
    /// Time-based eviction policy.
    /// </summary>
    TimeBased
}