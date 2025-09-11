using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SqlDbEntityNotifier.Core.Interfaces;
using SqlDbEntityNotifier.Core.MultiTenant.Models;
using SqlDbEntityNotifier.Core.Throttling;
using SqlDbEntityNotifier.Core.Transactional.Models;
using SqlDbEntityNotifier.Monitoring.Dashboard.Models;
using System.Text.Json;

namespace SqlDbEntityNotifier.Monitoring.Dashboard.Services;

/// <summary>
/// Service for managing the monitoring dashboard.
/// </summary>
public class DashboardService
{
    private readonly ILogger<DashboardService> _logger;
    private readonly DashboardOptions _options;
    private readonly IChangeEventMetrics _metrics;
    private readonly IHealthCheckService _healthCheckService;
    private readonly ITenantManager? _tenantManager;
    private readonly IThrottlingManager? _throttlingManager;
    private readonly ITransactionalGroupManager? _transactionalGroupManager;
    private readonly Dictionary<string, DashboardData> _dashboardCache;
    private readonly Timer _refreshTimer;

    /// <summary>
    /// Initializes a new instance of the DashboardService class.
    /// </summary>
    public DashboardService(
        ILogger<DashboardService> logger,
        IOptions<DashboardOptions> options,
        IChangeEventMetrics metrics,
        IHealthCheckService healthCheckService,
        ITenantManager? tenantManager = null,
        IThrottlingManager? throttlingManager = null,
        ITransactionalGroupManager? transactionalGroupManager = null)
    {
        _logger = logger;
        _options = options.Value;
        _metrics = metrics;
        _healthCheckService = healthCheckService;
        _tenantManager = tenantManager;
        _throttlingManager = throttlingManager;
        _transactionalGroupManager = transactionalGroupManager;
        _dashboardCache = new Dictionary<string, DashboardData>();

        // Start refresh timer
        _refreshTimer = new Timer(RefreshDashboardData, null, TimeSpan.Zero, TimeSpan.FromSeconds(_options.RefreshIntervalSeconds));
    }

    /// <summary>
    /// Gets the dashboard data.
    /// </summary>
    /// <param name="tenantId">The tenant ID (optional).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The dashboard data.</returns>
    public async Task<DashboardData> GetDashboardDataAsync(string? tenantId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = tenantId ?? "global";
            
            if (_dashboardCache.TryGetValue(cacheKey, out var cachedData) && 
                DateTime.UtcNow - cachedData.LastUpdated < TimeSpan.FromSeconds(_options.RefreshIntervalSeconds))
            {
                return cachedData;
            }

            var dashboardData = new DashboardData
            {
                TenantId = tenantId,
                LastUpdated = DateTime.UtcNow,
                Metrics = await GetMetricsDataAsync(tenantId, cancellationToken),
                HealthStatus = await GetHealthStatusAsync(cancellationToken),
                Alerts = await GetAlertsAsync(tenantId, cancellationToken),
                Performance = await GetPerformanceDataAsync(tenantId, cancellationToken),
                TenantOverview = await GetTenantOverviewAsync(cancellationToken),
                DatabaseHealth = await GetDatabaseHealthAsync(cancellationToken),
                DeliveryStatus = await GetDeliveryStatusAsync(tenantId, cancellationToken),
                ThrottlingStatus = await GetThrottlingStatusAsync(tenantId, cancellationToken)
            };

            _dashboardCache[cacheKey] = dashboardData;
            return dashboardData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard data for tenant: {TenantId}", tenantId);
            throw;
        }
    }

    /// <summary>
    /// Gets the metrics data for the dashboard.
    /// </summary>
    /// <param name="tenantId">The tenant ID (optional).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The metrics data.</returns>
    public async Task<MetricsData> GetMetricsDataAsync(string? tenantId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var metrics = await _metrics.GetMetricsAsync(cancellationToken);
            
            return new MetricsData
            {
                EventsPerSecond = metrics.EventsPerSecond,
                TotalEvents = metrics.TotalEvents,
                ErrorRate = metrics.ErrorRate,
                AverageLatency = metrics.AverageLatency,
                ActiveConnections = metrics.ActiveConnections,
                ActiveSubscriptions = metrics.ActiveSubscriptions,
                MemoryUsage = metrics.MemoryUsage,
                CpuUsage = metrics.CpuUsage,
                LastUpdated = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting metrics data for tenant: {TenantId}", tenantId);
            throw;
        }
    }

    /// <summary>
    /// Gets the health status for the dashboard.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The health status.</returns>
    public async Task<HealthStatusData> GetHealthStatusAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var healthChecks = await _healthCheckService.GetHealthChecksAsync(cancellationToken);
            
            return new HealthStatusData
            {
                OverallStatus = healthChecks.OverallStatus,
                DatabaseStatus = healthChecks.DatabaseStatus,
                PublisherStatus = healthChecks.PublisherStatus,
                AdapterStatus = healthChecks.AdapterStatus,
                LastUpdated = DateTime.UtcNow,
                Details = healthChecks.Details
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting health status");
            throw;
        }
    }

    /// <summary>
    /// Gets the alerts for the dashboard.
    /// </summary>
    /// <param name="tenantId">The tenant ID (optional).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The alerts data.</returns>
    public async Task<AlertsData> GetAlertsAsync(string? tenantId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            // This is a simplified implementation
            // In a real implementation, you would query an alerting system
            var alerts = new List<AlertData>
            {
                new AlertData
                {
                    Id = "alert-1",
                    Title = "High Error Rate",
                    Message = "Error rate has exceeded 5%",
                    Severity = AlertSeverity.High,
                    Timestamp = DateTime.UtcNow.AddMinutes(-5),
                    Source = "metrics-monitor",
                    TenantId = tenantId
                },
                new AlertData
                {
                    Id = "alert-2",
                    Title = "Database Connection Issue",
                    Message = "Database connection pool is at 90% capacity",
                    Severity = AlertSeverity.Medium,
                    Timestamp = DateTime.UtcNow.AddMinutes(-10),
                    Source = "health-checker",
                    TenantId = tenantId
                }
            };

            return new AlertsData
            {
                Alerts = alerts,
                TotalCount = alerts.Count,
                CriticalCount = alerts.Count(a => a.Severity == AlertSeverity.Critical),
                HighCount = alerts.Count(a => a.Severity == AlertSeverity.High),
                MediumCount = alerts.Count(a => a.Severity == AlertSeverity.Medium),
                LowCount = alerts.Count(a => a.Severity == AlertSeverity.Low),
                LastUpdated = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting alerts for tenant: {TenantId}", tenantId);
            throw;
        }
    }

    /// <summary>
    /// Gets the performance data for the dashboard.
    /// </summary>
    /// <param name="tenantId">The tenant ID (optional).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The performance data.</returns>
    public async Task<PerformanceData> GetPerformanceDataAsync(string? tenantId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            // This is a simplified implementation
            // In a real implementation, you would query performance metrics
            return new PerformanceData
            {
                CpuUsage = 45.2,
                MemoryUsage = 67.8,
                DiskUsage = 23.1,
                NetworkUsage = 12.5,
                GcPressure = 15.3,
                ThreadCount = 45,
                LastUpdated = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting performance data for tenant: {TenantId}", tenantId);
            throw;
        }
    }

    /// <summary>
    /// Gets the tenant overview data for the dashboard.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The tenant overview data.</returns>
    public async Task<TenantOverviewData> GetTenantOverviewAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_tenantManager == null)
            {
                return new TenantOverviewData
                {
                    TotalTenants = 0,
                    ActiveTenants = 0,
                    InactiveTenants = 0,
                    LastUpdated = DateTime.UtcNow
                };
            }

            var tenants = await _tenantManager.GetAllTenantsAsync(cancellationToken);
            var activeTenants = tenants.Count(t => t.IsActive);

            return new TenantOverviewData
            {
                TotalTenants = tenants.Count,
                ActiveTenants = activeTenants,
                InactiveTenants = tenants.Count - activeTenants,
                LastUpdated = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tenant overview");
            throw;
        }
    }

    /// <summary>
    /// Gets the database health data for the dashboard.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The database health data.</returns>
    public async Task<DatabaseHealthData> GetDatabaseHealthAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // This is a simplified implementation
            // In a real implementation, you would query database health metrics
            return new DatabaseHealthData
            {
                ConnectionCount = 25,
                ReplicationLag = 150,
                BinaryLogSize = 1024,
                HealthStatus = "Healthy",
                LastUpdated = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting database health");
            throw;
        }
    }

    /// <summary>
    /// Gets the delivery status data for the dashboard.
    /// </summary>
    /// <param name="tenantId">The tenant ID (optional).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The delivery status data.</returns>
    public async Task<DeliveryStatusData> GetDeliveryStatusAsync(string? tenantId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            // This is a simplified implementation
            // In a real implementation, you would query delivery metrics
            return new DeliveryStatusData
            {
                SuccessRate = 99.5,
                AverageLatency = 45.2,
                FailedDeliveries = 12,
                RetryAttempts = 8,
                LastUpdated = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting delivery status for tenant: {TenantId}", tenantId);
            throw;
        }
    }

    /// <summary>
    /// Gets the throttling status data for the dashboard.
    /// </summary>
    /// <param name="tenantId">The tenant ID (optional).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The throttling status data.</returns>
    public async Task<ThrottlingStatusData> GetThrottlingStatusAsync(string? tenantId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_throttlingManager == null)
            {
                return new ThrottlingStatusData
                {
                    ThrottledRequests = 0,
                    ThrottlingRate = 0.0,
                    ActiveThrottlers = 0,
                    ThrottlingViolations = 0,
                    LastUpdated = DateTime.UtcNow
                };
            }

            var statistics = await _throttlingManager.GetThrottlingStatisticsAsync(cancellationToken);

            return new ThrottlingStatusData
            {
                ThrottledRequests = statistics.TotalThrottledRequests,
                ThrottlingRate = statistics.TotalTenants > 0 ? (double)statistics.ThrottledTenants / statistics.TotalTenants : 0.0,
                ActiveThrottlers = statistics.ActiveTenants,
                ThrottlingViolations = statistics.ThrottledTenants,
                LastUpdated = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting throttling status for tenant: {TenantId}", tenantId);
            throw;
        }
    }

    /// <summary>
    /// Exports dashboard data in the specified format.
    /// </summary>
    /// <param name="format">The export format.</param>
    /// <param name="tenantId">The tenant ID (optional).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The exported data.</returns>
    public async Task<byte[]> ExportDashboardDataAsync(ExportFormat format, string? tenantId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var dashboardData = await GetDashboardDataAsync(tenantId, cancellationToken);

            return format switch
            {
                ExportFormat.Json => System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(dashboardData, new JsonSerializerOptions { WriteIndented = true })),
                ExportFormat.Csv => await ExportToCsvAsync(dashboardData, cancellationToken),
                ExportFormat.Pdf => await ExportToPdfAsync(dashboardData, cancellationToken),
                _ => throw new NotSupportedException($"Export format {format} is not supported")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting dashboard data in format: {Format}", format);
            throw;
        }
    }

    private async Task<byte[]> ExportToCsvAsync(DashboardData data, CancellationToken cancellationToken)
    {
        // Simplified CSV export implementation
        var csv = $"Timestamp,EventsPerSecond,TotalEvents,ErrorRate,AverageLatency\n";
        csv += $"{data.LastUpdated:yyyy-MM-dd HH:mm:ss},{data.Metrics.EventsPerSecond},{data.Metrics.TotalEvents},{data.Metrics.ErrorRate},{data.Metrics.AverageLatency}\n";
        
        return System.Text.Encoding.UTF8.GetBytes(csv);
    }

    private async Task<byte[]> ExportToPdfAsync(DashboardData data, CancellationToken cancellationToken)
    {
        // Simplified PDF export implementation
        // In a real implementation, you would use a PDF library like iTextSharp
        var pdfContent = $"Dashboard Report\nGenerated: {data.LastUpdated:yyyy-MM-dd HH:mm:ss}\n\n";
        pdfContent += $"Events Per Second: {data.Metrics.EventsPerSecond}\n";
        pdfContent += $"Total Events: {data.Metrics.TotalEvents}\n";
        pdfContent += $"Error Rate: {data.Metrics.ErrorRate}%\n";
        pdfContent += $"Average Latency: {data.Metrics.AverageLatency}ms\n";
        
        return System.Text.Encoding.UTF8.GetBytes(pdfContent);
    }

    private void RefreshDashboardData(object? state)
    {
        try
        {
            // Clear cache to force refresh on next request
            _dashboardCache.Clear();
            _logger.LogDebug("Dashboard cache cleared for refresh");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing dashboard data");
        }
    }

    /// <summary>
    /// Disposes the dashboard service.
    /// </summary>
    public void Dispose()
    {
        _refreshTimer?.Dispose();
    }
}

// Data models for the dashboard
public sealed class DashboardData
{
    public string? TenantId { get; set; }
    public DateTime LastUpdated { get; set; }
    public MetricsData Metrics { get; set; } = new();
    public HealthStatusData HealthStatus { get; set; } = new();
    public AlertsData Alerts { get; set; } = new();
    public PerformanceData Performance { get; set; } = new();
    public TenantOverviewData TenantOverview { get; set; } = new();
    public DatabaseHealthData DatabaseHealth { get; set; } = new();
    public DeliveryStatusData DeliveryStatus { get; set; } = new();
    public ThrottlingStatusData ThrottlingStatus { get; set; } = new();
}

public sealed class MetricsData
{
    public int EventsPerSecond { get; set; }
    public long TotalEvents { get; set; }
    public double ErrorRate { get; set; }
    public double AverageLatency { get; set; }
    public int ActiveConnections { get; set; }
    public int ActiveSubscriptions { get; set; }
    public long MemoryUsage { get; set; }
    public double CpuUsage { get; set; }
    public DateTime LastUpdated { get; set; }
}

public sealed class HealthStatusData
{
    public string OverallStatus { get; set; } = string.Empty;
    public string DatabaseStatus { get; set; } = string.Empty;
    public string PublisherStatus { get; set; } = string.Empty;
    public string AdapterStatus { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; }
    public IDictionary<string, string> Details { get; set; } = new Dictionary<string, string>();
}

public sealed class AlertsData
{
    public IList<AlertData> Alerts { get; set; } = new List<AlertData>();
    public int TotalCount { get; set; }
    public int CriticalCount { get; set; }
    public int HighCount { get; set; }
    public int MediumCount { get; set; }
    public int LowCount { get; set; }
    public DateTime LastUpdated { get; set; }
}

public sealed class AlertData
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public AlertSeverity Severity { get; set; }
    public DateTime Timestamp { get; set; }
    public string Source { get; set; } = string.Empty;
    public string? TenantId { get; set; }
}

public sealed class PerformanceData
{
    public double CpuUsage { get; set; }
    public double MemoryUsage { get; set; }
    public double DiskUsage { get; set; }
    public double NetworkUsage { get; set; }
    public double GcPressure { get; set; }
    public int ThreadCount { get; set; }
    public DateTime LastUpdated { get; set; }
}

public sealed class TenantOverviewData
{
    public int TotalTenants { get; set; }
    public int ActiveTenants { get; set; }
    public int InactiveTenants { get; set; }
    public DateTime LastUpdated { get; set; }
}

public sealed class DatabaseHealthData
{
    public int ConnectionCount { get; set; }
    public long ReplicationLag { get; set; }
    public long BinaryLogSize { get; set; }
    public string HealthStatus { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; }
}

public sealed class DeliveryStatusData
{
    public double SuccessRate { get; set; }
    public double AverageLatency { get; set; }
    public int FailedDeliveries { get; set; }
    public int RetryAttempts { get; set; }
    public DateTime LastUpdated { get; set; }
}

public sealed class ThrottlingStatusData
{
    public long ThrottledRequests { get; set; }
    public double ThrottlingRate { get; set; }
    public int ActiveThrottlers { get; set; }
    public int ThrottlingViolations { get; set; }
    public DateTime LastUpdated { get; set; }
}