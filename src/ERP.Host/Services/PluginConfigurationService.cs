using System;
using System.Collections.Generic;
using ERP.SharedKernel.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ERP.Host.Services;

/// <summary>
/// Service for managing plugin configurations.
/// </summary>
public class PluginConfigurationService
{
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PluginConfigurationService> _logger;
    private readonly Dictionary<string, IPluginConfiguration> _pluginConfigurations = new();

    public PluginConfigurationService(
        IConfiguration configuration,
        IServiceProvider serviceProvider,
        ILogger<PluginConfigurationService> logger)
    {
        _configuration = configuration;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Gets the configuration for the specified plugin.
    /// </summary>
    /// <typeparam name="TConfiguration">The configuration type.</typeparam>
    /// <param name="pluginModuleId">The plugin module ID.</param>
    /// <returns>The plugin configuration or null if not found.</returns>
    public TConfiguration? GetConfiguration<TConfiguration>(string pluginModuleId)
        where TConfiguration : class, IPluginConfiguration, new()
    {
        if (_pluginConfigurations.TryGetValue(pluginModuleId, out var config))
        {
            return (TConfiguration?)config;
        }

        return default;
    }

    /// <summary>
    /// Gets the configuration for the specified plugin.
    /// </summary>
    /// <param name="pluginModuleId">The plugin module ID.</param>
    /// <returns>The plugin configuration or null if not found.</returns>
    public IPluginConfiguration? GetConfiguration(string pluginModuleId)
    {
        _pluginConfigurations.TryGetValue(pluginModuleId, out var config);
        return config;
    }

    /// <summary>
    /// Loads configuration for all loaded plugins.
    /// </summary>
    /// <param name="pluginManager">The plugin manager.</param>
    public void LoadPluginConfigurations(PluginManager pluginManager)
    {
        foreach (var plugin in pluginManager.LoadedPlugins)
        {
            LoadPluginConfiguration(plugin);
        }

        _logger.LogInformation("Loaded configurations for {PluginCount} plugins", _pluginConfigurations.Count);
    }

    /// <summary>
    /// Loads configuration for a specific plugin.
    /// </summary>
    /// <param name="plugin">The plugin.</param>
    public void LoadPluginConfiguration(IPluginModule plugin)
    {
        var pluginModuleId = plugin.ModuleId;
        
        // Check if plugin implements IPluginConfiguration
        if (plugin is IPluginConfiguration pluginConfig)
        {
            pluginConfig.Configure(_configuration);
            _pluginConfigurations[pluginModuleId] = pluginConfig;
            _logger.LogDebug("Loaded configuration for plugin {PluginModuleId}", pluginModuleId);
            return;
        }

        // Try to create configuration from convention
        var configTypeName = $"ERP.Plugin.{pluginModuleId}.Configuration.{pluginModuleId}Configuration";
        
        try
        {
            var configType = Type.GetType(configTypeName);
            if (configType != null && typeof(IPluginConfiguration).IsAssignableFrom(configType))
            {
                var configInstance = (IPluginConfiguration)Activator.CreateInstance(configType, 
                    pluginModuleId, 
                    $"PluginSettings:{pluginModuleId}",
                    _logger)!;
                
                configInstance.Configure(_configuration);
                _pluginConfigurations[pluginModuleId] = configInstance;
                _logger.LogDebug("Loaded convention-based configuration for plugin {PluginModuleId}", pluginModuleId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not load configuration for plugin {PluginModuleId}", pluginModuleId);
        }
    }

    /// <summary>
    /// Gets the database configuration for a plugin.
    /// </summary>
    /// <param name="pluginModuleId">The plugin module ID.</param>
    /// <returns>The database configuration or a default.</returns>
    public PluginDatabaseConfiguration GetDatabaseConfiguration(string pluginModuleId)
    {
        var section = _configuration.GetSection($"PluginSettings:{pluginModuleId}:Database");
        return section.Get<PluginDatabaseConfiguration>() ?? new PluginDatabaseConfiguration();
    }

    /// <summary>
    /// Gets the feature configuration for a plugin.
    /// </summary>
    /// <param name="pluginModuleId">The plugin module ID.</param>
    /// <returns>The feature configuration or a default.</returns>
    public PluginFeatureConfiguration GetFeatureConfiguration(string pluginModuleId)
    {
        var section = _configuration.GetSection($"PluginSettings:{pluginModuleId}:Features");
        return section.Get<PluginFeatureConfiguration>() ?? new PluginFeatureConfiguration();
    }

    /// <summary>
    /// Gets the API configuration for a plugin.
    /// </summary>
    /// <param name="pluginModuleId">The plugin module ID.</param>
    /// <returns>The API configuration or a default.</returns>
    public PluginApiConfiguration GetApiConfiguration(string pluginModuleId)
    {
        var section = _configuration.GetSection($"PluginSettings:{pluginModuleId}:Api");
        return section.Get<PluginApiConfiguration>() ?? new PluginApiConfiguration();
    }

    /// <summary>
    /// Gets a configuration value for a plugin.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="pluginModuleId">The plugin module ID.</param>
    /// <param name="key">The configuration key.</param>
    /// <param name="defaultValue">The default value.</param>
    /// <returns>The configuration value.</returns>
    public T GetValue<T>(string pluginModuleId, string key, T defaultValue = default!)
    {
        var section = _configuration.GetSection($"PluginSettings:{pluginModuleId}");
        return section.GetValue(key, defaultValue);
    }
}
