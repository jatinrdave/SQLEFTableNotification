using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SqlDbEntityNotifier.Core.Interfaces;
using SqlDbEntityNotifier.Core.Models;

namespace SqlDbEntityNotifier.Core.Management;

/// <summary>
/// Manages replay operations for change events.
/// </summary>
public class ReplayManager
{
    private readonly ILogger<ReplayManager> _logger;
    private readonly IOffsetStore _offsetStore;
    private readonly IDbAdapter _dbAdapter;
    private readonly ReplayManagerOptions _options;

    /// <summary>
    /// Initializes a new instance of the ReplayManager class.
    /// </summary>
    public ReplayManager(
        ILogger<ReplayManager> logger,
        IOffsetStore offsetStore,
        IDbAdapter dbAdapter,
        IOptions<ReplayManagerOptions> options)
    {
        _logger = logger;
        _offsetStore = offsetStore;
        _dbAdapter = dbAdapter;
        _options = options.Value;
    }

    /// <summary>
    /// Replays events from a specific offset.
    /// </summary>
    /// <param name="fromOffset">The offset to start replaying from.</param>
    /// <param name="onChangeEvent">Callback to invoke for each replayed event.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the replay operation.</returns>
    public async Task<ReplayResult> ReplayFromOffsetAsync(
        string fromOffset,
        Func<ChangeEvent, CancellationToken, Task> onChangeEvent,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting replay from offset: {Offset}", fromOffset);

        var result = new ReplayResult
        {
            StartOffset = fromOffset,
            StartTime = DateTime.UtcNow
        };

        try
        {
            var eventCount = 0;
            var errorCount = 0;

            await _dbAdapter.ReplayFromOffsetAsync(fromOffset, async (changeEvent, ct) =>
            {
                try
                {
                    await onChangeEvent(changeEvent, ct);
                    eventCount++;
                    result.LastProcessedOffset = changeEvent.Offset;

                    // Update progress periodically
                    if (eventCount % _options.ProgressUpdateInterval == 0)
                    {
                        _logger.LogInformation("Replay progress: {EventCount} events processed, current offset: {Offset}", 
                            eventCount, changeEvent.Offset);
                    }
                }
                catch (Exception ex)
                {
                    errorCount++;
                    _logger.LogError(ex, "Error processing replayed event at offset: {Offset}", changeEvent.Offset);
                    
                    if (errorCount > _options.MaxErrors)
                    {
                        throw new ReplayException($"Too many errors during replay. Stopping at {errorCount} errors.", ex);
                    }
                }
            }, cancellationToken);

            result.Success = true;
            result.EventsProcessed = eventCount;
            result.ErrorsEncountered = errorCount;
            result.EndTime = DateTime.UtcNow;
            result.Duration = result.EndTime - result.StartTime;

            _logger.LogInformation("Replay completed successfully. Processed {EventCount} events in {Duration}", 
                eventCount, result.Duration);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = ex.Message;
            result.EndTime = DateTime.UtcNow;
            result.Duration = result.EndTime - result.StartTime;

            _logger.LogError(ex, "Replay failed after processing {EventCount} events", result.EventsProcessed);
        }

        return result;
    }

    /// <summary>
    /// Replays events from a specific timestamp.
    /// </summary>
    /// <param name="fromTimestamp">The timestamp to start replaying from.</param>
    /// <param name="onChangeEvent">Callback to invoke for each replayed event.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the replay operation.</returns>
    public async Task<ReplayResult> ReplayFromTimestampAsync(
        DateTime fromTimestamp,
        Func<ChangeEvent, CancellationToken, Task> onChangeEvent,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting replay from timestamp: {Timestamp}", fromTimestamp);

        // This is a simplified implementation - in a real scenario, you would need to
        // implement timestamp-to-offset conversion based on your database adapter
        var fromOffset = await ConvertTimestampToOffsetAsync(fromTimestamp, cancellationToken);
        
        return await ReplayFromOffsetAsync(fromOffset, onChangeEvent, cancellationToken);
    }

    /// <summary>
    /// Replays events for a specific time range.
    /// </summary>
    /// <param name="fromTimestamp">The start timestamp.</param>
    /// <param name="toTimestamp">The end timestamp.</param>
    /// <param name="onChangeEvent">Callback to invoke for each replayed event.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the replay operation.</returns>
    public async Task<ReplayResult> ReplayTimeRangeAsync(
        DateTime fromTimestamp,
        DateTime toTimestamp,
        Func<ChangeEvent, CancellationToken, Task> onChangeEvent,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting replay for time range: {FromTimestamp} to {ToTimestamp}", 
            fromTimestamp, toTimestamp);

        var result = new ReplayResult
        {
            StartTime = DateTime.UtcNow,
            FromTimestamp = fromTimestamp,
            ToTimestamp = toTimestamp
        };

        try
        {
            var eventCount = 0;
            var errorCount = 0;

            await _dbAdapter.ReplayFromOffsetAsync(
                await ConvertTimestampToOffsetAsync(fromTimestamp, cancellationToken),
                async (changeEvent, ct) =>
                {
                    // Check if we've reached the end timestamp
                    if (changeEvent.TimestampUtc > toTimestamp)
                    {
                        return; // Stop processing
                    }

                    try
                    {
                        await onChangeEvent(changeEvent, ct);
                        eventCount++;
                        result.LastProcessedOffset = changeEvent.Offset;
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        _logger.LogError(ex, "Error processing replayed event at offset: {Offset}", changeEvent.Offset);
                        
                        if (errorCount > _options.MaxErrors)
                        {
                            throw new ReplayException($"Too many errors during replay. Stopping at {errorCount} errors.", ex);
                        }
                    }
                }, cancellationToken);

            result.Success = true;
            result.EventsProcessed = eventCount;
            result.ErrorsEncountered = errorCount;
            result.EndTime = DateTime.UtcNow;
            result.Duration = result.EndTime - result.StartTime;

            _logger.LogInformation("Time range replay completed successfully. Processed {EventCount} events in {Duration}", 
                eventCount, result.Duration);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = ex.Message;
            result.EndTime = DateTime.UtcNow;
            result.Duration = result.EndTime - result.StartTime;

            _logger.LogError(ex, "Time range replay failed after processing {EventCount} events", result.EventsProcessed);
        }

        return result;
    }

    /// <summary>
    /// Gets available replay points (offsets) for a time range.
    /// </summary>
    /// <param name="fromTimestamp">The start timestamp.</param>
    /// <param name="toTimestamp">The end timestamp.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of available replay points.</returns>
    public async Task<IList<ReplayPoint>> GetAvailableReplayPointsAsync(
        DateTime fromTimestamp,
        DateTime toTimestamp,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting available replay points for time range: {FromTimestamp} to {ToTimestamp}", 
            fromTimestamp, toTimestamp);

        // This is a simplified implementation - in a real scenario, you would need to
        // implement this based on your database adapter's capabilities
        var replayPoints = new List<ReplayPoint>();

        try
        {
            // For now, return a single replay point at the start timestamp
            var startOffset = await ConvertTimestampToOffsetAsync(fromTimestamp, cancellationToken);
            replayPoints.Add(new ReplayPoint
            {
                Offset = startOffset,
                Timestamp = fromTimestamp,
                Description = $"Replay point at {fromTimestamp:O}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available replay points");
        }

        return replayPoints;
    }

    private async Task<string> ConvertTimestampToOffsetAsync(DateTime timestamp, CancellationToken cancellationToken)
    {
        // This is a simplified implementation - in a real scenario, you would need to
        // implement timestamp-to-offset conversion based on your database adapter
        // For now, return a placeholder offset
        return $"timestamp_{timestamp:yyyyMMddHHmmss}";
    }
}

/// <summary>
/// Configuration options for the replay manager.
/// </summary>
public sealed class ReplayManagerOptions
{
    /// <summary>
    /// Gets or sets the maximum number of errors allowed during replay.
    /// </summary>
    public int MaxErrors { get; set; } = 100;

    /// <summary>
    /// Gets or sets the interval for progress updates (number of events).
    /// </summary>
    public int ProgressUpdateInterval { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the maximum duration for a replay operation.
    /// </summary>
    public TimeSpan MaxReplayDuration { get; set; } = TimeSpan.FromHours(24);

    /// <summary>
    /// Gets or sets the batch size for replay operations.
    /// </summary>
    public int BatchSize { get; set; } = 100;
}

/// <summary>
/// Result of a replay operation.
/// </summary>
public sealed class ReplayResult
{
    /// <summary>
    /// Gets or sets whether the replay was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the start offset for the replay.
    /// </summary>
    public string StartOffset { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the last processed offset.
    /// </summary>
    public string LastProcessedOffset { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of events processed.
    /// </summary>
    public int EventsProcessed { get; set; }

    /// <summary>
    /// Gets or sets the number of errors encountered.
    /// </summary>
    public int ErrorsEncountered { get; set; }

    /// <summary>
    /// Gets or sets the start time of the replay.
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Gets or sets the end time of the replay.
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// Gets or sets the duration of the replay.
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Gets or sets the from timestamp for time range replays.
    /// </summary>
    public DateTime? FromTimestamp { get; set; }

    /// <summary>
    /// Gets or sets the to timestamp for time range replays.
    /// </summary>
    public DateTime? ToTimestamp { get; set; }

    /// <summary>
    /// Gets or sets any error message.
    /// </summary>
    public string? Error { get; set; }
}

/// <summary>
/// Represents a replay point (offset and timestamp).
/// </summary>
public sealed class ReplayPoint
{
    /// <summary>
    /// Gets or sets the offset.
    /// </summary>
    public string Offset { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets a description of the replay point.
    /// </summary>
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Exception thrown during replay operations.
/// </summary>
public class ReplayException : Exception
{
    /// <summary>
    /// Initializes a new instance of the ReplayException class.
    /// </summary>
    public ReplayException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the ReplayException class.
    /// </summary>
    public ReplayException(string message, Exception innerException) : base(message, innerException)
    {
    }
}