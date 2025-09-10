using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SqlDbEntityNotifier.Core.Interfaces;
using SqlDbEntityNotifier.Core.Models;
using SqlDbEntityNotifier.Core.MultiTenant.Models;

namespace SqlDbEntityNotifier.Core.MultiTenant;

/// <summary>
/// Manages multi-tenant operations and tenant isolation.
/// </summary>
public class TenantManager
{
    private readonly ILogger<TenantManager> _logger;
    private readonly TenantManagerOptions _options;
    private readonly ITenantStore _tenantStore;
    private readonly Dictionary<string, TenantContext> _activeTenants;
    private readonly SemaphoreSlim _tenantSemaphore;

    /// <summary>
    /// Initializes a new instance of the TenantManager class.
    /// </summary>
    public TenantManager(
        ILogger<TenantManager> logger,
        IOptions<TenantManagerOptions> options,
        ITenantStore tenantStore)
    {
        _logger = logger;
        _options = options.Value;
        _tenantStore = tenantStore;
        _activeTenants = new Dictionary<string, TenantContext>();
        _tenantSemaphore = new SemaphoreSlim(_options.MaxConcurrentTenants, _options.MaxConcurrentTenants);
    }

    /// <summary>
    /// Gets the current tenant context.
    /// </summary>
    public TenantContext? CurrentTenant { get; private set; }

    /// <summary>
    /// Gets all active tenants.
    /// </summary>
    public IReadOnlyDictionary<string, TenantContext> ActiveTenants => _activeTenants.AsReadOnly();

    /// <summary>
    /// Registers a new tenant.
    /// </summary>
    /// <param name="tenantContext">The tenant context to register.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the registration operation.</returns>
    public async Task RegisterTenantAsync(TenantContext tenantContext, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Registering tenant: {TenantId}", tenantContext.TenantId);

            // Validate tenant context
            ValidateTenantContext(tenantContext);

            // Store tenant in persistent store
            await _tenantStore.StoreTenantAsync(tenantContext, cancellationToken);

            // Add to active tenants if not at limit
            if (_activeTenants.Count < _options.MaxConcurrentTenants)
            {
                _activeTenants[tenantContext.TenantId] = tenantContext;
                _logger.LogInformation("Tenant registered and activated: {TenantId}", tenantContext.TenantId);
            }
            else
            {
                _logger.LogWarning("Tenant registered but not activated due to limit: {TenantId}", tenantContext.TenantId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering tenant: {TenantId}", tenantContext.TenantId);
            throw;
        }
    }

    /// <summary>
    /// Unregisters a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant ID to unregister.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the unregistration operation.</returns>
    public async Task UnregisterTenantAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Unregistering tenant: {TenantId}", tenantId);

            // Remove from active tenants
            if (_activeTenants.Remove(tenantId))
            {
                _logger.LogInformation("Tenant deactivated: {TenantId}", tenantId);
            }

            // Remove from persistent store
            await _tenantStore.RemoveTenantAsync(tenantId, cancellationToken);

            _logger.LogInformation("Tenant unregistered: {TenantId}", tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unregistering tenant: {TenantId}", tenantId);
            throw;
        }
    }

    /// <summary>
    /// Activates a tenant for processing.
    /// </summary>
    /// <param name="tenantId">The tenant ID to activate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the activation operation.</returns>
    public async Task ActivateTenantAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _tenantSemaphore.WaitAsync(cancellationToken);

            _logger.LogInformation("Activating tenant: {TenantId}", tenantId);

            // Load tenant from store if not in active tenants
            if (!_activeTenants.TryGetValue(tenantId, out var tenantContext))
            {
                tenantContext = await _tenantStore.GetTenantAsync(tenantId, cancellationToken);
                if (tenantContext == null)
                {
                    throw new InvalidOperationException($"Tenant not found: {tenantId}");
                }
            }

            // Check if tenant is active
            if (!tenantContext.IsActive)
            {
                throw new InvalidOperationException($"Tenant is not active: {tenantId}");
            }

            // Check resource limits
            await ValidateTenantResourceLimitsAsync(tenantContext, cancellationToken);

            // Add to active tenants
            _activeTenants[tenantId] = tenantContext;

            _logger.LogInformation("Tenant activated: {TenantId}", tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating tenant: {TenantId}", tenantId);
            throw;
        }
        finally
        {
            _tenantSemaphore.Release();
        }
    }

    /// <summary>
    /// Deactivates a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant ID to deactivate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the deactivation operation.</returns>
    public async Task DeactivateTenantAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Deactivating tenant: {TenantId}", tenantId);

            if (_activeTenants.Remove(tenantId))
            {
                _logger.LogInformation("Tenant deactivated: {TenantId}", tenantId);
            }
            else
            {
                _logger.LogWarning("Tenant was not active: {TenantId}", tenantId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating tenant: {TenantId}", tenantId);
            throw;
        }
    }

    /// <summary>
    /// Sets the current tenant context for the current operation.
    /// </summary>
    /// <param name="tenantId">The tenant ID to set as current.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the operation.</returns>
    public async Task SetCurrentTenantAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_activeTenants.TryGetValue(tenantId, out var tenantContext))
            {
                tenantContext = await _tenantStore.GetTenantAsync(tenantId, cancellationToken);
                if (tenantContext == null)
                {
                    throw new InvalidOperationException($"Tenant not found: {tenantId}");
                }
            }

            CurrentTenant = tenantContext;
            _logger.LogDebug("Current tenant set to: {TenantId}", tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting current tenant: {TenantId}", tenantId);
            throw;
        }
    }

    /// <summary>
    /// Clears the current tenant context.
    /// </summary>
    public void ClearCurrentTenant()
    {
        CurrentTenant = null;
        _logger.LogDebug("Current tenant cleared");
    }

    /// <summary>
    /// Gets a tenant context by ID.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The tenant context or null if not found.</returns>
    public async Task<TenantContext?> GetTenantAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_activeTenants.TryGetValue(tenantId, out var tenantContext))
            {
                return tenantContext;
            }

            return await _tenantStore.GetTenantAsync(tenantId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tenant: {TenantId}", tenantId);
            throw;
        }
    }

    /// <summary>
    /// Gets all registered tenants.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of all registered tenants.</returns>
    public async Task<IList<TenantContext>> GetAllTenantsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _tenantStore.GetAllTenantsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all tenants");
            throw;
        }
    }

    /// <summary>
    /// Updates a tenant configuration.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="configuration">The new configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the update operation.</returns>
    public async Task UpdateTenantConfigurationAsync(string tenantId, TenantConfiguration configuration, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Updating tenant configuration: {TenantId}", tenantId);

            var tenantContext = await GetTenantAsync(tenantId, cancellationToken);
            if (tenantContext == null)
            {
                throw new InvalidOperationException($"Tenant not found: {tenantId}");
            }

            tenantContext.Configuration = configuration;
            tenantContext.UpdatedAt = DateTime.UtcNow;

            // Update in store
            await _tenantStore.StoreTenantAsync(tenantContext, cancellationToken);

            // Update in active tenants if present
            if (_activeTenants.ContainsKey(tenantId))
            {
                _activeTenants[tenantId] = tenantContext;
            }

            _logger.LogInformation("Tenant configuration updated: {TenantId}", tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating tenant configuration: {TenantId}", tenantId);
            throw;
        }
    }

    /// <summary>
    /// Validates tenant resource limits.
    /// </summary>
    /// <param name="tenantContext">The tenant context to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the validation operation.</returns>
    public async Task ValidateTenantResourceLimitsAsync(TenantContext tenantContext, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if tenant is within resource limits
            var currentUsage = await GetTenantResourceUsageAsync(tenantContext.TenantId, cancellationToken);
            var limits = tenantContext.ResourceLimits;

            if (currentUsage.Connections > limits.MaxConnections)
            {
                throw new InvalidOperationException($"Tenant {tenantContext.TenantId} exceeds maximum connections: {currentUsage.Connections}/{limits.MaxConnections}");
            }

            if (currentUsage.EventsPerSecond > limits.MaxEventsPerSecond)
            {
                throw new InvalidOperationException($"Tenant {tenantContext.TenantId} exceeds maximum events per second: {currentUsage.EventsPerSecond}/{limits.MaxEventsPerSecond}");
            }

            if (currentUsage.Subscriptions > limits.MaxSubscriptions)
            {
                throw new InvalidOperationException($"Tenant {tenantContext.TenantId} exceeds maximum subscriptions: {currentUsage.Subscriptions}/{limits.MaxSubscriptions}");
            }

            if (currentUsage.MemoryUsageMb > limits.MaxMemoryUsageMb)
            {
                throw new InvalidOperationException($"Tenant {tenantContext.TenantId} exceeds maximum memory usage: {currentUsage.MemoryUsageMb}MB/{limits.MaxMemoryUsageMb}MB");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating tenant resource limits: {TenantId}", tenantContext.TenantId);
            throw;
        }
    }

    /// <summary>
    /// Gets the current resource usage for a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The current resource usage.</returns>
    public async Task<TenantResourceUsage> GetTenantResourceUsageAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            // This is a simplified implementation
            // In a real implementation, you would query actual resource usage
            return new TenantResourceUsage
            {
                TenantId = tenantId,
                Connections = 0,
                EventsPerSecond = 0,
                Subscriptions = 0,
                MemoryUsageMb = 0,
                StorageUsageMb = 0,
                CpuUsage = 0.0,
                Timestamp = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tenant resource usage: {TenantId}", tenantId);
            throw;
        }
    }

    private void ValidateTenantContext(TenantContext tenantContext)
    {
        if (string.IsNullOrEmpty(tenantContext.TenantId))
        {
            throw new ArgumentException("Tenant ID cannot be null or empty", nameof(tenantContext));
        }

        if (string.IsNullOrEmpty(tenantContext.TenantName))
        {
            throw new ArgumentException("Tenant name cannot be null or empty", nameof(tenantContext));
        }

        if (tenantContext.Configuration == null)
        {
            throw new ArgumentException("Tenant configuration cannot be null", nameof(tenantContext));
        }
    }

    /// <summary>
    /// Disposes the tenant manager.
    /// </summary>
    public void Dispose()
    {
        _tenantSemaphore?.Dispose();
    }
}

/// <summary>
/// Tenant resource usage information.
/// </summary>
public sealed class TenantResourceUsage
{
    /// <summary>
    /// Gets or sets the tenant ID.
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of active connections.
    /// </summary>
    public int Connections { get; set; }

    /// <summary>
    /// Gets or sets the current events per second.
    /// </summary>
    public int EventsPerSecond { get; set; }

    /// <summary>
    /// Gets or sets the number of active subscriptions.
    /// </summary>
    public int Subscriptions { get; set; }

    /// <summary>
    /// Gets or sets the current memory usage in MB.
    /// </summary>
    public long MemoryUsageMb { get; set; }

    /// <summary>
    /// Gets or sets the current storage usage in MB.
    /// </summary>
    public long StorageUsageMb { get; set; }

    /// <summary>
    /// Gets or sets the current CPU usage percentage.
    /// </summary>
    public double CpuUsage { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the usage was measured.
    /// </summary>
    public DateTime Timestamp { get; set; }
}