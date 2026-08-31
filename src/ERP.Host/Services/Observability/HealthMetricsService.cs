using System;
using System.Diagnostics.Metrics;
using ERP.Host.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace ERP.Host.Services.Observability;

/// <summary>
/// Service for tracking health metrics.
/// </summary>
public class HealthMetricsService
{
    private readonly TelemetryService _telemetryService;
    private readonly PluginManager _pluginManager;
    private readonly ILogger<HealthMetricsService> _logger;
    private Counter<long>? _healthCheckCounter;
    private Counter<long>? _healthCheckFailureCounter;
    private Histogram<double>? _healthCheckDurationHistogram;
    private Counter<long>? _pluginHealthCounter;
    private Counter<long>? _pluginHealthFailureCounter;

    public HealthMetricsService(
        TelemetryService telemetryService,
        PluginManager pluginManager,
        ILogger<HealthMetricsService> logger)
    {
        _telemetryService = telemetryService;
        _pluginManager = pluginManager;
        _logger = logger;

        InitializeMetrics();
    }

    private void InitializeMetrics()
    {
        try
        {
            var meter = _telemetryService.GetMeter("ErpLight.Health");
            if (meter != null)
            {
                _healthCheckCounter = meter.CreateCounter<long>("health.checks.total", "Total number of health checks");
                _healthCheckFailureCounter = meter.CreateCounter<long>("health.checks.failed", "Number of failed health checks");
                _healthCheckDurationHistogram = meter.CreateHistogram<double>("health.check.duration.ms", "Health check duration in milliseconds");
                _pluginHealthCounter = meter.CreateCounter<long>("plugin.health.checks.total", "Total number of plugin health checks");
                _pluginHealthFailureCounter = meter.CreateCounter<long>("plugin.health.checks.failed", "Number of failed plugin health checks");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing health metrics");
        }
    }

    /// <summary>
    /// Records a health check execution.
    /// </summary>
    /// <param name="healthCheckName">The health check name.</param>
    /// <param name="status">The health check status.</param>
    /// <param name="durationMs">The duration in milliseconds.</param>
    public void RecordHealthCheck(string healthCheckName, HealthStatus status, double durationMs)
    {
        try
        {
            _healthCheckCounter?.Add(1);
            _healthCheckDurationHistogram?.Record(durationMs);

            if (status != HealthStatus.Healthy)
            {
                _healthCheckFailureCounter?.Add(1);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording health check metric");
        }
    }

    /// <summary>
    /// Records a plugin health check execution.
    /// </summary>
    /// <param name="pluginModuleId">The plugin module ID.</param>
    /// <param name="status">The health check status.</param>
    public void RecordPluginHealthCheck(string pluginModuleId, HealthStatus status)
    {
        try
        {
            _pluginHealthCounter?.Add(1);

            if (status != HealthStatus.Healthy)
            {
                _pluginHealthFailureCounter?.Add(1);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording plugin health check metric");
        }
    }

    /// <summary>
    /// Gets the current health status of all plugins.
    /// </summary>
    /// <returns>A dictionary with plugin IDs and their health status.</returns>
    public Dictionary<string, HealthStatus> GetPluginHealthStatus()
    {
        var status = new Dictionary<string, HealthStatus>();

        foreach (var plugin in _pluginManager.LoadedPlugins)
        {
            try
            {
                var pluginWithHealth = plugin as IPluginModule;
                if (pluginWithHealth != null)
                {
                    var healthStatus = pluginWithHealth.CheckHealthAsync().GetAwaiter().GetResult();
                    status[plugin.ModuleId] = healthStatus;
                    RecordPluginHealthCheck(plugin.ModuleId, healthStatus);
                }
                else
                {
                    status[plugin.ModuleId] = HealthStatus.Healthy;
                    RecordPluginHealthCheck(plugin.ModuleId, HealthStatus.Healthy);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking health for plugin {PluginModuleId}", plugin.ModuleId);
                status[plugin.ModuleId] = HealthStatus.Unhealthy;
                RecordPluginHealthCheck(plugin.ModuleId, HealthStatus.Unhealthy);
            }
        }

        return status;
    }

    /// <summary>
    /// Gets the overall health status.
    /// </summary>
    /// <returns>The overall health status.</returns>
    public HealthStatus GetOverallHealthStatus()
    {
        var pluginStatus = GetPluginHealthStatus();

        foreach (var (pluginId, status) in pluginStatus)
        {
            if (status != HealthStatus.Healthy)
            {
                return status == HealthStatus.Degraded ? HealthStatus.Degraded : HealthStatus.Unhealthy;
            }
        }

        return HealthStatus.Healthy;
    }
}
