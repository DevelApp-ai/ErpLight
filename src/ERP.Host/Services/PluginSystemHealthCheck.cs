using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ERP.Host.Services;

public class PluginSystemHealthCheck : IHealthCheck
{
    private readonly PluginManager _pluginManager;

    public PluginSystemHealthCheck(PluginManager pluginManager)
    {
        _pluginManager = pluginManager;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (_pluginManager.LoadedPlugins.Count == 0)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("No plugins are loaded."));
        }

        var data = new Dictionary<string, object>
        {
            ["LoadedPlugins"] = _pluginManager.LoadedPlugins.Count
        };

        return Task.FromResult(HealthCheckResult.Healthy("Plugin system is ready.", data));
    }
}
