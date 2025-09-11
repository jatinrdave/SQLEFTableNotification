using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SqlDbEntityNotifier.Core.Interfaces;
using SqlDbEntityNotifier.Core.Models;

namespace SqlDbEntityNotifier.Monitoring.Health;

/// <summary>
/// Health check for change event processing.
/// </summary>
public class ChangeEventHealthCheck : IHealthCheck
{
    private readonly ILogger<ChangeEventHealthCheck> _logger;
    private readonly IEntityNotifier _entityNotifier;
    private readonly ChangeEventHealthCheckOptions _options;

    /// <summary>
    /// Initializes a new instance of the ChangeEventHealthCheck class.
    /// </summary>
    public ChangeEventHealthCheck(
        ILogger<ChangeEventHealthCheck> logger,
        IEntityNotifier entityNotifier,
        IOptions<ChangeEventHealthCheckOptions> options)
    {
        _logger = logger;
        _entityNotifier = entityNotifier;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var status = await _entityNotifier.GetStatusAsync();
            var healthData = new Dictionary<string, object>
            {
                ["is_running"] = status.IsRunning,
                ["active_subscriptions"] = status.ActiveSubscriptions,
                ["events_processed"] = status.EventsProcessed,
                ["events_failed"] = status.EventsFailed,
                ["last_started"] = status.LastStartedUtc?.ToString("O") ?? "Never"
            };

            // Add lag information
            foreach (var lag in status.LagBySource)
            {
                healthData[$"lag_{lag.Key}"] = lag.Value;
            }

            // Determine health status
            var healthStatus = DetermineHealthStatus(status);

            return healthStatus switch
            {
                HealthStatus.Healthy => HealthCheckResult.Healthy("Change event processing is healthy", healthData),
                HealthStatus.Degraded => HealthCheckResult.Degraded("Change event processing is degraded", null, healthData),
                HealthStatus.Unhealthy => HealthCheckResult.Unhealthy("Change event processing is unhealthy", null, healthData),
                _ => HealthCheckResult.Healthy("Change event processing is healthy", healthData)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during health check");
            return HealthCheckResult.Unhealthy("Health check failed", ex);
        }
    }

    private HealthStatus DetermineHealthStatus(NotifierStatus status)
    {
        // Check if notifier is running
        if (!status.IsRunning)
        {
            return HealthStatus.Unhealthy;
        }

        // Check if there are active subscriptions
        if (status.ActiveSubscriptions == 0)
        {
            return HealthStatus.Degraded;
        }

        // Check failure rate
        if (status.EventsProcessed > 0)
        {
            var failureRate = (double)status.EventsFailed / status.EventsProcessed;
            if (failureRate > _options.MaxFailureRate)
            {
                return HealthStatus.Unhealthy;
            }
            if (failureRate > _options.WarningFailureRate)
            {
                return HealthStatus.Degraded;
            }
        }

        // Check lag
        foreach (var lag in status.LagBySource)
        {
            if (lag.Value > _options.MaxLagSeconds)
            {
                return HealthStatus.Unhealthy;
            }
            if (lag.Value > _options.WarningLagSeconds)
            {
                return HealthStatus.Degraded;
            }
        }

        return HealthStatus.Healthy;
    }
}

/// <summary>
/// Configuration options for change event health checks.
/// </summary>
public sealed class ChangeEventHealthCheckOptions
{
    /// <summary>
    /// Gets or sets the maximum acceptable failure rate (0.0 to 1.0).
    /// </summary>
    public double MaxFailureRate { get; set; } = 0.05; // 5%

    /// <summary>
    /// Gets or sets the warning failure rate (0.0 to 1.0).
    /// </summary>
    public double WarningFailureRate { get; set; } = 0.01; // 1%

    /// <summary>
    /// Gets or sets the maximum acceptable lag in seconds.
    /// </summary>
    public double MaxLagSeconds { get; set; } = 300; // 5 minutes

    /// <summary>
    /// Gets or sets the warning lag threshold in seconds.
    /// </summary>
    public double WarningLagSeconds { get; set; } = 60; // 1 minute

    /// <summary>
    /// Gets or sets the health check timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
}