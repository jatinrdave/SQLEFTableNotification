using SqlDbEntityNotifier.Core.MultiTenant.Models;

namespace SqlDbEntityNotifier.Core.MultiTenant;

/// <summary>
/// Interface for storing and retrieving tenant information.
/// </summary>
public interface ITenantStore
{
    /// <summary>
    /// Stores a tenant context.
    /// </summary>
    /// <param name="tenantContext">The tenant context to store.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the storage operation.</returns>
    Task StoreTenantAsync(TenantContext tenantContext, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a tenant context by ID.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The tenant context or null if not found.</returns>
    Task<TenantContext?> GetTenantAsync(string tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all tenant contexts.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of all tenant contexts.</returns>
    Task<IList<TenantContext>> GetAllTenantsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a tenant context by ID.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the removal operation.</returns>
    Task RemoveTenantAsync(string tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a tenant exists.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the tenant exists, false otherwise.</returns>
    Task<bool> TenantExistsAsync(string tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a tenant context.
    /// </summary>
    /// <param name="tenantContext">The tenant context to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the update operation.</returns>
    Task UpdateTenantAsync(TenantContext tenantContext, CancellationToken cancellationToken = default);
}