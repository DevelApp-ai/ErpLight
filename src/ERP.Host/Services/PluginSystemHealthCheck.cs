using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ERP.Host.Services;

public class PluginSystemHealthCheck : IHealthCheck
{
    private readonly PluginManager _pluginManager;

    public PluginSystemHealthCheck(PluginManager pluginManager)
    {
        _pluginManager = pluginManager;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (_pluginManager.LoadedPlugins.Count == 0)
        {
            return HealthCheckResult.Unhealthy("No plugins are loaded.");
        }

        var data = new Dictionary<string, object>
        {
            ["LoadedPlugins"] = _pluginManager.LoadedPlugins.Count
        };

        // Check individual plugin health
        var pluginHealth = new Dictionary<string, HealthStatus>();
        foreach (var plugin in _pluginManager.LoadedPlugins)
        {
            try
            {
                var healthStatus = await ((dynamic)plugin).CheckHealthAsync();
                pluginHealth[plugin.ModuleId] = healthStatus;
            }
            catch (Exception ex)
            {
                pluginHealth[plugin.ModuleId] = HealthStatus.Unhealthy;
            }
        }

        data["PluginHealth"] = pluginHealth;

        // If any plugin is unhealthy, mark the whole system as degraded
        var unhealthyPlugins = pluginHealth.Count(kvp => kvp.Value == HealthStatus.Unhealthy);
        if (unhealthyPlugins > 0)
        {
            return HealthCheckResult.Degraded(
                $"Plugin system is degraded. {unhealthyPlugins} plugin(s) unhealthy.",
                data);
        }

        return HealthCheckResult.Healthy("Plugin system is ready.", data);
    }
}
