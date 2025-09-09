using SqlDbEntityNotifier.Core.Models;

namespace SqlDbEntityNotifier.Core.Interfaces;

/// <summary>
/// Main interface for the SQLDBEntityNotifier that manages database change subscriptions.
/// </summary>
public interface IEntityNotifier : IAsyncDisposable
{
    /// <summary>
    /// Starts the notifier and begins monitoring for database changes.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to stop the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the notifier and all active subscriptions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to stop the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscribes to change events for a specific entity type with the given options.
    /// </summary>
    /// <typeparam name="TDto">The DTO type to deserialize change events to.</typeparam>
    /// <param name="options">Subscription configuration options.</param>
    /// <param name="handler">Handler function to process change events.</param>
    /// <param name="cancellationToken">Cancellation token to stop the operation.</param>
    /// <returns>A subscription handle that can be used to manage the subscription.</returns>
    Task<SubscriptionHandle> SubscribeAsync<TDto>(
        SubscriptionOptions options,
        Func<ChangeEvent, TDto, CancellationToken, Task> handler,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current status of the notifier.
    /// </summary>
    /// <returns>The notifier status information.</returns>
    Task<NotifierStatus> GetStatusAsync();
}