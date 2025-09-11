using SqlDbEntityNotifier.Core.Transactional.Models;

namespace SqlDbEntityNotifier.Core.Transactional;

/// <summary>
/// Interface for storing and retrieving transactional groups.
/// </summary>
public interface ITransactionalGroupStore
{
    /// <summary>
    /// Stores a transactional group.
    /// </summary>
    /// <param name="transaction">The transactional group to store.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the storage operation.</returns>
    Task StoreTransactionAsync(TransactionalGroup transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a transactional group by ID.
    /// </summary>
    /// <param name="transactionId">The transaction ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The transactional group or null if not found.</returns>
    Task<TransactionalGroup?> GetTransactionAsync(string transactionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves transactional groups by status.
    /// </summary>
    /// <param name="status">The transaction status.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of transactional groups with the specified status.</returns>
    Task<IList<TransactionalGroup>> GetTransactionsByStatusAsync(TransactionStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves transactional groups by source.
    /// </summary>
    /// <param name="source">The source database identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of transactional groups for the specified source.</returns>
    Task<IList<TransactionalGroup>> GetTransactionsBySourceAsync(string source, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves transactional groups by tenant ID.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of transactional groups for the specified tenant.</returns>
    Task<IList<TransactionalGroup>> GetTransactionsByTenantAsync(string tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all transactional groups.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of all transactional groups.</returns>
    Task<IList<TransactionalGroup>> GetAllTransactionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a transactional group by ID.
    /// </summary>
    /// <param name="transactionId">The transaction ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the deletion operation.</returns>
    Task DeleteTransactionAsync(string transactionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a transactional group exists.
    /// </summary>
    /// <param name="transactionId">The transaction ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the transactional group exists, false otherwise.</returns>
    Task<bool> TransactionExistsAsync(string transactionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a transactional group.
    /// </summary>
    /// <param name="transaction">The transactional group to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the update operation.</returns>
    Task UpdateTransactionAsync(TransactionalGroup transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of transactions by status.
    /// </summary>
    /// <param name="status">The transaction status.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The count of transactions with the specified status.</returns>
    Task<long> GetTransactionCountByStatusAsync(TransactionStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of all transactions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The total count of transactions.</returns>
    Task<long> GetTotalTransactionCountAsync(CancellationToken cancellationToken = default);
}