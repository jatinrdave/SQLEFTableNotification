using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Trace;
using SqlDbEntityNotifier.Core.Models;
using SqlDbEntityNotifier.Tracing.Models;

namespace SqlDbEntityNotifier.Tracing;

/// <summary>
/// Tracer for change events with OpenTelemetry integration.
/// </summary>
public class ChangeEventTracer
{
    private readonly ILogger<ChangeEventTracer> _logger;
    private readonly TracingOptions _options;
    private readonly Tracer _tracer;
    private readonly ActivitySource _activitySource;

    /// <summary>
    /// Initializes a new instance of the ChangeEventTracer class.
    /// </summary>
    public ChangeEventTracer(
        ILogger<ChangeEventTracer> logger,
        IOptions<TracingOptions> options,
        TracerProvider tracerProvider)
    {
        _logger = logger;
        _options = options.Value;
        _tracer = tracerProvider.GetTracer(_options.ServiceName, _options.ServiceVersion);
        _activitySource = new ActivitySource(_options.ServiceName, _options.ServiceVersion);
    }

    /// <summary>
    /// Creates a trace for processing a change event.
    /// </summary>
    /// <param name="changeEvent">The change event to trace.</param>
    /// <param name="operationName">The operation name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A disposable trace scope.</returns>
    public TraceScope StartChangeEventTrace(ChangeEvent changeEvent, string operationName, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return new TraceScope(null, null);
        }

        try
        {
            var activity = _activitySource.StartActivity(operationName);
            if (activity == null)
            {
                return new TraceScope(null, null);
            }

            // Set activity attributes
            activity.SetTag("sqldb.source", changeEvent.Source);
            activity.SetTag("sqldb.schema", changeEvent.Schema);
            activity.SetTag("sqldb.table", changeEvent.Table);
            activity.SetTag("sqldb.operation", changeEvent.Operation);
            activity.SetTag("sqldb.offset", changeEvent.Offset);
            activity.SetTag("sqldb.timestamp", changeEvent.TimestampUtc.ToString("O"));

            // Add metadata as tags
            foreach (var metadata in changeEvent.Metadata)
            {
                activity.SetTag($"sqldb.metadata.{metadata.Key}", metadata.Value);
            }

            // Set span kind
            activity.SetTag("span.kind", "internal");

            return new TraceScope(activity, _tracer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting change event trace");
            return new TraceScope(null, null);
        }
    }

    /// <summary>
    /// Creates a trace for publishing a change event.
    /// </summary>
    /// <param name="changeEvent">The change event to trace.</param>
    /// <param name="publisherName">The publisher name.</param>
    /// <param name="destination">The destination.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A disposable trace scope.</returns>
    public TraceScope StartPublishTrace(ChangeEvent changeEvent, string publisherName, string destination, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return new TraceScope(null, null);
        }

        try
        {
            var activity = _activitySource.StartActivity($"publish.{publisherName}");
            if (activity == null)
            {
                return new TraceScope(null, null);
            }

            // Set activity attributes
            activity.SetTag("sqldb.source", changeEvent.Source);
            activity.SetTag("sqldb.schema", changeEvent.Schema);
            activity.SetTag("sqldb.table", changeEvent.Table);
            activity.SetTag("sqldb.operation", changeEvent.Operation);
            activity.SetTag("sqldb.offset", changeEvent.Offset);
            activity.SetTag("sqldb.publisher", publisherName);
            activity.SetTag("sqldb.destination", destination);
            activity.SetTag("sqldb.timestamp", changeEvent.TimestampUtc.ToString("O"));

            // Set span kind
            activity.SetTag("span.kind", "producer");

            return new TraceScope(activity, _tracer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting publish trace");
            return new TraceScope(null, null);
        }
    }

    /// <summary>
    /// Creates a trace for database operations.
    /// </summary>
    /// <param name="operationName">The operation name.</param>
    /// <param name="source">The database source.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A disposable trace scope.</returns>
    public TraceScope StartDatabaseTrace(string operationName, string source, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return new TraceScope(null, null);
        }

        try
        {
            var activity = _activitySource.StartActivity($"db.{operationName}");
            if (activity == null)
            {
                return new TraceScope(null, null);
            }

            // Set activity attributes
            activity.SetTag("sqldb.source", source);
            activity.SetTag("db.system", "sql");
            activity.SetTag("db.operation", operationName);

            // Set span kind
            activity.SetTag("span.kind", "client");

            return new TraceScope(activity, _tracer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting database trace");
            return new TraceScope(null, null);
        }
    }

    /// <summary>
    /// Records an event in the current trace.
    /// </summary>
    /// <param name="eventName">The event name.</param>
    /// <param name="attributes">The event attributes.</param>
    public void RecordEvent(string eventName, IDictionary<string, object>? attributes = null)
    {
        if (!_options.Enabled)
        {
            return;
        }

        try
        {
            var activity = Activity.Current;
            if (activity != null)
            {
                activity.AddEvent(new ActivityEvent(eventName, attributes: attributes));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording trace event: {EventName}", eventName);
        }
    }

    /// <summary>
    /// Sets a tag on the current trace.
    /// </summary>
    /// <param name="key">The tag key.</param>
    /// <param name="value">The tag value.</param>
    public void SetTag(string key, object value)
    {
        if (!_options.Enabled)
        {
            return;
        }

        try
        {
            var activity = Activity.Current;
            if (activity != null)
            {
                activity.SetTag(key, value);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting trace tag: {Key}", key);
        }
    }

    /// <summary>
    /// Records an exception in the current trace.
    /// </summary>
    /// <param name="exception">The exception to record.</param>
    public void RecordException(Exception exception)
    {
        if (!_options.Enabled)
        {
            return;
        }

        try
        {
            var activity = Activity.Current;
            if (activity != null)
            {
                activity.SetStatus(ActivityStatusCode.Error, exception.Message);
                activity.RecordException(exception);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording exception in trace");
        }
    }

    /// <summary>
    /// Extracts trace context from headers.
    /// </summary>
    /// <param name="headers">The headers containing trace context.</param>
    /// <param name="getHeader">Function to get header value.</param>
    /// <returns>The extracted trace context.</returns>
    public ActivityContext? ExtractTraceContext(IDictionary<string, string> headers, Func<string, string?> getHeader)
    {
        if (!_options.Enabled)
        {
            return null;
        }

        try
        {
            var propagator = new TraceContextPropagator();
            var carrier = new DictionaryCarrier(headers, getHeader);
            var parentContext = propagator.Extract(default, carrier, ExtractTraceContextFromCarrier);
            
            return parentContext.ActivityContext;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting trace context from headers");
            return null;
        }
    }

    /// <summary>
    /// Injects trace context into headers.
    /// </summary>
    /// <param name="headers">The headers to inject trace context into.</param>
    /// <param name="setHeader">Function to set header value.</param>
    public void InjectTraceContext(IDictionary<string, string> headers, Action<string, string> setHeader)
    {
        if (!_options.Enabled)
        {
            return;
        }

        try
        {
            var activity = Activity.Current;
            if (activity != null)
            {
                var propagator = new TraceContextPropagator();
                var carrier = new DictionaryCarrier(headers, setHeader);
                propagator.Inject(new PropagationContext(activity.Context, Baggage.Current), carrier, InjectTraceContextIntoCarrier);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error injecting trace context into headers");
        }
    }

    private static IEnumerable<string> ExtractTraceContextFromCarrier(IDictionary<string, string> carrier, string key)
    {
        if (carrier.TryGetValue(key, out var value))
        {
            return new[] { value };
        }
        return Enumerable.Empty<string>();
    }

    private static void InjectTraceContextIntoCarrier(IDictionary<string, string> carrier, string key, string value)
    {
        carrier[key] = value;
    }

    /// <summary>
    /// Disposes the tracer resources.
    /// </summary>
    public void Dispose()
    {
        _activitySource?.Dispose();
    }
}

/// <summary>
/// Represents a trace scope that can be disposed.
/// </summary>
public sealed class TraceScope : IDisposable
{
    private readonly Activity? _activity;
    private readonly Tracer? _tracer;

    /// <summary>
    /// Initializes a new instance of the TraceScope class.
    /// </summary>
    public TraceScope(Activity? activity, Tracer? tracer)
    {
        _activity = activity;
        _tracer = tracer;
    }

    /// <summary>
    /// Gets the current activity.
    /// </summary>
    public Activity? Activity => _activity;

    /// <summary>
    /// Gets the current tracer.
    /// </summary>
    public Tracer? Tracer => _tracer;

    /// <summary>
    /// Disposes the trace scope.
    /// </summary>
    public void Dispose()
    {
        _activity?.Dispose();
    }
}

/// <summary>
/// Dictionary-based carrier for trace context propagation.
/// </summary>
public sealed class DictionaryCarrier
{
    private readonly IDictionary<string, string> _headers;
    private readonly Func<string, string?>? _getHeader;
    private readonly Action<string, string>? _setHeader;

    /// <summary>
    /// Initializes a new instance of the DictionaryCarrier class.
    /// </summary>
    public DictionaryCarrier(IDictionary<string, string> headers, Func<string, string?> getHeader)
    {
        _headers = headers;
        _getHeader = getHeader;
    }

    /// <summary>
    /// Initializes a new instance of the DictionaryCarrier class.
    /// </summary>
    public DictionaryCarrier(IDictionary<string, string> headers, Action<string, string> setHeader)
    {
        _headers = headers;
        _setHeader = setHeader;
    }

    /// <summary>
    /// Gets a header value.
    /// </summary>
    public string? Get(string key)
    {
        return _getHeader?.Invoke(key) ?? _headers.TryGetValue(key, out var value) ? value : null;
    }

    /// <summary>
    /// Sets a header value.
    /// </summary>
    public void Set(string key, string value)
    {
        _setHeader?.Invoke(key, value);
        _headers[key] = value;
    }
}