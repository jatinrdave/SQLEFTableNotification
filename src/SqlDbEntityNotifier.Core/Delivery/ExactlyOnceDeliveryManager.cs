using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SqlDbEntityNotifier.Core.Interfaces;
using SqlDbEntityNotifier.Core.Models;
using SqlDbEntityNotifier.Core.Delivery.Models;
using SqlDbEntityNotifier.Core.Transactional.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SqlDbEntityNotifier.Core.Delivery;

/// <summary>
/// Manages exactly-once delivery semantics for change events.
/// </summary>
public class ExactlyOnceDeliveryManager
{
    private readonly ILogger<ExactlyOnceDeliveryManager> _logger;
    private readonly ExactlyOnceOptions _options;
    private readonly IIdempotencyStore _idempotencyStore;
    private readonly IDeduplicationStore _deduplicationStore;
    private readonly IAcknowledgmentStore _acknowledgmentStore;
    private readonly Dictionary<string, DeliverySession> _activeSessions;
    private readonly SemaphoreSlim _deliverySemaphore;

    /// <summary>
    /// Initializes a new instance of the ExactlyOnceDeliveryManager class.
    /// </summary>
    public ExactlyOnceDeliveryManager(
        ILogger<ExactlyOnceDeliveryManager> logger,
        IOptions<ExactlyOnceOptions> options,
        IIdempotencyStore idempotencyStore,
        IDeduplicationStore deduplicationStore,
        IAcknowledgmentStore acknowledgmentStore)
    {
        _logger = logger;
        _options = options.Value;
        _idempotencyStore = idempotencyStore;
        _deduplicationStore = deduplicationStore;
        _acknowledgmentStore = acknowledgmentStore;
        _activeSessions = new Dictionary<string, DeliverySession>();
        _deliverySemaphore = new SemaphoreSlim(_options.Idempotency.MaxKeys, _options.Idempotency.MaxKeys);
    }

    /// <summary>
    /// Delivers a change event with exactly-once semantics.
    /// </summary>
    /// <param name="changeEvent">The change event to deliver.</param>
    /// <param name="publisher">The publisher to use for delivery.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A delivery result indicating success or failure.</returns>
    public async Task<DeliveryResult> DeliverExactlyOnceAsync(
        ChangeEvent changeEvent,
        IChangePublisher publisher,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _deliverySemaphore.WaitAsync(cancellationToken);

            // Generate idempotency key
            var idempotencyKey = GenerateIdempotencyKey(changeEvent);

            // Check for duplicate delivery
            if (await IsDuplicateDeliveryAsync(idempotencyKey, cancellationToken))
            {
                _logger.LogDebug("Duplicate delivery detected for event: {EventId}, key: {Key}", 
                    changeEvent.Offset, idempotencyKey);
                
                return new DeliveryResult
                {
                    IsSuccess = true,
                    IsDuplicate = true,
                    IdempotencyKey = idempotencyKey,
                    DeliveryAttempts = 1,
                    DeliveryTime = DateTime.UtcNow
                };
            }

            // Check for content deduplication
            if (await IsContentDuplicateAsync(changeEvent, cancellationToken))
            {
                _logger.LogDebug("Content duplicate detected for event: {EventId}", changeEvent.Offset);
                
                return new DeliveryResult
                {
                    IsSuccess = true,
                    IsDuplicate = true,
                    IdempotencyKey = idempotencyKey,
                    DeliveryAttempts = 1,
                    DeliveryTime = DateTime.UtcNow
                };
            }

            // Create delivery session
            var session = new DeliverySession
            {
                IdempotencyKey = idempotencyKey,
                ChangeEvent = changeEvent,
                Publisher = publisher,
                StartTime = DateTime.UtcNow,
                Status = DeliveryStatus.InProgress
            };

            _activeSessions[idempotencyKey] = session;

            // Perform delivery with retry logic
            var result = await PerformDeliveryWithRetryAsync(session, cancellationToken);

            // Store idempotency key
            await _idempotencyStore.StoreIdempotencyKeyAsync(idempotencyKey, changeEvent, cancellationToken);

            // Store content hash for deduplication
            await _deduplicationStore.StoreContentHashAsync(changeEvent, cancellationToken);

            // Handle acknowledgment if required
            if (_options.Acknowledgment.Required && result.IsSuccess)
            {
                await HandleAcknowledgmentAsync(session, result, cancellationToken);
            }

            // Remove from active sessions
            _activeSessions.Remove(idempotencyKey);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in exactly-once delivery for event: {EventId}", changeEvent.Offset);
            
            return new DeliveryResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message,
                DeliveryAttempts = 1,
                DeliveryTime = DateTime.UtcNow
            };
        }
        finally
        {
            _deliverySemaphore.Release();
        }
    }

    /// <summary>
    /// Delivers a transactional group with exactly-once semantics.
    /// </summary>
    /// <param name="transaction">The transactional group to deliver.</param>
    /// <param name="publisher">The publisher to use for delivery.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A delivery result indicating success or failure.</returns>
    public async Task<DeliveryResult> DeliverTransactionalGroupExactlyOnceAsync(
        TransactionalGroup transaction,
        IChangePublisher publisher,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Delivering transactional group: {TransactionId} with {EventCount} events", 
                transaction.TransactionId, transaction.ChangeEvents.Count);

            var results = new List<DeliveryResult>();
            var failedEvents = new List<ChangeEvent>();

            // Deliver each event in the transaction
            foreach (var changeEvent in transaction.ChangeEvents)
            {
                var result = await DeliverExactlyOnceAsync(changeEvent, publisher, cancellationToken);
                results.Add(result);

                if (!result.IsSuccess)
                {
                    failedEvents.Add(changeEvent);
                }
            }

            // Determine overall transaction delivery result
            var overallSuccess = results.All(r => r.IsSuccess);
            var totalAttempts = results.Sum(r => r.DeliveryAttempts);
            var duplicates = results.Count(r => r.IsDuplicate);

            var transactionResult = new DeliveryResult
            {
                IsSuccess = overallSuccess,
                IsDuplicate = duplicates == transaction.ChangeEvents.Count,
                DeliveryAttempts = totalAttempts,
                DeliveryTime = DateTime.UtcNow,
                TransactionId = transaction.TransactionId,
                EventCount = transaction.ChangeEvents.Count,
                FailedEventCount = failedEvents.Count
            };

            if (!overallSuccess)
            {
                transactionResult.ErrorMessage = $"Failed to deliver {failedEvents.Count} out of {transaction.ChangeEvents.Count} events";
            }

            _logger.LogInformation("Transactional group delivery completed: {TransactionId}, success: {Success}, events: {EventCount}/{TotalEvents}", 
                transaction.TransactionId, overallSuccess, transaction.ChangeEvents.Count - failedEvents.Count, transaction.ChangeEvents.Count);

            return transactionResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error delivering transactional group: {TransactionId}", transaction.TransactionId);
            
            return new DeliveryResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message,
                DeliveryAttempts = 1,
                DeliveryTime = DateTime.UtcNow,
                TransactionId = transaction.TransactionId
            };
        }
    }

    /// <summary>
    /// Acknowledges a delivery.
    /// </summary>
    /// <param name="idempotencyKey">The idempotency key.</param>
    /// <param name="acknowledgment">The acknowledgment data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the operation.</returns>
    public async Task AcknowledgeDeliveryAsync(
        string idempotencyKey,
        AcknowledgmentData acknowledgment,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _acknowledgmentStore.StoreAcknowledgmentAsync(idempotencyKey, acknowledgment, cancellationToken);
            
            _logger.LogDebug("Delivery acknowledged: {IdempotencyKey}", idempotencyKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error acknowledging delivery: {IdempotencyKey}", idempotencyKey);
            throw;
        }
    }

    /// <summary>
    /// Gets the delivery status for an idempotency key.
    /// </summary>
    /// <param name="idempotencyKey">The idempotency key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The delivery status.</returns>
    public async Task<DeliveryStatus> GetDeliveryStatusAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check active sessions first
            if (_activeSessions.TryGetValue(idempotencyKey, out var session))
            {
                return session.Status;
            }

            // Check acknowledgment store
            var acknowledgment = await _acknowledgmentStore.GetAcknowledgmentAsync(idempotencyKey, cancellationToken);
            if (acknowledgment != null)
            {
                return DeliveryStatus.Successful;
            }

            // Check idempotency store
            var idempotencyData = await _idempotencyStore.GetIdempotencyDataAsync(idempotencyKey, cancellationToken);
            if (idempotencyData != null)
            {
                return DeliveryStatus.Successful;
            }

            return DeliveryStatus.Pending;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting delivery status: {IdempotencyKey}", idempotencyKey);
            throw;
        }
    }

    private async Task<bool> IsDuplicateDeliveryAsync(string idempotencyKey, CancellationToken cancellationToken)
    {
        try
        {
            var existingData = await _idempotencyStore.GetIdempotencyDataAsync(idempotencyKey, cancellationToken);
            return existingData != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking duplicate delivery: {IdempotencyKey}", idempotencyKey);
            return false; // Allow delivery on error
        }
    }

    private async Task<bool> IsContentDuplicateAsync(ChangeEvent changeEvent, CancellationToken cancellationToken)
    {
        try
        {
            if (!_options.Deduplication.Enabled)
            {
                return false;
            }

            var contentHash = CalculateContentHash(changeEvent);
            return await _deduplicationStore.IsContentDuplicateAsync(contentHash, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking content duplicate: {EventId}", changeEvent.Offset);
            return false; // Allow delivery on error
        }
    }

    private async Task<DeliveryResult> PerformDeliveryWithRetryAsync(
        DeliverySession session,
        CancellationToken cancellationToken)
    {
        var attempt = 0;
        var delay = _options.Retry.InitialDelaySeconds;

        while (attempt < _options.Retry.MaxAttempts)
        {
            attempt++;
            session.Attempts.Add(new DeliveryAttempt
            {
                AttemptNumber = attempt,
                Timestamp = DateTime.UtcNow,
                Status = DeliveryStatus.InProgress
            });

            try
            {
                _logger.LogDebug("Delivery attempt {Attempt} for event: {EventId}", attempt, session.ChangeEvent.Offset);

                // Perform the actual delivery
                await session.Publisher.PublishAsync(session.ChangeEvent, cancellationToken);

                // Mark attempt as successful
                session.Attempts.Last().Status = DeliveryStatus.Successful;
                session.Status = DeliveryStatus.Successful;

                _logger.LogDebug("Delivery successful for event: {EventId} on attempt {Attempt}", 
                    session.ChangeEvent.Offset, attempt);

                return new DeliveryResult
                {
                    IsSuccess = true,
                    IsDuplicate = false,
                    IdempotencyKey = session.IdempotencyKey,
                    DeliveryAttempts = attempt,
                    DeliveryTime = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Delivery attempt {Attempt} failed for event: {EventId}", 
                    attempt, session.ChangeEvent.Offset);

                // Mark attempt as failed
                session.Attempts.Last().Status = DeliveryStatus.Failed;
                session.Attempts.Last().ErrorMessage = ex.Message;

                if (attempt >= _options.Retry.MaxAttempts)
                {
                    session.Status = DeliveryStatus.Failed;
                    
                    return new DeliveryResult
                    {
                        IsSuccess = false,
                        ErrorMessage = ex.Message,
                        IdempotencyKey = session.IdempotencyKey,
                        DeliveryAttempts = attempt,
                        DeliveryTime = DateTime.UtcNow
                    };
                }

                // Wait before retry
                await Task.Delay(TimeSpan.FromSeconds(delay), cancellationToken);
                delay = Math.Min((int)(delay * _options.Retry.BackoffMultiplier), _options.Retry.MaxDelaySeconds);
            }
        }

        session.Status = DeliveryStatus.Failed;
        return new DeliveryResult
        {
            IsSuccess = false,
            ErrorMessage = "Maximum retry attempts exceeded",
            IdempotencyKey = session.IdempotencyKey,
            DeliveryAttempts = attempt,
            DeliveryTime = DateTime.UtcNow
        };
    }

    private async Task HandleAcknowledgmentAsync(
        DeliverySession session,
        DeliveryResult result,
        CancellationToken cancellationToken)
    {
        try
        {
            var acknowledgment = new AcknowledgmentData
            {
                IdempotencyKey = session.IdempotencyKey,
                EventId = session.ChangeEvent.Offset,
                Timestamp = DateTime.UtcNow,
                Status = result.IsSuccess ? AcknowledgmentStatus.Success : AcknowledgmentStatus.Failure,
                DeliveryAttempts = result.DeliveryAttempts,
                ErrorMessage = result.ErrorMessage
            };

            await _acknowledgmentStore.StoreAcknowledgmentAsync(session.IdempotencyKey, acknowledgment, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling acknowledgment for event: {EventId}", session.ChangeEvent.Offset);
        }
    }

    private string GenerateIdempotencyKey(ChangeEvent changeEvent)
    {
        return _options.Idempotency.KeyStrategy switch
        {
            IdempotencyKeyStrategy.Offset => changeEvent.Offset,
            IdempotencyKeyStrategy.ContentHash => CalculateContentHash(changeEvent),
            IdempotencyKeyStrategy.Composite => GenerateCompositeKey(changeEvent),
            _ => GenerateCompositeKey(changeEvent)
        };
    }

    private string GenerateCompositeKey(ChangeEvent changeEvent)
    {
        var keyData = new
        {
            changeEvent.Source,
            changeEvent.Schema,
            changeEvent.Table,
            changeEvent.Operation,
            changeEvent.Offset,
            changeEvent.TimestampUtc
        };

        var json = JsonSerializer.Serialize(keyData);
        var bytes = Encoding.UTF8.GetBytes(json);
        
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    private string CalculateContentHash(ChangeEvent changeEvent)
    {
        var content = new
        {
            changeEvent.Before,
            changeEvent.After,
            changeEvent.Metadata
        };

        var json = JsonSerializer.Serialize(content);
        var bytes = Encoding.UTF8.GetBytes(json);
        
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    /// <summary>
    /// Disposes the exactly-once delivery manager.
    /// </summary>
    public void Dispose()
    {
        _deliverySemaphore?.Dispose();
    }
}

/// <summary>
/// Represents a delivery session for tracking delivery state.
/// </summary>
public sealed class DeliverySession
{
    /// <summary>
    /// Gets or sets the idempotency key.
    /// </summary>
    public string IdempotencyKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the change event being delivered.
    /// </summary>
    public ChangeEvent ChangeEvent { get; set; } = null!;

    /// <summary>
    /// Gets or sets the publisher used for delivery.
    /// </summary>
    public IChangePublisher Publisher { get; set; } = null!;

    /// <summary>
    /// Gets or sets the session start time.
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Gets or sets the session status.
    /// </summary>
    public DeliveryStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the delivery attempts.
    /// </summary>
    public IList<DeliveryAttempt> Attempts { get; set; } = new List<DeliveryAttempt>();
}

/// <summary>
/// Represents the result of a delivery operation.
/// </summary>
public sealed class DeliveryResult
{
    /// <summary>
    /// Gets or sets whether the delivery was successful.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Gets or sets whether this was a duplicate delivery.
    /// </summary>
    public bool IsDuplicate { get; set; }

    /// <summary>
    /// Gets or sets the idempotency key used.
    /// </summary>
    public string IdempotencyKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of delivery attempts.
    /// </summary>
    public int DeliveryAttempts { get; set; }

    /// <summary>
    /// Gets or sets the delivery time.
    /// </summary>
    public DateTime DeliveryTime { get; set; }

    /// <summary>
    /// Gets or sets the error message (if failed).
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the transaction ID (if part of a transaction).
    /// </summary>
    public string? TransactionId { get; set; }

    /// <summary>
    /// Gets or sets the number of events in the transaction.
    /// </summary>
    public int EventCount { get; set; }

    /// <summary>
    /// Gets or sets the number of failed events in the transaction.
    /// </summary>
    public int FailedEventCount { get; set; }
}

/// <summary>
/// Represents acknowledgment data for a delivery.
/// </summary>
public sealed class AcknowledgmentData
{
    /// <summary>
    /// Gets or sets the idempotency key.
    /// </summary>
    public string IdempotencyKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event ID.
    /// </summary>
    public string EventId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the acknowledgment timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the acknowledgment status.
    /// </summary>
    public AcknowledgmentStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the number of delivery attempts.
    /// </summary>
    public int DeliveryAttempts { get; set; }

    /// <summary>
    /// Gets or sets the error message (if failed).
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the acknowledgment metadata.
    /// </summary>
    public IDictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
}

/// <summary>
/// Acknowledgment status enumeration.
/// </summary>
public enum AcknowledgmentStatus
{
    /// <summary>
    /// Acknowledgment is pending.
    /// </summary>
    Pending,

    /// <summary>
    /// Acknowledgment was successful.
    /// </summary>
    Success,

    /// <summary>
    /// Acknowledgment failed.
    /// </summary>
    Failure,

    /// <summary>
    /// Acknowledgment timed out.
    /// </summary>
    Timeout
}