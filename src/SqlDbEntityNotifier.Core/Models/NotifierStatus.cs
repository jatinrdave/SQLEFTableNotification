namespace SqlDbEntityNotifier.Core.Models;

/// <summary>
/// Represents the current status of the entity notifier.
/// </summary>
public sealed class NotifierStatus
{
    /// <summary>
    /// Gets whether the notifier is currently running.
    /// </summary>
    public bool IsRunning { get; init; }

    /// <summary>
    /// Gets the number of active subscriptions.
    /// </summary>
    public int ActiveSubscriptions { get; init; }

    /// <summary>
    /// Gets the total number of events processed.
    /// </summary>
    public long EventsProcessed { get; init; }

    /// <summary>
    /// Gets the total number of events that failed processing.
    /// </summary>
    public long EventsFailed { get; init; }

    /// <summary>
    /// Gets the current lag in seconds for each source.
    /// </summary>
    public IDictionary<string, double> LagBySource { get; init; } = new Dictionary<string, double>();

    /// <summary>
    /// Gets the timestamp when the notifier was last started.
    /// </summary>
    public DateTime? LastStartedUtc { get; init; }

    /// <summary>
    /// Gets any error messages or warnings.
    /// </summary>
    public IList<string> Messages { get; init; } = new List<string>();
}