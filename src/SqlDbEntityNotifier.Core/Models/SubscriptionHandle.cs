namespace SqlDbEntityNotifier.Core.Models;

/// <summary>
/// Represents a handle to an active subscription that can be used to manage its lifecycle.
/// </summary>
public sealed class SubscriptionHandle : IAsyncDisposable
{
    private readonly Func<Task> _unsubscribeAction;
    private bool _disposed;

    /// <summary>
    /// Gets the subscription identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the subscription options.
    /// </summary>
    public SubscriptionOptions Options { get; }

    /// <summary>
    /// Gets whether the subscription is active.
    /// </summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Initializes a new instance of the SubscriptionHandle class.
    /// </summary>
    public SubscriptionHandle(string id, SubscriptionOptions options, Func<Task> unsubscribeAction)
    {
        Id = id;
        Options = options;
        _unsubscribeAction = unsubscribeAction;
    }

    /// <summary>
    /// Unsubscribes from the change events and disposes the handle.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (!_disposed && IsActive)
        {
            try
            {
                await _unsubscribeAction();
            }
            finally
            {
                IsActive = false;
                _disposed = true;
            }
        }
    }
}