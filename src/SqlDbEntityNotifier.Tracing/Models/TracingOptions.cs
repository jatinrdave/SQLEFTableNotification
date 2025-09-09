namespace SqlDbEntityNotifier.Tracing.Models;

/// <summary>
/// Configuration options for OpenTelemetry tracing.
/// </summary>
public sealed class TracingOptions
{
    /// <summary>
    /// Gets or sets whether tracing is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the service name for tracing.
    /// </summary>
    public string ServiceName { get; set; } = "sqldb-notifier";

    /// <summary>
    /// Gets or sets the service version.
    /// </summary>
    public string ServiceVersion { get; set; } = "1.0.0";

    /// <summary>
    /// Gets or sets the sampling configuration.
    /// </summary>
    public SamplingOptions Sampling { get; set; } = new();

    /// <summary>
    /// Gets or sets the exporter configuration.
    /// </summary>
    public ExporterOptions Exporter { get; set; } = new();

    /// <summary>
    /// Gets or sets the instrumentation configuration.
    /// </summary>
    public InstrumentationOptions Instrumentation { get; set; } = new();

    /// <summary>
    /// Gets or sets additional resource attributes.
    /// </summary>
    public IDictionary<string, string> ResourceAttributes { get; set; } = new Dictionary<string, string>();
}

/// <summary>
/// Sampling configuration for tracing.
/// </summary>
public sealed class SamplingOptions
{
    /// <summary>
    /// Gets or sets the sampling type.
    /// </summary>
    public SamplingType Type { get; set; } = SamplingType.AlwaysOn;

    /// <summary>
    /// Gets or sets the sampling ratio (for probabilistic sampling).
    /// </summary>
    public double Ratio { get; set; } = 1.0;

    /// <summary>
    /// Gets or sets the parent sampling behavior.
    /// </summary>
    public ParentSamplingBehavior ParentSamplingBehavior { get; set; } = ParentSamplingBehavior.ParentBased;
}

/// <summary>
/// Exporter configuration for tracing.
/// </summary>
public sealed class ExporterOptions
{
    /// <summary>
    /// Gets or sets the exporter type.
    /// </summary>
    public ExporterType Type { get; set; } = ExporterType.Console;

    /// <summary>
    /// Gets or sets the Jaeger configuration.
    /// </summary>
    public JaegerOptions Jaeger { get; set; } = new();

    /// <summary>
    /// Gets or sets the Zipkin configuration.
    /// </summary>
    public ZipkinOptions Zipkin { get; set; } = new();

    /// <summary>
    /// Gets or sets the OTLP configuration.
    /// </summary>
    public OtlpOptions Otlp { get; set; } = new();
}

/// <summary>
/// Instrumentation configuration for tracing.
/// </summary>
public sealed class InstrumentationOptions
{
    /// <summary>
    /// Gets or sets whether to enable HTTP instrumentation.
    /// </summary>
    public bool EnableHttp { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable SQL instrumentation.
    /// </summary>
    public bool EnableSql { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable custom instrumentation.
    /// </summary>
    public bool EnableCustom { get; set; } = true;

    /// <summary>
    /// Gets or sets the list of HTTP request headers to capture.
    /// </summary>
    public IList<string> HttpRequestHeaders { get; set; } = new List<string>
    {
        "User-Agent", "Content-Type", "Authorization"
    };

    /// <summary>
    /// Gets or sets the list of HTTP response headers to capture.
    /// </summary>
    public IList<string> HttpResponseHeaders { get; set; } = new List<string>
    {
        "Content-Type", "Content-Length"
    };
}

/// <summary>
/// Jaeger exporter configuration.
/// </summary>
public sealed class JaegerOptions
{
    /// <summary>
    /// Gets or sets the Jaeger agent host.
    /// </summary>
    public string AgentHost { get; set; } = "localhost";

    /// <summary>
    /// Gets or sets the Jaeger agent port.
    /// </summary>
    public int AgentPort { get; set; } = 6831;

    /// <summary>
    /// Gets or sets the Jaeger endpoint URL.
    /// </summary>
    public string? Endpoint { get; set; }

    /// <summary>
    /// Gets or sets the protocol to use.
    /// </summary>
    public JaegerProtocol Protocol { get; set; } = JaegerProtocol.UdpCompactThrift;
}

/// <summary>
/// Zipkin exporter configuration.
/// </summary>
public sealed class ZipkinOptions
{
    /// <summary>
    /// Gets or sets the Zipkin endpoint URL.
    /// </summary>
    public string Endpoint { get; set; } = "http://localhost:9411/api/v2/spans";

    /// <summary>
    /// Gets or sets the maximum batch size.
    /// </summary>
    public int MaxBatchSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets the export timeout in seconds.
    /// </summary>
    public int ExportTimeoutSeconds { get; set; } = 30;
}

/// <summary>
/// OTLP exporter configuration.
/// </summary>
public sealed class OtlpOptions
{
    /// <summary>
    /// Gets or sets the OTLP endpoint URL.
    /// </summary>
    public string Endpoint { get; set; } = "http://localhost:4317";

    /// <summary>
    /// Gets or sets the OTLP protocol.
    /// </summary>
    public OtlpProtocol Protocol { get; set; } = OtlpProtocol.Grpc;

    /// <summary>
    /// Gets or sets the headers to include in requests.
    /// </summary>
    public IDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
}

/// <summary>
/// Sampling types for tracing.
/// </summary>
public enum SamplingType
{
    /// <summary>
    /// Always sample traces.
    /// </summary>
    AlwaysOn,

    /// <summary>
    /// Never sample traces.
    /// </summary>
    AlwaysOff,

    /// <summary>
    /// Probabilistic sampling based on ratio.
    /// </summary>
    Probabilistic,

    /// <summary>
    /// Trace ID ratio based sampling.
    /// </summary>
    TraceIdRatioBased
}

/// <summary>
/// Parent sampling behaviors.
/// </summary>
public enum ParentSamplingBehavior
{
    /// <summary>
    /// Use parent-based sampling.
    /// </summary>
    ParentBased,

    /// <summary>
    /// Ignore parent sampling decision.
    /// </summary>
    IgnoreParent
}

/// <summary>
/// Exporter types for tracing.
/// </summary>
public enum ExporterType
{
    /// <summary>
    /// Console exporter.
    /// </summary>
    Console,

    /// <summary>
    /// Jaeger exporter.
    /// </summary>
    Jaeger,

    /// <summary>
    /// Zipkin exporter.
    /// </summary>
    Zipkin,

    /// <summary>
    /// OTLP exporter.
    /// </summary>
    Otlp
}

/// <summary>
/// Jaeger protocols.
/// </summary>
public enum JaegerProtocol
{
    /// <summary>
    /// UDP compact thrift protocol.
    /// </summary>
    UdpCompactThrift,

    /// <summary>
    /// HTTP thrift protocol.
    /// </summary>
    HttpThrift
}

/// <summary>
/// OTLP protocols.
/// </summary>
public enum OtlpProtocol
{
    /// <summary>
    /// gRPC protocol.
    /// </summary>
    Grpc,

    /// <summary>
    /// HTTP protocol.
    /// </summary>
    HttpProtobuf
}