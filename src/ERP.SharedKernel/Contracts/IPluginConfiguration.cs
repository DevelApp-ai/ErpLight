using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Binder;
using Microsoft.Extensions.Logging;

namespace ERP.SharedKernel.Contracts;

/// <summary>
/// Interface for plugin configuration.
/// Plugins can implement this to provide their own configuration.
/// </summary>
public interface IPluginConfiguration
{
    /// <summary>
    /// Gets the plugin module ID that this configuration applies to.
    /// </summary>
    string PluginModuleId { get; }

    /// <summary>
    /// Gets the configuration section name.
    /// </summary>
    string ConfigurationSection { get; }

    /// <summary>
    /// Configures the plugin with the provided configuration.
    /// </summary>
    /// <param name="configuration">The configuration to use.</param>
    void Configure(IConfiguration configuration);

    /// <summary>
    /// Validates the plugin configuration.
    /// </summary>
    /// <returns>True if the configuration is valid.</returns>
    bool ValidateConfiguration();

    /// <summary>
    /// Gets the configuration as a strongly-typed object.
    /// </summary>
    /// <typeparam name="T">The configuration type.</typeparam>
    /// <returns>The configuration object.</returns>
    T GetConfiguration<T>() where T : class, new();
}

/// <summary>
/// Base class for plugin configuration.
/// </summary>
/// <typeparam name="TConfiguration">The configuration type.</typeparam>
public abstract class PluginConfigurationBase<TConfiguration> : IPluginConfiguration
    where TConfiguration : class, new()
{
    private readonly ILogger _logger;
    private TConfiguration? _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginConfigurationBase{TConfiguration}"/> class.
    /// </summary>
    /// <param name="pluginModuleId">The plugin module ID.</param>
    /// <param name="configurationSection">The configuration section name.</param>
    /// <param name="logger">The logger.</param>
    protected PluginConfigurationBase(string pluginModuleId, string configurationSection, ILogger logger)
    {
        PluginModuleId = pluginModuleId;
        ConfigurationSection = configurationSection;
        _logger = logger;
    }

    /// <summary>
    /// Gets the plugin module ID that this configuration applies to.
    /// </summary>
    public string PluginModuleId { get; }

    /// <summary>
    /// Gets the configuration section name.
    /// </summary>
    public string ConfigurationSection { get; }

    /// <summary>
    /// Gets the current configuration.
    /// </summary>
    public TConfiguration? Configuration => _configuration;

    /// <summary>
    /// Configures the plugin with the provided configuration.
    /// </summary>
    /// <param name="configuration">The configuration to use.</param>
    public void Configure(IConfiguration configuration)
    {
        var section = configuration.GetSection(ConfigurationSection);
        _configuration = section.Get<TConfiguration>() ?? new TConfiguration();
        
        _logger.LogDebug("Configured {PluginModuleId} with section {ConfigurationSection}", 
            PluginModuleId, ConfigurationSection);
    }

    /// <summary>
    /// Validates the plugin configuration.
    /// </summary>
    /// <returns>True if the configuration is valid.</returns>
    public virtual bool ValidateConfiguration()
    {
        if (_configuration == null)
        {
            _logger.LogWarning("Configuration for {PluginModuleId} is null", PluginModuleId);
            return false;
        }
        
        return true;
    }

    /// <summary>
    /// Gets the configuration as a strongly-typed object.
    /// </summary>
    /// <typeparam name="T">The configuration type.</typeparam>
    /// <returns>The configuration object.</returns>
    public T GetConfiguration<T>() where T : class, new()
    {
        if (_configuration is T typedConfig)
        {
            return typedConfig;
        }
        
        return new T();
    }
}

/// <summary>
/// Configuration for plugin database settings.
/// </summary>
public class PluginDatabaseConfiguration
{
    /// <summary>
    /// Gets or sets the connection string.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets the database provider (e.g., SqlServer, PostgreSQL, SQLite).
    /// </summary>
    public string Provider { get; set; } = "SqlServer";

    /// <summary>
    /// Gets or sets whether to enable migrations.
    /// </summary>
    public bool EnableMigrations { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum retry count for transient errors.
    /// </summary>
    public int MaxRetryCount { get; set; } = 3;

    /// <summary>
    /// Gets or sets the retry delay in milliseconds.
    /// </summary>
    public int RetryDelayMs { get; set; } = 1000;
}

/// <summary>
/// Configuration for plugin feature flags.
/// </summary>
public class PluginFeatureConfiguration
{
    /// <summary>
    /// Gets or sets whether the plugin is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets whether debug mode is enabled.
    /// </summary>
    public bool DebugEnabled { get; set; } = false;

    /// <summary>
    /// Gets or sets whether audit logging is enabled.
    /// </summary>
    public bool AuditLoggingEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets whether caching is enabled.
    /// </summary>
    public bool CachingEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the cache duration in minutes.
    /// </summary>
    public int CacheDurationMinutes { get; set; } = 30;

    /// <summary>
    /// Gets or sets the maximum items to cache.
    /// </summary>
    public int MaxCacheItems { get; set; } = 1000;
}

/// <summary>
/// Configuration for plugin API settings.
/// </summary>
public class PluginApiConfiguration
{
    /// <summary>
    /// Gets or sets the API prefix for this plugin.
    /// </summary>
    public string ApiPrefix { get; set; } = "api/plugins/{moduleId}";

    /// <summary>
    /// Gets or sets whether API is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets whether swagger documentation is enabled.
    /// </summary>
    public bool SwaggerEnabled { get; set; } = false;

    /// <summary>
    /// Gets or sets the API version.
    /// </summary>
    public string Version { get; set; } = "v1";

    /// <summary>
    /// Gets or sets the rate limit per minute.
    /// </summary>
    public int RateLimitPerMinute { get; set; } = 100;
}
