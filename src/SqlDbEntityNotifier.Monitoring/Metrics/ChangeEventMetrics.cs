using Microsoft.Extensions.Logging;
using Prometheus;
using SqlDbEntityNotifier.Core.Models;

namespace SqlDbEntityNotifier.Monitoring.Metrics;

/// <summary>
/// Metrics collector for change events and system performance.
/// </summary>
public class ChangeEventMetrics
{
    private readonly ILogger<ChangeEventMetrics> _logger;
    
    // Prometheus metrics
    private readonly Counter _eventsProcessedTotal;
    private readonly Counter _eventsFailedTotal;
    private readonly Counter _eventsPublishedTotal;
    private readonly Counter _eventsPublishedFailedTotal;
    private readonly Gauge _lagSeconds;
    private readonly Gauge _lastOffsetProcessed;
    private readonly Histogram _processingDuration;
    private readonly Histogram _publishDuration;
    private readonly Counter _retryAttemptsTotal;
    private readonly Counter _dlqEventsTotal;

    /// <summary>
    /// Initializes a new instance of the ChangeEventMetrics class.
    /// </summary>
    public ChangeEventMetrics(ILogger<ChangeEventMetrics> logger)
    {
        _logger = logger;

        // Initialize Prometheus metrics
        _eventsProcessedTotal = Metrics
            .CreateCounter("sqldb_events_processed_total", "Total number of change events processed", 
                new[] { "source", "schema", "table", "operation" });

        _eventsFailedTotal = Metrics
            .CreateCounter("sqldb_events_failed_total", "Total number of change events that failed processing", 
                new[] { "source", "schema", "table", "operation", "error_type" });

        _eventsPublishedTotal = Metrics
            .CreateCounter("sqldb_events_published_total", "Total number of change events published", 
                new[] { "source", "publisher", "destination" });

        _eventsPublishedFailedTotal = Metrics
            .CreateCounter("sqldb_events_published_failed_total", "Total number of change events that failed to publish", 
                new[] { "source", "publisher", "destination", "error_type" });

        _lagSeconds = Metrics
            .CreateGauge("sqldb_lag_seconds", "Current lag in seconds for each source", 
                new[] { "source", "schema", "table" });

        _lastOffsetProcessed = Metrics
            .CreateGauge("sqldb_last_offset_processed", "Last processed offset for each source", 
                new[] { "source", "schema", "table" });

        _processingDuration = Metrics
            .CreateHistogram("sqldb_processing_duration_seconds", "Time spent processing change events", 
                new[] { "source", "schema", "table" });

        _publishDuration = Metrics
            .CreateHistogram("sqldb_publish_duration_seconds", "Time spent publishing change events", 
                new[] { "source", "publisher", "destination" });

        _retryAttemptsTotal = Metrics
            .CreateCounter("sqldb_retry_attempts_total", "Total number of retry attempts", 
                new[] { "source", "publisher", "destination", "retry_reason" });

        _dlqEventsTotal = Metrics
            .CreateCounter("sqldb_dlq_events_total", "Total number of events sent to dead letter queue", 
                new[] { "source", "publisher", "destination", "failure_reason" });

        _logger.LogInformation("ChangeEventMetrics initialized");
    }

    /// <summary>
    /// Records a successfully processed change event.
    /// </summary>
    public void RecordEventProcessed(ChangeEvent changeEvent, TimeSpan processingDuration)
    {
        try
        {
            var labels = new[] { changeEvent.Source, changeEvent.Schema, changeEvent.Table, changeEvent.Operation };
            _eventsProcessedTotal.WithLabels(labels).Inc();
            _processingDuration.WithLabels(changeEvent.Source, changeEvent.Schema, changeEvent.Table)
                .Observe(processingDuration.TotalSeconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording processed event metrics");
        }
    }

    /// <summary>
    /// Records a failed change event processing.
    /// </summary>
    public void RecordEventFailed(ChangeEvent changeEvent, Exception exception)
    {
        try
        {
            var errorType = exception.GetType().Name;
            var labels = new[] { changeEvent.Source, changeEvent.Schema, changeEvent.Table, changeEvent.Operation, errorType };
            _eventsFailedTotal.WithLabels(labels).Inc();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording failed event metrics");
        }
    }

    /// <summary>
    /// Records a successfully published change event.
    /// </summary>
    public void RecordEventPublished(string source, string publisher, string destination, TimeSpan publishDuration)
    {
        try
        {
            var labels = new[] { source, publisher, destination };
            _eventsPublishedTotal.WithLabels(labels).Inc();
            _publishDuration.WithLabels(source, publisher, destination).Observe(publishDuration.TotalSeconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording published event metrics");
        }
    }

    /// <summary>
    /// Records a failed change event publishing.
    /// </summary>
    public void RecordEventPublishFailed(string source, string publisher, string destination, Exception exception)
    {
        try
        {
            var errorType = exception.GetType().Name;
            var labels = new[] { source, publisher, destination, errorType };
            _eventsPublishedFailedTotal.WithLabels(labels).Inc();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording failed publish metrics");
        }
    }

    /// <summary>
    /// Updates the lag metric for a source.
    /// </summary>
    public void UpdateLag(string source, string schema, string table, double lagSeconds)
    {
        try
        {
            _lagSeconds.WithLabels(source, schema, table).Set(lagSeconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating lag metrics");
        }
    }

    /// <summary>
    /// Updates the last processed offset for a source.
    /// </summary>
    public void UpdateLastOffset(string source, string schema, string table, long offset)
    {
        try
        {
            _lastOffsetProcessed.WithLabels(source, schema, table).Set(offset);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating last offset metrics");
        }
    }

    /// <summary>
    /// Records a retry attempt.
    /// </summary>
    public void RecordRetryAttempt(string source, string publisher, string destination, string retryReason)
    {
        try
        {
            var labels = new[] { source, publisher, destination, retryReason };
            _retryAttemptsTotal.WithLabels(labels).Inc();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording retry attempt metrics");
        }
    }

    /// <summary>
    /// Records an event sent to the dead letter queue.
    /// </summary>
    public void RecordDlqEvent(string source, string publisher, string destination, string failureReason)
    {
        try
        {
            var labels = new[] { source, publisher, destination, failureReason };
            _dlqEventsTotal.WithLabels(labels).Inc();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording DLQ event metrics");
        }
    }

    /// <summary>
    /// Gets the current metrics summary.
    /// </summary>
    public MetricsSummary GetMetricsSummary()
    {
        try
        {
            return new MetricsSummary
            {
                EventsProcessedTotal = _eventsProcessedTotal.Value,
                EventsFailedTotal = _eventsFailedTotal.Value,
                EventsPublishedTotal = _eventsPublishedTotal.Value,
                EventsPublishedFailedTotal = _eventsPublishedFailedTotal.Value,
                RetryAttemptsTotal = _retryAttemptsTotal.Value,
                DlqEventsTotal = _dlqEventsTotal.Value,
                Timestamp = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting metrics summary");
            return new MetricsSummary { Timestamp = DateTime.UtcNow };
        }
    }
}

/// <summary>
/// Summary of current metrics.
/// </summary>
public sealed class MetricsSummary
{
    /// <summary>
    /// Gets or sets the total number of events processed.
    /// </summary>
    public double EventsProcessedTotal { get; set; }

    /// <summary>
    /// Gets or sets the total number of events that failed.
    /// </summary>
    public double EventsFailedTotal { get; set; }

    /// <summary>
    /// Gets or sets the total number of events published.
    /// </summary>
    public double EventsPublishedTotal { get; set; }

    /// <summary>
    /// Gets or sets the total number of events that failed to publish.
    /// </summary>
    public double EventsPublishedFailedTotal { get; set; }

    /// <summary>
    /// Gets or sets the total number of retry attempts.
    /// </summary>
    public double RetryAttemptsTotal { get; set; }

    /// <summary>
    /// Gets or sets the total number of DLQ events.
    /// </summary>
    public double DlqEventsTotal { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the summary was generated.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets the success rate for event processing.
    /// </summary>
    public double ProcessingSuccessRate => EventsProcessedTotal > 0 
        ? (EventsProcessedTotal - EventsFailedTotal) / EventsProcessedTotal * 100 
        : 100;

    /// <summary>
    /// Gets the success rate for event publishing.
    /// </summary>
    public double PublishingSuccessRate => EventsPublishedTotal > 0 
        ? (EventsPublishedTotal - EventsPublishedFailedTotal) / EventsPublishedTotal * 100 
        : 100;
}