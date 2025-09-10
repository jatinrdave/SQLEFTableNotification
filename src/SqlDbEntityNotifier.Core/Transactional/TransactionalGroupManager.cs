using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SqlDbEntityNotifier.Core.Interfaces;
using SqlDbEntityNotifier.Core.Models;
using SqlDbEntityNotifier.Core.Transactional.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SqlDbEntityNotifier.Core.Transactional;

/// <summary>
/// Manages transactional groups for exactly-once semantics.
/// </summary>
public class TransactionalGroupManager
{
    private readonly ILogger<TransactionalGroupManager> _logger;
    private readonly TransactionalGroupOptions _options;
    private readonly ITransactionalGroupStore _groupStore;
    private readonly Dictionary<string, TransactionalGroup> _activeTransactions;
    private readonly SemaphoreSlim _transactionSemaphore;
    private long _sequenceNumber = 0;

    /// <summary>
    /// Initializes a new instance of the TransactionalGroupManager class.
    /// </summary>
    public TransactionalGroupManager(
        ILogger<TransactionalGroupManager> logger,
        IOptions<TransactionalGroupOptions> options,
        ITransactionalGroupStore groupStore)
    {
        _logger = logger;
        _options = options.Value;
        _groupStore = groupStore;
        _activeTransactions = new Dictionary<string, TransactionalGroup>();
        _transactionSemaphore = new SemaphoreSlim(_options.MaxConcurrentTransactions, _options.MaxConcurrentTransactions);
    }

    /// <summary>
    /// Starts a new transactional group.
    /// </summary>
    /// <param name="transactionId">The transaction ID.</param>
    /// <param name="source">The source database identifier.</param>
    /// <param name="tenantId">The tenant ID (if multi-tenant).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created transactional group.</returns>
    public async Task<TransactionalGroup> StartTransactionAsync(
        string transactionId,
        string source,
        string? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _transactionSemaphore.WaitAsync(cancellationToken);

            _logger.LogInformation("Starting transaction: {TransactionId} for source: {Source}", transactionId, source);

            var transaction = new TransactionalGroup
            {
                TransactionId = transactionId,
                Source = source,
                TenantId = tenantId,
                StartTimestamp = DateTime.UtcNow,
                Status = TransactionStatus.Active,
                SequenceNumber = Interlocked.Increment(ref _sequenceNumber),
                TimeoutSeconds = _options.DefaultTimeoutSeconds,
                RequiresExactlyOnce = _options.RequireExactlyOnce
            };

            _activeTransactions[transactionId] = transaction;
            await _groupStore.StoreTransactionAsync(transaction, cancellationToken);

            _logger.LogInformation("Transaction started: {TransactionId}", transactionId);
            return transaction;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting transaction: {TransactionId}", transactionId);
            throw;
        }
        finally
        {
            _transactionSemaphore.Release();
        }
    }

    /// <summary>
    /// Adds a change event to a transactional group.
    /// </summary>
    /// <param name="transactionId">The transaction ID.</param>
    /// <param name="changeEvent">The change event to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the operation.</returns>
    public async Task AddChangeEventAsync(
        string transactionId,
        ChangeEvent changeEvent,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_activeTransactions.TryGetValue(transactionId, out var transaction))
            {
                throw new InvalidOperationException($"Transaction not found: {transactionId}");
            }

            if (transaction.Status != TransactionStatus.Active)
            {
                throw new InvalidOperationException($"Transaction is not active: {transactionId}, status: {transaction.Status}");
            }

            // Add change event to transaction
            transaction.ChangeEvents.Add(changeEvent);

            // Update transaction metadata
            transaction.Metadata["event_count"] = transaction.ChangeEvents.Count.ToString();
            transaction.Metadata["last_event_timestamp"] = changeEvent.TimestampUtc.ToString("O");

            // Update checksum
            transaction.Checksum = CalculateTransactionChecksum(transaction);

            // Store updated transaction
            await _groupStore.StoreTransactionAsync(transaction, cancellationToken);

            _logger.LogDebug("Added change event to transaction: {TransactionId}, event count: {EventCount}", 
                transactionId, transaction.ChangeEvents.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding change event to transaction: {TransactionId}", transactionId);
            throw;
        }
    }

    /// <summary>
    /// Commits a transactional group.
    /// </summary>
    /// <param name="transactionId">The transaction ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the operation.</returns>
    public async Task CommitTransactionAsync(string transactionId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_activeTransactions.TryGetValue(transactionId, out var transaction))
            {
                throw new InvalidOperationException($"Transaction not found: {transactionId}");
            }

            _logger.LogInformation("Committing transaction: {TransactionId} with {EventCount} events", 
                transactionId, transaction.ChangeEvents.Count);

            // Validate transaction
            await ValidateTransactionAsync(transaction, cancellationToken);

            // Update transaction status
            transaction.Status = TransactionStatus.Preparing;
            transaction.EndTimestamp = DateTime.UtcNow;

            // Store updated transaction
            await _groupStore.StoreTransactionAsync(transaction, cancellationToken);

            // Remove from active transactions
            _activeTransactions.Remove(transactionId);

            _logger.LogInformation("Transaction committed: {TransactionId}", transactionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error committing transaction: {TransactionId}", transactionId);
            
            // Mark transaction as failed
            if (_activeTransactions.TryGetValue(transactionId, out var transaction))
            {
                transaction.Status = TransactionStatus.Failed;
                transaction.LastError = ex.Message;
                await _groupStore.StoreTransactionAsync(transaction, cancellationToken);
                _activeTransactions.Remove(transactionId);
            }
            
            throw;
        }
    }

    /// <summary>
    /// Rolls back a transactional group.
    /// </summary>
    /// <param name="transactionId">The transaction ID.</param>
    /// <param name="reason">The rollback reason.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the operation.</returns>
    public async Task RollbackTransactionAsync(
        string transactionId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_activeTransactions.TryGetValue(transactionId, out var transaction))
            {
                throw new InvalidOperationException($"Transaction not found: {transactionId}");
            }

            _logger.LogWarning("Rolling back transaction: {TransactionId}, reason: {Reason}", transactionId, reason);

            // Update transaction status
            transaction.Status = TransactionStatus.RolledBack;
            transaction.EndTimestamp = DateTime.UtcNow;
            transaction.LastError = reason;

            // Store updated transaction
            await _groupStore.StoreTransactionAsync(transaction, cancellationToken);

            // Remove from active transactions
            _activeTransactions.Remove(transactionId);

            _logger.LogInformation("Transaction rolled back: {TransactionId}", transactionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rolling back transaction: {TransactionId}", transactionId);
            throw;
        }
    }

    /// <summary>
    /// Gets a transactional group by ID.
    /// </summary>
    /// <param name="transactionId">The transaction ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The transactional group or null if not found.</returns>
    public async Task<TransactionalGroup?> GetTransactionAsync(string transactionId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_activeTransactions.TryGetValue(transactionId, out var transaction))
            {
                return transaction;
            }

            return await _groupStore.GetTransactionAsync(transactionId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting transaction: {TransactionId}", transactionId);
            throw;
        }
    }

    /// <summary>
    /// Gets all active transactions.
    /// </summary>
    /// <returns>A list of active transactions.</returns>
    public IList<TransactionalGroup> GetActiveTransactions()
    {
        return _activeTransactions.Values.ToList();
    }

    /// <summary>
    /// Gets transactions by status.
    /// </summary>
    /// <param name="status">The transaction status.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of transactions with the specified status.</returns>
    public async Task<IList<TransactionalGroup>> GetTransactionsByStatusAsync(
        TransactionStatus status,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _groupStore.GetTransactionsByStatusAsync(status, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting transactions by status: {Status}", status);
            throw;
        }
    }

    /// <summary>
    /// Records a delivery attempt for a transaction.
    /// </summary>
    /// <param name="transactionId">The transaction ID.</param>
    /// <param name="attempt">The delivery attempt.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the operation.</returns>
    public async Task RecordDeliveryAttemptAsync(
        string transactionId,
        DeliveryAttempt attempt,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var transaction = await GetTransactionAsync(transactionId, cancellationToken);
            if (transaction == null)
            {
                throw new InvalidOperationException($"Transaction not found: {transactionId}");
            }

            transaction.DeliveryAttempts.Add(attempt);
            transaction.RetryCount = transaction.DeliveryAttempts.Count;

            // Update transaction status based on delivery attempt
            if (attempt.Status == DeliveryStatus.Successful)
            {
                transaction.Status = TransactionStatus.Committed;
            }
            else if (attempt.Status == DeliveryStatus.Failed)
            {
                transaction.Status = TransactionStatus.Failed;
                transaction.LastError = attempt.ErrorMessage;
            }

            await _groupStore.StoreTransactionAsync(transaction, cancellationToken);

            _logger.LogDebug("Recorded delivery attempt for transaction: {TransactionId}, attempt: {AttemptNumber}, status: {Status}", 
                transactionId, attempt.AttemptNumber, attempt.Status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording delivery attempt for transaction: {TransactionId}", transactionId);
            throw;
        }
    }

    /// <summary>
    /// Cleans up completed transactions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the operation.</returns>
    public async Task CleanupCompletedTransactionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var cutoffTime = DateTime.UtcNow.AddDays(-_options.RetentionDays);
            var completedStatuses = new[] { TransactionStatus.Committed, TransactionStatus.RolledBack, TransactionStatus.Failed };

            foreach (var status in completedStatuses)
            {
                var transactions = await _groupStore.GetTransactionsByStatusAsync(status, cancellationToken);
                var transactionsToDelete = transactions.Where(t => t.EndTimestamp < cutoffTime).ToList();

                foreach (var transaction in transactionsToDelete)
                {
                    await _groupStore.DeleteTransactionAsync(transaction.TransactionId, cancellationToken);
                    _logger.LogDebug("Cleaned up completed transaction: {TransactionId}", transaction.TransactionId);
                }
            }

            _logger.LogInformation("Completed transaction cleanup");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up completed transactions");
            throw;
        }
    }

    /// <summary>
    /// Processes timeout for active transactions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the operation.</returns>
    public async Task ProcessTimeoutsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTime.UtcNow;
            var timedOutTransactions = new List<string>();

            foreach (var kvp in _activeTransactions)
            {
                var transaction = kvp.Value;
                var timeoutTime = transaction.StartTimestamp.AddSeconds(transaction.TimeoutSeconds);

                if (now > timeoutTime)
                {
                    timedOutTransactions.Add(kvp.Key);
                }
            }

            foreach (var transactionId in timedOutTransactions)
            {
                await RollbackTransactionAsync(transactionId, "Transaction timeout", cancellationToken);
                _logger.LogWarning("Transaction timed out: {TransactionId}", transactionId);
            }

            if (timedOutTransactions.Count > 0)
            {
                _logger.LogInformation("Processed {Count} timed out transactions", timedOutTransactions.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing transaction timeouts");
            throw;
        }
    }

    private async Task ValidateTransactionAsync(TransactionalGroup transaction, CancellationToken cancellationToken)
    {
        // Validate transaction has events
        if (transaction.ChangeEvents.Count == 0)
        {
            throw new InvalidOperationException($"Transaction {transaction.TransactionId} has no events");
        }

        // Validate checksum
        var expectedChecksum = CalculateTransactionChecksum(transaction);
        if (transaction.Checksum != expectedChecksum)
        {
            throw new InvalidOperationException($"Transaction {transaction.TransactionId} checksum validation failed");
        }

        // Validate transaction size
        if (transaction.ChangeEvents.Count > _options.MaxEventsPerTransaction)
        {
            throw new InvalidOperationException($"Transaction {transaction.TransactionId} exceeds maximum events per transaction: {transaction.ChangeEvents.Count}/{_options.MaxEventsPerTransaction}");
        }

        // Additional validation can be added here
        await Task.CompletedTask;
    }

    private string CalculateTransactionChecksum(TransactionalGroup transaction)
    {
        var data = new
        {
            transaction.TransactionId,
            transaction.Source,
            transaction.TenantId,
            transaction.StartTimestamp,
            transaction.SequenceNumber,
            EventCount = transaction.ChangeEvents.Count,
            EventOffsets = transaction.ChangeEvents.Select(e => e.Offset).OrderBy(o => o).ToArray()
        };

        var json = JsonSerializer.Serialize(data);
        var bytes = Encoding.UTF8.GetBytes(json);
        
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    /// <summary>
    /// Disposes the transactional group manager.
    /// </summary>
    public void Dispose()
    {
        _transactionSemaphore?.Dispose();
    }
}