using SqlDbEntityNotifier.Core.Models;

namespace SqlDbEntityNotifier.Core.Interfaces;

/// <summary>
/// Interface for publishing change events to external systems.
/// </summary>
public interface IChangePublisher
{
    /// <summary>
    /// Publishes a change event to the configured destination.
    /// </summary>
    /// <param name="changeEvent">The change event to publish.</param>
    /// <param name="cancellationToken">Cancellation token to stop the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task PublishAsync(ChangeEvent changeEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes a batch of change events to the configured destination.
    /// </summary>
    /// <param name="changeEvents">The change events to publish.</param>
    /// <param name="cancellationToken">Cancellation token to stop the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task PublishBatchAsync(IEnumerable<ChangeEvent> changeEvents, CancellationToken cancellationToken = default);
}