using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SqlDbEntityNotifier.Tracing.Models;

namespace SqlDbEntityNotifier.Tracing;

/// <summary>
/// Extensions for OpenTelemetry integration.
/// </summary>
public static class OpenTelemetryExtensions
{
    /// <summary>
    /// Adds OpenTelemetry tracing to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddOpenTelemetryTracing(
        this IServiceCollection services,
        Action<TracingOptions>? configure = null)
    {
        if (configure != null)
        {
            services.Configure(configure);
        }

        services.AddSingleton<ChangeEventTracer>();
        services.AddSingleton<ITraceContextProvider, TraceContextProvider>();
        services.AddSingleton<ITraceMetricsCollector, TraceMetricsCollector>();

        return services;
    }

    /// <summary>
    /// Adds OpenTelemetry tracing to the host builder.
    /// </summary>
    /// <param name="hostBuilder">The host builder.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The host builder.</returns>
    public static IHostBuilder AddOpenTelemetryTracing(
        this IHostBuilder hostBuilder,
        Action<TracingOptions>? configure = null)
    {
        return hostBuilder.ConfigureServices(services =>
        {
            services.AddOpenTelemetryTracing(configure);
        });
    }
}

/// <summary>
/// Interface for trace context providers.
/// </summary>
public interface ITraceContextProvider
{
    /// <summary>
    /// Gets the current trace context.
    /// </summary>
    /// <returns>The current trace context or null if not available.</returns>
    ActivityContext? GetCurrentTraceContext();

    /// <summary>
    /// Sets the current trace context.
    /// </summary>
    /// <param name="context">The trace context to set.</param>
    void SetCurrentTraceContext(ActivityContext context);

    /// <summary>
    /// Creates a new trace context.
    /// </summary>
    /// <returns>A new trace context.</returns>
    ActivityContext CreateTraceContext();
}

/// <summary>
/// Implementation of trace context provider.
/// </summary>
public class TraceContextProvider : ITraceContextProvider
{
    private readonly ILogger<TraceContextProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the TraceContextProvider class.
    /// </summary>
    public TraceContextProvider(ILogger<TraceContextProvider> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public ActivityContext? GetCurrentTraceContext()
    {
        try
        {
            var activity = Activity.Current;
            return activity?.Context;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current trace context");
            return null;
        }
    }

    /// <inheritdoc />
    public void SetCurrentTraceContext(ActivityContext context)
    {
        try
        {
            // In a real implementation, you would set the current activity context
            // This is a simplified implementation
            _logger.LogDebug("Setting trace context: {TraceId}", context.TraceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting trace context");
        }
    }

    /// <inheritdoc />
    public ActivityContext CreateTraceContext()
    {
        try
        {
            var traceId = ActivityTraceId.CreateRandom();
            var spanId = ActivitySpanId.CreateRandom();
            return new ActivityContext(traceId, spanId, ActivityTraceFlags.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating trace context");
            return new ActivityContext();
        }
    }
}

/// <summary>
/// Interface for trace metrics collectors.
/// </summary>
public interface ITraceMetricsCollector
{
    /// <summary>
    /// Records a trace metric.
    /// </summary>
    /// <param name="metricName">The metric name.</param>
    /// <param name="value">The metric value.</param>
    /// <param name="tags">Optional tags for the metric.</param>
    void RecordMetric(string metricName, double value, IDictionary<string, object>? tags = null);

    /// <summary>
    /// Records a trace counter.
    /// </summary>
    /// <param name="counterName">The counter name.</param>
    /// <param name="increment">The increment value.</param>
    /// <param name="tags">Optional tags for the counter.</param>
    void RecordCounter(string counterName, long increment = 1, IDictionary<string, object>? tags = null);

    /// <summary>
    /// Records a trace histogram.
    /// </summary>
    /// <param name="histogramName">The histogram name.</param>
    /// <param name="value">The histogram value.</param>
    /// <param name="tags">Optional tags for the histogram.</param>
    void RecordHistogram(string histogramName, double value, IDictionary<string, object>? tags = null);

    /// <summary>
    /// Gets the current trace metrics.
    /// </summary>
    /// <returns>The current trace metrics.</returns>
    TraceMetrics GetMetrics();
}

/// <summary>
/// Implementation of trace metrics collector.
/// </summary>
public class TraceMetricsCollector : ITraceMetricsCollector
{
    private readonly ILogger<TraceMetricsCollector> _logger;
    private readonly Dictionary<string, double> _metrics;
    private readonly Dictionary<string, long> _counters;
    private readonly Dictionary<string, List<double>> _histograms;
    private readonly object _lock = new object();

    /// <summary>
    /// Initializes a new instance of the TraceMetricsCollector class.
    /// </summary>
    public TraceMetricsCollector(ILogger<TraceMetricsCollector> logger)
    {
        _logger = logger;
        _metrics = new Dictionary<string, double>();
        _counters = new Dictionary<string, long>();
        _histograms = new Dictionary<string, List<double>>();
    }

    /// <inheritdoc />
    public void RecordMetric(string metricName, double value, IDictionary<string, object>? tags = null)
    {
        try
        {
            lock (_lock)
            {
                _metrics[metricName] = value;
            }

            _logger.LogDebug("Recorded metric: {MetricName} = {Value}", metricName, value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording metric: {MetricName}", metricName);
        }
    }

    /// <inheritdoc />
    public void RecordCounter(string counterName, long increment = 1, IDictionary<string, object>? tags = null)
    {
        try
        {
            lock (_lock)
            {
                if (_counters.TryGetValue(counterName, out var currentValue))
                {
                    _counters[counterName] = currentValue + increment;
                }
                else
                {
                    _counters[counterName] = increment;
                }
            }

            _logger.LogDebug("Recorded counter: {CounterName} += {Increment}", counterName, increment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording counter: {CounterName}", counterName);
        }
    }

    /// <inheritdoc />
    public void RecordHistogram(string histogramName, double value, IDictionary<string, object>? tags = null)
    {
        try
        {
            lock (_lock)
            {
                if (!_histograms.TryGetValue(histogramName, out var values))
                {
                    values = new List<double>();
                    _histograms[histogramName] = values;
                }

                values.Add(value);

                // Keep only the last 1000 values to prevent memory issues
                if (values.Count > 1000)
                {
                    values.RemoveAt(0);
                }
            }

            _logger.LogDebug("Recorded histogram: {HistogramName} = {Value}", histogramName, value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording histogram: {HistogramName}", histogramName);
        }
    }

    /// <inheritdoc />
    public TraceMetrics GetMetrics()
    {
        lock (_lock)
        {
            var histogramStats = new Dictionary<string, HistogramStatistics>();
            foreach (var kvp in _histograms)
            {
                var values = kvp.Value;
                if (values.Count > 0)
                {
                    histogramStats[kvp.Key] = new HistogramStatistics
                    {
                        Count = values.Count,
                        Min = values.Min(),
                        Max = values.Max(),
                        Mean = values.Average(),
                        P50 = CalculatePercentile(values, 0.5),
                        P95 = CalculatePercentile(values, 0.95),
                        P99 = CalculatePercentile(values, 0.99)
                    };
                }
            }

            return new TraceMetrics
            {
                Metrics = new Dictionary<string, double>(_metrics),
                Counters = new Dictionary<string, long>(_counters),
                Histograms = histogramStats,
                LastUpdated = DateTime.UtcNow
            };
        }
    }

    private double CalculatePercentile(List<double> values, double percentile)
    {
        if (values.Count == 0)
        {
            return 0;
        }

        var sortedValues = values.OrderBy(x => x).ToList();
        var index = (int)Math.Ceiling(percentile * sortedValues.Count) - 1;
        return sortedValues[Math.Max(0, Math.Min(index, sortedValues.Count - 1))];
    }
}

/// <summary>
/// Trace metrics data.
/// </summary>
public sealed class TraceMetrics
{
    /// <summary>
    /// Gets or sets the metrics.
    /// </summary>
    public IDictionary<string, double> Metrics { get; set; } = new Dictionary<string, double>();

    /// <summary>
    /// Gets or sets the counters.
    /// </summary>
    public IDictionary<string, long> Counters { get; set; } = new Dictionary<string, long>();

    /// <summary>
    /// Gets or sets the histogram statistics.
    /// </summary>
    public IDictionary<string, HistogramStatistics> Histograms { get; set; } = new Dictionary<string, HistogramStatistics>();

    /// <summary>
    /// Gets or sets the last updated timestamp.
    /// </summary>
    public DateTime LastUpdated { get; set; }
}

/// <summary>
/// Histogram statistics.
/// </summary>
public sealed class HistogramStatistics
{
    /// <summary>
    /// Gets or sets the count of values.
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Gets or sets the minimum value.
    /// </summary>
    public double Min { get; set; }

    /// <summary>
    /// Gets or sets the maximum value.
    /// </summary>
    public double Max { get; set; }

    /// <summary>
    /// Gets or sets the mean value.
    /// </summary>
    public double Mean { get; set; }

    /// <summary>
    /// Gets or sets the 50th percentile.
    /// </summary>
    public double P50 { get; set; }

    /// <summary>
    /// Gets or sets the 95th percentile.
    /// </summary>
    public double P95 { get; set; }

    /// <summary>
    /// Gets or sets the 99th percentile.
    /// </summary>
    public double P99 { get; set; }
}