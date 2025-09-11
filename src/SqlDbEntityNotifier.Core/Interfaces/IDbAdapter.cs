using SqlDbEntityNotifier.Core.Models;

namespace SqlDbEntityNotifier.Core.Interfaces;

/// <summary>
/// Interface for database adapters that can monitor and extract change events.
/// </summary>
public interface IDbAdapter
{
    /// <summary>
    /// Gets the database source identifier.
    /// </summary>
    string Source { get; }

    /// <summary>
    /// Starts monitoring for database changes.
    /// </summary>
    /// <param name="onChangeEvent">Callback to invoke when a change event is detected.</param>
    /// <param name="cancellationToken">Cancellation token to stop the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StartAsync(Func<ChangeEvent, CancellationToken, Task> onChangeEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops monitoring for database changes.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to stop the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current offset for the database source.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to stop the operation.</param>
    /// <returns>The current offset string.</returns>
    Task<string> GetCurrentOffsetAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the offset for the database source.
    /// </summary>
    /// <param name="offset">The offset to set.</param>
    /// <param name="cancellationToken">Cancellation token to stop the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SetOffsetAsync(string offset, CancellationToken cancellationToken = default);

    /// <summary>
    /// Replays events from a specific offset.
    /// </summary>
    /// <param name="fromOffset">The offset to start replaying from.</param>
    /// <param name="onChangeEvent">Callback to invoke for each replayed event.</param>
    /// <param name="cancellationToken">Cancellation token to stop the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ReplayFromOffsetAsync(string fromOffset, Func<ChangeEvent, CancellationToken, Task> onChangeEvent, CancellationToken cancellationToken = default);
}