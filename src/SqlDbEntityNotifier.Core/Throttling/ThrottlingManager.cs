using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SqlDbEntityNotifier.Core.Throttling.Models;
using SqlDbEntityNotifier.Core.MultiTenant.Models;

namespace SqlDbEntityNotifier.Core.Throttling;

/// <summary>
/// Manages throttling and rate limiting for multi-tenant operations.
/// </summary>
public class ThrottlingManager
{
    private readonly ILogger<ThrottlingManager> _logger;
    private readonly ThrottlingOptions _options;
    private readonly Dictionary<string, TenantThrottler> _tenantThrottlers;
    private readonly GlobalThrottler _globalThrottler;
    private readonly SemaphoreSlim _throttlingSemaphore;

    /// <summary>
    /// Initializes a new instance of the ThrottlingManager class.
    /// </summary>
    public ThrottlingManager(
        ILogger<ThrottlingManager> logger,
        IOptions<ThrottlingOptions> options)
    {
        _logger = logger;
        _options = options.Value;
        _tenantThrottlers = new Dictionary<string, TenantThrottler>();
        _globalThrottler = new GlobalThrottler(_options.Global, _logger);
        _throttlingSemaphore = new SemaphoreSlim(1, 1);
    }

    /// <summary>
    /// Checks if a request is allowed for a specific tenant.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="requestType">The type of request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A throttling result indicating if the request is allowed.</returns>
    public async Task<ThrottlingResult> CheckThrottlingAsync(string tenantId, ThrottlingRequestType requestType, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check global throttling first
            var globalResult = await _globalThrottler.CheckThrottlingAsync(requestType, cancellationToken);
            if (!globalResult.IsAllowed)
            {
                _logger.LogWarning("Request throttled by global limits: {RequestType}", requestType);
                return globalResult;
            }

            // Check tenant-specific throttling
            var tenantThrottler = await GetOrCreateTenantThrottlerAsync(tenantId, cancellationToken);
            var tenantResult = await tenantThrottler.CheckThrottlingAsync(requestType, cancellationToken);
            
            if (!tenantResult.IsAllowed)
            {
                _logger.LogWarning("Request throttled by tenant limits: {TenantId}, {RequestType}", tenantId, requestType);
            }

            return tenantResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking throttling for tenant: {TenantId}, request type: {RequestType}", tenantId, requestType);
            
            // In case of error, allow the request but log the issue
            return new ThrottlingResult
            {
                IsAllowed = true,
                Reason = "Throttling check failed, allowing request",
                RetryAfterSeconds = 0,
                RemainingRequests = 0,
                ResetTime = DateTime.UtcNow.AddSeconds(60)
            };
        }
    }

    /// <summary>
    /// Records a successful request for throttling tracking.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="requestType">The type of request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the recording operation.</returns>
    public async Task RecordRequestAsync(string tenantId, ThrottlingRequestType requestType, CancellationToken cancellationToken = default)
    {
        try
        {
            // Record global request
            await _globalThrottler.RecordRequestAsync(requestType, cancellationToken);

            // Record tenant request
            var tenantThrottler = await GetOrCreateTenantThrottlerAsync(tenantId, cancellationToken);
            await tenantThrottler.RecordRequestAsync(requestType, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording request for tenant: {TenantId}, request type: {RequestType}", tenantId, requestType);
        }
    }

    /// <summary>
    /// Updates the throttling configuration for a specific tenant.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="config">The new throttling configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the update operation.</returns>
    public async Task UpdateTenantThrottlingAsync(string tenantId, TenantThrottlingConfig config, CancellationToken cancellationToken = default)
    {
        try
        {
            await _throttlingSemaphore.WaitAsync(cancellationToken);

            if (_tenantThrottlers.TryGetValue(tenantId, out var throttler))
            {
                await throttler.UpdateConfigurationAsync(config, cancellationToken);
            }
            else
            {
                var newThrottler = new TenantThrottler(tenantId, config, _options.Algorithm, _logger);
                _tenantThrottlers[tenantId] = newThrottler;
            }

            _logger.LogInformation("Updated throttling configuration for tenant: {TenantId}", tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating throttling configuration for tenant: {TenantId}", tenantId);
            throw;
        }
        finally
        {
            _throttlingSemaphore.Release();
        }
    }

    /// <summary>
    /// Removes throttling configuration for a specific tenant.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the removal operation.</returns>
    public async Task RemoveTenantThrottlingAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _throttlingSemaphore.WaitAsync(cancellationToken);

            if (_tenantThrottlers.Remove(tenantId, out var throttler))
            {
                await throttler.DisposeAsync();
                _logger.LogInformation("Removed throttling configuration for tenant: {TenantId}", tenantId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing throttling configuration for tenant: {TenantId}", tenantId);
            throw;
        }
        finally
        {
            _throttlingSemaphore.Release();
        }
    }

    /// <summary>
    /// Gets the current throttling status for a specific tenant.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The current throttling status.</returns>
    public async Task<ThrottlingStatus> GetTenantThrottlingStatusAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_tenantThrottlers.TryGetValue(tenantId, out var throttler))
            {
                return await throttler.GetStatusAsync(cancellationToken);
            }

            // Return default status if tenant throttler doesn't exist
            return new ThrottlingStatus
            {
                TenantId = tenantId,
                IsThrottled = false,
                CurrentRequestsPerSecond = 0,
                MaxRequestsPerSecond = _options.PerTenant.DefaultMaxEventsPerSecond,
                RemainingRequests = _options.PerTenant.DefaultMaxEventsPerSecond,
                ResetTime = DateTime.UtcNow.AddSeconds(60),
                LastUpdated = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting throttling status for tenant: {TenantId}", tenantId);
            throw;
        }
    }

    /// <summary>
    /// Gets the global throttling status.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The global throttling status.</returns>
    public async Task<GlobalThrottlingStatus> GetGlobalThrottlingStatusAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _globalThrottler.GetStatusAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting global throttling status");
            throw;
        }
    }

    /// <summary>
    /// Gets statistics for all tenants.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Throttling statistics for all tenants.</returns>
    public async Task<ThrottlingStatistics> GetThrottlingStatisticsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var statistics = new ThrottlingStatistics
            {
                TotalTenants = _tenantThrottlers.Count,
                ActiveTenants = 0,
                ThrottledTenants = 0,
                TotalRequestsPerSecond = 0,
                TotalThrottledRequests = 0,
                LastUpdated = DateTime.UtcNow
            };

            foreach (var kvp in _tenantThrottlers)
            {
                var status = await kvp.Value.GetStatusAsync(cancellationToken);
                
                if (status.CurrentRequestsPerSecond > 0)
                {
                    statistics.ActiveTenants++;
                }

                if (status.IsThrottled)
                {
                    statistics.ThrottledTenants++;
                }

                statistics.TotalRequestsPerSecond += status.CurrentRequestsPerSecond;
                statistics.TotalThrottledRequests += status.ThrottledRequests;
            }

            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting throttling statistics");
            throw;
        }
    }

    private async Task<TenantThrottler> GetOrCreateTenantThrottlerAsync(string tenantId, CancellationToken cancellationToken)
    {
        if (_tenantThrottlers.TryGetValue(tenantId, out var throttler))
        {
            return throttler;
        }

        await _throttlingSemaphore.WaitAsync(cancellationToken);
        try
        {
            // Double-check after acquiring lock
            if (_tenantThrottlers.TryGetValue(tenantId, out throttler))
            {
                return throttler;
            }

            // Get tenant-specific configuration or use defaults
            var config = GetTenantThrottlingConfig(tenantId);
            throttler = new TenantThrottler(tenantId, config, _options.Algorithm, _logger);
            _tenantThrottlers[tenantId] = throttler;

            return throttler;
        }
        finally
        {
            _throttlingSemaphore.Release();
        }
    }

    private TenantThrottlingConfig GetTenantThrottlingConfig(string tenantId)
    {
        if (_options.PerTenant.TenantConfigs.TryGetValue(tenantId, out var config))
        {
            return config;
        }

        // Return default configuration
        return new TenantThrottlingConfig
        {
            MaxEventsPerSecond = _options.PerTenant.DefaultMaxEventsPerSecond,
            MaxConcurrentConnections = _options.PerTenant.DefaultMaxConcurrentConnections,
            MaxConcurrentSubscriptions = _options.PerTenant.DefaultMaxConcurrentSubscriptions,
            MaxMemoryUsageMb = _options.PerTenant.DefaultMaxMemoryUsageMb,
            MaxCpuUsage = _options.PerTenant.DefaultMaxCpuUsage,
            BurstMultiplier = _options.PerTenant.DefaultBurstMultiplier,
            Priority = TenantPriority.Normal
        };
    }

    /// <summary>
    /// Disposes the throttling manager.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await _throttlingSemaphore.WaitAsync();
        try
        {
            foreach (var throttler in _tenantThrottlers.Values)
            {
                await throttler.DisposeAsync();
            }
            _tenantThrottlers.Clear();
        }
        finally
        {
            _throttlingSemaphore.Release();
            _throttlingSemaphore.Dispose();
        }
    }
}

/// <summary>
/// Types of throttling requests.
/// </summary>
public enum ThrottlingRequestType
{
    /// <summary>
    /// Event processing request.
    /// </summary>
    EventProcessing,

    /// <summary>
    /// Subscription creation request.
    /// </summary>
    SubscriptionCreation,

    /// <summary>
    /// Connection establishment request.
    /// </summary>
    ConnectionEstablishment,

    /// <summary>
    /// Bulk operation request.
    /// </summary>
    BulkOperation,

    /// <summary>
    /// Schema change request.
    /// </summary>
    SchemaChange,

    /// <summary>
    /// Replay request.
    /// </summary>
    Replay
}

/// <summary>
/// Result of a throttling check.
/// </summary>
public sealed class ThrottlingResult
{
    /// <summary>
    /// Gets or sets whether the request is allowed.
    /// </summary>
    public bool IsAllowed { get; set; }

    /// <summary>
    /// Gets or sets the reason for throttling (if not allowed).
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of seconds to wait before retrying.
    /// </summary>
    public int RetryAfterSeconds { get; set; }

    /// <summary>
    /// Gets or sets the number of remaining requests in the current window.
    /// </summary>
    public int RemainingRequests { get; set; }

    /// <summary>
    /// Gets or sets the time when the throttling window resets.
    /// </summary>
    public DateTime ResetTime { get; set; }

    /// <summary>
    /// Gets or sets the current requests per second.
    /// </summary>
    public int CurrentRequestsPerSecond { get; set; }

    /// <summary>
    /// Gets or sets the maximum requests per second allowed.
    /// </summary>
    public int MaxRequestsPerSecond { get; set; }
}

/// <summary>
/// Throttling status for a tenant.
/// </summary>
public sealed class ThrottlingStatus
{
    /// <summary>
    /// Gets or sets the tenant ID.
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the tenant is currently throttled.
    /// </summary>
    public bool IsThrottled { get; set; }

    /// <summary>
    /// Gets or sets the current requests per second.
    /// </summary>
    public int CurrentRequestsPerSecond { get; set; }

    /// <summary>
    /// Gets or sets the maximum requests per second allowed.
    /// </summary>
    public int MaxRequestsPerSecond { get; set; }

    /// <summary>
    /// Gets or sets the number of remaining requests in the current window.
    /// </summary>
    public int RemainingRequests { get; set; }

    /// <summary>
    /// Gets or sets the time when the throttling window resets.
    /// </summary>
    public DateTime ResetTime { get; set; }

    /// <summary>
    /// Gets or sets the number of throttled requests.
    /// </summary>
    public long ThrottledRequests { get; set; }

    /// <summary>
    /// Gets or sets the last updated timestamp.
    /// </summary>
    public DateTime LastUpdated { get; set; }
}

/// <summary>
/// Global throttling status.
/// </summary>
public sealed class GlobalThrottlingStatus
{
    /// <summary>
    /// Gets or sets whether global throttling is active.
    /// </summary>
    public bool IsThrottled { get; set; }

    /// <summary>
    /// Gets or sets the current global requests per second.
    /// </summary>
    public int CurrentRequestsPerSecond { get; set; }

    /// <summary>
    /// Gets or sets the maximum global requests per second allowed.
    /// </summary>
    public int MaxRequestsPerSecond { get; set; }

    /// <summary>
    /// Gets or sets the current memory usage in MB.
    /// </summary>
    public long CurrentMemoryUsageMb { get; set; }

    /// <summary>
    /// Gets or sets the maximum memory usage in MB allowed.
    /// </summary>
    public long MaxMemoryUsageMb { get; set; }

    /// <summary>
    /// Gets or sets the current CPU usage percentage.
    /// </summary>
    public double CurrentCpuUsage { get; set; }

    /// <summary>
    /// Gets or sets the maximum CPU usage percentage allowed.
    /// </summary>
    public double MaxCpuUsage { get; set; }

    /// <summary>
    /// Gets or sets the last updated timestamp.
    /// </summary>
    public DateTime LastUpdated { get; set; }
}

/// <summary>
/// Throttling statistics for all tenants.
/// </summary>
public sealed class ThrottlingStatistics
{
    /// <summary>
    /// Gets or sets the total number of tenants.
    /// </summary>
    public int TotalTenants { get; set; }

    /// <summary>
    /// Gets or sets the number of active tenants.
    /// </summary>
    public int ActiveTenants { get; set; }

    /// <summary>
    /// Gets or sets the number of throttled tenants.
    /// </summary>
    public int ThrottledTenants { get; set; }

    /// <summary>
    /// Gets or sets the total requests per second across all tenants.
    /// </summary>
    public int TotalRequestsPerSecond { get; set; }

    /// <summary>
    /// Gets or sets the total number of throttled requests.
    /// </summary>
    public long TotalThrottledRequests { get; set; }

    /// <summary>
    /// Gets or sets the last updated timestamp.
    /// </summary>
    public DateTime LastUpdated { get; set; }
}