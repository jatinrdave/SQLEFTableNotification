namespace SqlDbEntityNotifier.Core.Interfaces;

/// <summary>
/// Interface for storing and retrieving offset information for database sources.
/// </summary>
public interface IOffsetStore
{
    /// <summary>
    /// Gets the stored offset for a specific source.
    /// </summary>
    /// <param name="source">The database source identifier.</param>
    /// <param name="cancellationToken">Cancellation token to stop the operation.</param>
    /// <returns>The stored offset, or null if no offset exists.</returns>
    Task<string?> GetOffsetAsync(string source, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores the offset for a specific source.
    /// </summary>
    /// <param name="source">The database source identifier.</param>
    /// <param name="offset">The offset to store.</param>
    /// <param name="cancellationToken">Cancellation token to stop the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SetOffsetAsync(string source, string offset, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the stored offset for a specific source.
    /// </summary>
    /// <param name="source">The database source identifier.</param>
    /// <param name="cancellationToken">Cancellation token to stop the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteOffsetAsync(string source, CancellationToken cancellationToken = default);
}