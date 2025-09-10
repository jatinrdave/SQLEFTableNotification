namespace SqlDbEntityNotifier.Monitoring.Dashboard.Models;

/// <summary>
/// Configuration options for the monitoring dashboard.
/// </summary>
public sealed class DashboardOptions
{
    /// <summary>
    /// Gets or sets the dashboard title.
    /// </summary>
    public string Title { get; set; } = "SQLDBEntityNotifier Monitoring Dashboard";

    /// <summary>
    /// Gets or sets the dashboard refresh interval in seconds.
    /// </summary>
    public int RefreshIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the metrics retention period in hours.
    /// </summary>
    public int MetricsRetentionHours { get; set; } = 24;

    /// <summary>
    /// Gets or sets the maximum number of data points to display.
    /// </summary>
    public int MaxDataPoints { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the dashboard theme.
    /// </summary>
    public DashboardTheme Theme { get; set; } = DashboardTheme.Light;

    /// <summary>
    /// Gets or sets the dashboard layout configuration.
    /// </summary>
    public DashboardLayoutOptions Layout { get; set; } = new();

    /// <summary>
    /// Gets or sets the dashboard widgets configuration.
    /// </summary>
    public DashboardWidgetsOptions Widgets { get; set; } = new();

    /// <summary>
    /// Gets or sets the dashboard alerts configuration.
    /// </summary>
    public DashboardAlertsOptions Alerts { get; set; } = new();

    /// <summary>
    /// Gets or sets the dashboard export configuration.
    /// </summary>
    public DashboardExportOptions Export { get; set; } = new();
}

/// <summary>
/// Dashboard layout configuration.
/// </summary>
public sealed class DashboardLayoutOptions
{
    /// <summary>
    /// Gets or sets the number of columns in the dashboard grid.
    /// </summary>
    public int Columns { get; set; } = 4;

    /// <summary>
    /// Gets or sets the number of rows in the dashboard grid.
    /// </summary>
    public int Rows { get; set; } = 6;

    /// <summary>
    /// Gets or sets the widget spacing in pixels.
    /// </summary>
    public int WidgetSpacing { get; set; } = 16;

    /// <summary>
    /// Gets or sets whether the dashboard is responsive.
    /// </summary>
    public bool Responsive { get; set; } = true;

    /// <summary>
    /// Gets or sets the default widget size.
    /// </summary>
    public WidgetSize DefaultWidgetSize { get; set; } = WidgetSize.Medium;
}

/// <summary>
/// Dashboard widgets configuration.
/// </summary>
public sealed class DashboardWidgetsOptions
{
    /// <summary>
    /// Gets or sets the metrics overview widget configuration.
    /// </summary>
    public MetricsOverviewWidgetOptions MetricsOverview { get; set; } = new();

    /// <summary>
    /// Gets or sets the events timeline widget configuration.
    /// </summary>
    public EventsTimelineWidgetOptions EventsTimeline { get; set; } = new();

    /// <summary>
    /// Gets or sets the performance metrics widget configuration.
    /// </summary>
    public PerformanceMetricsWidgetOptions PerformanceMetrics { get; set; } = new();

    /// <summary>
    /// Gets or sets the error tracking widget configuration.
    /// </summary>
    public ErrorTrackingWidgetOptions ErrorTracking { get; set; } = new();

    /// <summary>
    /// Gets or sets the tenant overview widget configuration.
    /// </summary>
    public TenantOverviewWidgetOptions TenantOverview { get; set; } = new();

    /// <summary>
    /// Gets or sets the database health widget configuration.
    /// </summary>
    public DatabaseHealthWidgetOptions DatabaseHealth { get; set; } = new();

    /// <summary>
    /// Gets or sets the delivery status widget configuration.
    /// </summary>
    public DeliveryStatusWidgetOptions DeliveryStatus { get; set; } = new();

    /// <summary>
    /// Gets or sets the throttling status widget configuration.
    /// </summary>
    public ThrottlingStatusWidgetOptions ThrottlingStatus { get; set; } = new();
}

/// <summary>
/// Dashboard alerts configuration.
/// </summary>
public sealed class DashboardAlertsOptions
{
    /// <summary>
    /// Gets or sets whether alerts are enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the alert refresh interval in seconds.
    /// </summary>
    public int RefreshIntervalSeconds { get; set; } = 10;

    /// <summary>
    /// Gets or sets the maximum number of alerts to display.
    /// </summary>
    public int MaxAlerts { get; set; } = 50;

    /// <summary>
    /// Gets or sets the alert severity levels to display.
    /// </summary>
    public IList<AlertSeverity> DisplaySeverities { get; set; } = new List<AlertSeverity>
    {
        AlertSeverity.Critical,
        AlertSeverity.High,
        AlertSeverity.Medium,
        AlertSeverity.Low
    };

    /// <summary>
    /// Gets or sets whether to show alert history.
    /// </summary>
    public bool ShowHistory { get; set; } = true;

    /// <summary>
    /// Gets or sets the alert history retention period in hours.
    /// </summary>
    public int HistoryRetentionHours { get; set; } = 168; // 7 days
}

/// <summary>
/// Dashboard export configuration.
/// </summary>
public sealed class DashboardExportOptions
{
    /// <summary>
    /// Gets or sets whether export functionality is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the supported export formats.
    /// </summary>
    public IList<ExportFormat> SupportedFormats { get; set; } = new List<ExportFormat>
    {
        ExportFormat.Json,
        ExportFormat.Csv,
        ExportFormat.Pdf
    };

    /// <summary>
    /// Gets or sets the maximum export data points.
    /// </summary>
    public int MaxDataPoints { get; set; } = 10000;

    /// <summary>
    /// Gets or sets the export timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 300; // 5 minutes
}

/// <summary>
/// Metrics overview widget configuration.
/// </summary>
public sealed class MetricsOverviewWidgetOptions
{
    /// <summary>
    /// Gets or sets whether the widget is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the widget position.
    /// </summary>
    public WidgetPosition Position { get; set; } = new() { Row = 0, Column = 0, Width = 2, Height = 2 };

    /// <summary>
    /// Gets or sets the metrics to display.
    /// </summary>
    public IList<string> Metrics { get; set; } = new List<string>
    {
        "events_per_second",
        "total_events",
        "error_rate",
        "latency_ms"
    };

    /// <summary>
    /// Gets or sets the time range for metrics.
    /// </summary>
    public TimeRange TimeRange { get; set; } = TimeRange.LastHour;
}

/// <summary>
/// Events timeline widget configuration.
/// </summary>
public sealed class EventsTimelineWidgetOptions
{
    /// <summary>
    /// Gets or sets whether the widget is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the widget position.
    /// </summary>
    public WidgetPosition Position { get; set; } = new() { Row = 0, Column = 2, Width = 2, Height = 3 };

    /// <summary>
    /// Gets or sets the chart type.
    /// </summary>
    public ChartType ChartType { get; set; } = ChartType.Line;

    /// <summary>
    /// Gets or sets the time range for the timeline.
    /// </summary>
    public TimeRange TimeRange { get; set; } = TimeRange.LastHour;

    /// <summary>
    /// Gets or sets the event types to display.
    /// </summary>
    public IList<string> EventTypes { get; set; } = new List<string>
    {
        "INSERT",
        "UPDATE",
        "DELETE",
        "BULK_INSERT",
        "BULK_UPDATE",
        "BULK_DELETE"
    };
}

/// <summary>
/// Performance metrics widget configuration.
/// </summary>
public sealed class PerformanceMetricsWidgetOptions
{
    /// <summary>
    /// Gets or sets whether the widget is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the widget position.
    /// </summary>
    public WidgetPosition Position { get; set; } = new() { Row = 2, Column = 0, Width = 2, Height = 2 };

    /// <summary>
    /// Gets or sets the performance metrics to display.
    /// </summary>
    public IList<string> Metrics { get; set; } = new List<string>
    {
        "cpu_usage",
        "memory_usage",
        "disk_usage",
        "network_usage"
    };

    /// <summary>
    /// Gets or sets the chart type.
    /// </summary>
    public ChartType ChartType { get; set; } = ChartType.Gauge;
}

/// <summary>
/// Error tracking widget configuration.
/// </summary>
public sealed class ErrorTrackingWidgetOptions
{
    /// <summary>
    /// Gets or sets whether the widget is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the widget position.
    /// </summary>
    public WidgetPosition Position { get; set; } = new() { Row = 2, Column = 2, Width = 2, Height = 2 };

    /// <summary>
    /// Gets or sets the error types to track.
    /// </summary>
    public IList<string> ErrorTypes { get; set; } = new List<string>
    {
        "connection_error",
        "delivery_error",
        "serialization_error",
        "validation_error"
    };

    /// <summary>
    /// Gets or sets the time range for error tracking.
    /// </summary>
    public TimeRange TimeRange { get; set; } = TimeRange.Last24Hours;
}

/// <summary>
/// Tenant overview widget configuration.
/// </summary>
public sealed class TenantOverviewWidgetOptions
{
    /// <summary>
    /// Gets or sets whether the widget is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the widget position.
    /// </summary>
    public WidgetPosition Position { get; set; } = new() { Row = 4, Column = 0, Width = 2, Height = 2 };

    /// <summary>
    /// Gets or sets the tenant metrics to display.
    /// </summary>
    public IList<string> Metrics { get; set; } = new List<string>
    {
        "active_tenants",
        "tenant_events_per_second",
        "tenant_error_rate",
        "tenant_resource_usage"
    };
}

/// <summary>
/// Database health widget configuration.
/// </summary>
public sealed class DatabaseHealthWidgetOptions
{
    /// <summary>
    /// Gets or sets whether the widget is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the widget position.
    /// </summary>
    public WidgetPosition Position { get; set; } = new() { Row = 4, Column = 2, Width = 2, Height = 2 };

    /// <summary>
    /// Gets or sets the database health metrics to display.
    /// </summary>
    public IList<string> Metrics { get; set; } = new List<string>
    {
        "connection_count",
        "replication_lag",
        "binary_log_size",
        "health_status"
    };
}

/// <summary>
/// Delivery status widget configuration.
/// </summary>
public sealed class DeliveryStatusWidgetOptions
{
    /// <summary>
    /// Gets or sets whether the widget is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the widget position.
    /// </summary>
    public WidgetPosition Position { get; set; } = new() { Row = 0, Column = 4, Width = 2, Height = 2 };

    /// <summary>
    /// Gets or sets the delivery status metrics to display.
    /// </summary>
    public IList<string> Metrics { get; set; } = new List<string>
    {
        "delivery_success_rate",
        "delivery_latency",
        "failed_deliveries",
        "retry_attempts"
    };
}

/// <summary>
/// Throttling status widget configuration.
/// </summary>
public sealed class ThrottlingStatusWidgetOptions
{
    /// <summary>
    /// Gets or sets whether the widget is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the widget position.
    /// </summary>
    public WidgetPosition Position { get; set; } = new() { Row = 2, Column = 4, Width = 2, Height = 2 };

    /// <summary>
    /// Gets or sets the throttling status metrics to display.
    /// </summary>
    public IList<string> Metrics { get; set; } = new List<string>
    {
        "throttled_requests",
        "throttling_rate",
        "active_throttlers",
        "throttling_violations"
    };
}

/// <summary>
/// Widget position configuration.
/// </summary>
public sealed class WidgetPosition
{
    /// <summary>
    /// Gets or sets the row position.
    /// </summary>
    public int Row { get; set; }

    /// <summary>
    /// Gets or sets the column position.
    /// </summary>
    public int Column { get; set; }

    /// <summary>
    /// Gets or sets the widget width.
    /// </summary>
    public int Width { get; set; } = 1;

    /// <summary>
    /// Gets or sets the widget height.
    /// </summary>
    public int Height { get; set; } = 1;
}

/// <summary>
/// Dashboard themes.
/// </summary>
public enum DashboardTheme
{
    /// <summary>
    /// Light theme.
    /// </summary>
    Light,

    /// <summary>
    /// Dark theme.
    /// </summary>
    Dark,

    /// <summary>
    /// Auto theme (follows system preference).
    /// </summary>
    Auto
}

/// <summary>
/// Widget sizes.
/// </summary>
public enum WidgetSize
{
    /// <summary>
    /// Small widget.
    /// </summary>
    Small,

    /// <summary>
    /// Medium widget.
    /// </summary>
    Medium,

    /// <summary>
    /// Large widget.
    /// </summary>
    Large,

    /// <summary>
    /// Extra large widget.
    /// </summary>
    ExtraLarge
}

/// <summary>
/// Chart types.
/// </summary>
public enum ChartType
{
    /// <summary>
    /// Line chart.
    /// </summary>
    Line,

    /// <summary>
    /// Bar chart.
    /// </summary>
    Bar,

    /// <summary>
    /// Pie chart.
    /// </summary>
    Pie,

    /// <summary>
    /// Gauge chart.
    /// </summary>
    Gauge,

    /// <summary>
    /// Area chart.
    /// </summary>
    Area,

    /// <summary>
    /// Scatter chart.
    /// </summary>
    Scatter
}

/// <summary>
/// Time ranges for dashboard data.
/// </summary>
public enum TimeRange
{
    /// <summary>
    /// Last 5 minutes.
    /// </summary>
    Last5Minutes,

    /// <summary>
    /// Last 15 minutes.
    /// </summary>
    Last15Minutes,

    /// <summary>
    /// Last hour.
    /// </summary>
    LastHour,

    /// <summary>
    /// Last 6 hours.
    /// </summary>
    Last6Hours,

    /// <summary>
    /// Last 24 hours.
    /// </summary>
    Last24Hours,

    /// <summary>
    /// Last 7 days.
    /// </summary>
    Last7Days,

    /// <summary>
    /// Last 30 days.
    /// </summary>
    Last30Days
}

/// <summary>
/// Alert severity levels.
/// </summary>
public enum AlertSeverity
{
    /// <summary>
    /// Critical severity.
    /// </summary>
    Critical,

    /// <summary>
    /// High severity.
    /// </summary>
    High,

    /// <summary>
    /// Medium severity.
    /// </summary>
    Medium,

    /// <summary>
    /// Low severity.
    /// </summary>
    Low,

    /// <summary>
    /// Info severity.
    /// </summary>
    Info
}

/// <summary>
/// Export formats.
/// </summary>
public enum ExportFormat
{
    /// <summary>
    /// JSON format.
    /// </summary>
    Json,

    /// <summary>
    /// CSV format.
    /// </summary>
    Csv,

    /// <summary>
    /// PDF format.
    /// </summary>
    Pdf,

    /// <summary>
    /// Excel format.
    /// </summary>
    Excel
}