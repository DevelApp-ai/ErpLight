using ERP.SharedKernel.Contracts;
using DevelApp.RuntimePluggableClassFactory;
using DevelApp.RuntimePluggableClassFactory.FilePlugin;
using DevelApp.Utility.Model;
using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace ERP.Host.Services;

/// <summary>
/// Manages the lifecycle of plugin modules using the DevelApp.RuntimePluggableClassFactory 2.0.1 NuGet package.
/// Demonstrates enterprise-grade plugin discovery and loading capabilities for modular ERP systems.
/// 
/// This implementation showcases the following RuntimePluggableClassFactory features:
/// - Dynamic plugin discovery from directory paths
/// - Type-safe plugin instantiation with generic constraints
/// - Semantic versioning support and compatibility checks
/// - Rich plugin metadata handling (name, description, version)
/// - Error handling and recovery mechanisms
/// - Event-driven plugin lifecycle monitoring
/// 
/// Learn more: https://github.com/DevelApp-ai/RuntimePluggableClassFactory
/// </summary>
public class PluginManager
{
    private readonly ILogger<PluginManager> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private PluginClassFactory<IPluginModule>? _pluginFactory;
    private readonly List<IPluginModule> _loadedPlugins = new();

    public PluginManager(ILogger<PluginManager> logger, IServiceProvider serviceProvider, IConfiguration configuration)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _configuration = configuration;
    }

    /// <summary>
    /// Gets all currently loaded plugin modules.
    /// </summary>
    public IReadOnlyList<IPluginModule> LoadedPlugins => _loadedPlugins.AsReadOnly();

    /// <summary>
    /// Discovers and loads all plugins from the plugins directory using RuntimePluggableClassFactory.
    /// 
    /// This method demonstrates the primary workflow for the RuntimePluggableClassFactory:
    /// 1. Initialize FilePluginLoader with the plugin directory URI
    /// 2. Create PluginClassFactory with type constraint for IPluginModule
    /// 3. Subscribe to error events for monitoring and diagnostics
    /// 4. Refresh plugin discovery to scan for available assemblies
    /// 5. Enumerate discovered plugins with metadata
    /// 6. Instantiate and initialize each valid plugin
    /// </summary>
    /// <param name="pluginsDirectory">The directory to scan for plugins.</param>
    /// <returns>A task representing the discovery operation.</returns>
    public async Task DiscoverAndLoadPluginsAsync(string pluginsDirectory)
    {
        if (!Directory.Exists(pluginsDirectory))
        {
            _logger.LogWarning("Plugins directory '{PluginsDirectory}' does not exist. No plugins will be loaded.", pluginsDirectory);
            return;
        }

        try
        {
            // Step 1: Initialize the RuntimePluggableClassFactory with the plugins directory
            var absolutePath = Path.GetFullPath(pluginsDirectory);
            _logger.LogInformation("🔍 Initializing RuntimePluggableClassFactory 2.0.1 with directory: {AbsolutePath}", absolutePath);
            
            var pluginPathUri = new Uri(absolutePath);
            _logger.LogDebug("Plugin discovery URI: {PluginPath}", pluginPathUri);
            
            // Step 2: Create FilePluginLoader for directory-based plugin discovery
            var fileLoader = new FilePluginLoader<IPluginModule>(pluginPathUri);
            _pluginFactory = new PluginClassFactory<IPluginModule>(fileLoader);
            
            // Step 3: Subscribe to RuntimePluggableClassFactory events for monitoring
            _pluginFactory.PluginInstantiationFailed += OnPluginInstantiationFailed;
            _logger.LogDebug("✅ Event handlers registered for plugin monitoring");

            // Step 4: Refresh plugins to discover available assemblies
            _logger.LogInformation("🔄 Scanning for plugin assemblies implementing IPluginModule...");
            await _pluginFactory.RefreshPluginsAsync();
            
            // Step 5: Get discovered plugins with rich metadata
            var availablePlugins = await _pluginFactory.GetPossiblePlugins();
            var filteredPlugins = ApplyPluginSecurityPolicies(availablePlugins);

            _logger.LogInformation("📋 RuntimePluggableClassFactory discovered {PluginCount} plugin class(es), {AcceptedCount} accepted after policy validation", availablePlugins.Count(), filteredPlugins.Count);

            // Log detailed plugin information
            foreach (var plugin in filteredPlugins)
            {
                _logger.LogDebug("Discovered Plugin: {Module}.{Name} v{Version} - {Description}", 
                    plugin.moduleName, plugin.pluginName, plugin.version, plugin.Description);
            }

            // If no plugins found via factory, try manual discovery as fallback
            if (!filteredPlugins.Any())
            {
                _logger.LogWarning("⚠️ No plugins found via RuntimePluggableClassFactory, attempting manual discovery as fallback...");
                await DiscoverPluginsManually(pluginsDirectory);
                return;
            }

            // Step 6: Load each plugin using the factory
            foreach (var pluginInfo in filteredPlugins)
            {
                try
                {
                    await LoadPluginViaFactory(pluginInfo.moduleName, pluginInfo.pluginName, pluginInfo.version.ToString());
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Failed to load plugin '{Module}.{Name}' v{Version}", 
                        pluginInfo.moduleName, pluginInfo.pluginName, pluginInfo.version);
                }
            }

            _logger.LogInformation("✅ Successfully loaded {LoadedCount} plugins via RuntimePluggableClassFactory", _loadedPlugins.Count);
            
            // Log summary of loaded plugins
            foreach (var plugin in _loadedPlugins)
            {
                _logger.LogInformation("🔌 Active Plugin: {ModuleId} ({DisplayName}) v{Version}", 
                    plugin.ModuleId, plugin.DisplayName, plugin.Version);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to discover plugins from '{PluginsDirectory}' using RuntimePluggableClassFactory", pluginsDirectory);
        }
    }

    private List<(NamespaceString moduleName, IdentifierString pluginName, SemanticVersionNumber version, string Description, Type Type)> ApplyPluginSecurityPolicies(IEnumerable<(NamespaceString moduleName, IdentifierString pluginName, SemanticVersionNumber version, string Description, Type Type)> discoveredPlugins)
    {
        var pluginSettings = _configuration.GetSection("PluginSettings");
        var supportedVersions = pluginSettings.GetSection("SupportedPluginVersions").Get<string[]>() ?? Array.Empty<string>();
        var allowedModules = pluginSettings.GetSection("AllowedModules").Get<string[]>() ?? Array.Empty<string>();
        var enforceAllowList = pluginSettings.GetValue<bool>("EnforceAllowList");
        var requireDescription = pluginSettings.GetSection("RuntimePluggableClassFactory:PluginValidation").GetValue<bool>("RequireDescription");

        var acceptedPlugins = new List<(NamespaceString moduleName, IdentifierString pluginName, SemanticVersionNumber version, string Description, Type Type)>();

        foreach (var plugin in discoveredPlugins)
        {
            var versionString = plugin.version.ToString();
            if (supportedVersions.Length > 0 && !supportedVersions.Contains(versionString, StringComparer.OrdinalIgnoreCase))
            {
                _logger.LogWarning("🚫 Rejecting plugin {Module}.{Plugin}: unsupported version {Version}", plugin.moduleName, plugin.pluginName, versionString);
                continue;
            }

            if (enforceAllowList && allowedModules.Length > 0 && !allowedModules.Contains(plugin.moduleName.ToString(), StringComparer.OrdinalIgnoreCase))
            {
                _logger.LogWarning("🚫 Rejecting plugin {Module}.{Plugin}: module not in allowlist", plugin.moduleName, plugin.pluginName);
                continue;
            }

            if (requireDescription && string.IsNullOrWhiteSpace(plugin.Description))
            {
                _logger.LogWarning("🚫 Rejecting plugin {Module}.{Plugin}: description is required", plugin.moduleName, plugin.pluginName);
                continue;
            }

            acceptedPlugins.Add(plugin);
        }

        return acceptedPlugins;
    }

    /// <summary>
    /// Loads a specific plugin using the RuntimePluggableClassFactory.
    /// Demonstrates the plugin instantiation and initialization workflow.
    /// </summary>
    /// <param name="moduleName">The module name of the plugin.</param>
    /// <param name="pluginName">The name of the plugin.</param>
    /// <param name="version">The version of the plugin.</param>
    /// <returns>A task representing the loading operation.</returns>
    private async Task LoadPluginViaFactory(string moduleName, string pluginName, string version)
    {
        if (_pluginFactory == null)
        {
            _logger.LogError("❌ PluginClassFactory is not initialized - cannot load plugins");
            return;
        }

        _logger.LogDebug("🔧 Loading plugin '{Module}.{Name}' v{Version} via RuntimePluggableClassFactory", 
            moduleName, pluginName, version);

        // Use RuntimePluggableClassFactory to get plugin instance
        var plugin = _pluginFactory.GetInstance(moduleName, pluginName);
        
        if (plugin == null)
        {
            _logger.LogWarning("⚠️ RuntimePluggableClassFactory failed to instantiate plugin '{Module}.{Name}' v{Version}", 
                moduleName, pluginName, version);
            return;
        }

        _loadedPlugins.Add(plugin);
        
        _logger.LogInformation("✅ RuntimePluggableClassFactory loaded: {ModuleId} ({DisplayName}) v{Version}", 
            plugin.ModuleId, plugin.DisplayName, version);

        // Initialize the plugin instance
        try
        {
            await plugin.InitializeAsync(_serviceProvider);
            _logger.LogDebug("✅ Plugin '{ModuleId}' initialized successfully", plugin.ModuleId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to initialize plugin '{ModuleId}'", plugin.ModuleId);
        }
    }

    /// <summary>
    /// Gets all plugins that implement the specified interface.
    /// Useful for querying loaded plugins by capability.
    /// </summary>
    /// <typeparam name="T">The interface type to filter by.</typeparam>
    /// <returns>A collection of plugins implementing the interface.</returns>
    public IEnumerable<T> GetPlugins<T>() where T : class
    {
        return _loadedPlugins.OfType<T>();
    }

    /// <summary>
    /// Shuts down all loaded plugins and releases RuntimePluggableClassFactory resources.
    /// Demonstrates proper cleanup and resource management.
    /// </summary>
    /// <returns>A task representing the shutdown operation.</returns>
    public async Task ShutdownAsync()
    {
        _logger.LogInformation("🔄 Shutting down {PluginCount} plugins", _loadedPlugins.Count);

        foreach (var plugin in _loadedPlugins)
        {
            try
            {
                await plugin.ShutdownAsync();
                _logger.LogDebug("✅ Plugin '{ModuleId}' shut down successfully", plugin.ModuleId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error shutting down plugin '{ModuleId}'", plugin.ModuleId);
            }
        }

        _loadedPlugins.Clear();
        
        // Clean up RuntimePluggableClassFactory event subscriptions
        if (_pluginFactory != null)
        {
            _pluginFactory.PluginInstantiationFailed -= OnPluginInstantiationFailed;
            _logger.LogDebug("✅ RuntimePluggableClassFactory event handlers unregistered");
        }
    }

    /// <summary>
    /// Event handler for RuntimePluggableClassFactory plugin instantiation failures.
    /// Demonstrates error monitoring and diagnostics capabilities.
    /// </summary>
    /// <param name="sender">The PluginClassFactory that raised the event.</param>
    /// <param name="e">The event arguments containing detailed error information.</param>
    private void OnPluginInstantiationFailed(object? sender, PluginInstantiationErrorEventArgs e)
    {
        _logger.LogError(e.Exception, 
            "🚫 RuntimePluggableClassFactory plugin instantiation failed for '{ModuleName}.{PluginName}' v{Version} at {Timestamp}\n" +
            "   Error Type: {ErrorType}\n" +
            "   Plugin Assembly: {Assembly}\n" +
            "   This error was captured by the RuntimePluggableClassFactory event system", 
            e.ModuleName, e.PluginName, e.Version, e.Timestamp, 
            e.Exception?.GetType().Name ?? "Unknown", 
            e.Exception?.Source ?? "Unknown");
    }

    /// <summary>
    /// Manual plugin discovery fallback when RuntimePluggableClassFactory doesn't find plugins.
    /// This demonstrates backward compatibility and provides insight into what's happening
    /// when the factory-based approach encounters issues.
    /// 
    /// In production, this fallback helps diagnose configuration issues and ensures
    /// the application can still function while troubleshooting plugin discovery problems.
    /// </summary>
    /// <param name="pluginsDirectory">The directory to scan for plugins.</param>
    /// <returns>A task representing the discovery operation.</returns>
    private async Task DiscoverPluginsManually(string pluginsDirectory)
    {
        _logger.LogInformation("🔍 Manual plugin discovery - scanning for ERP.Plugin.*.dll assemblies");
        
        var pluginAssemblies = Directory.GetFiles(pluginsDirectory, "*.dll", SearchOption.AllDirectories)
            .Where(path => Path.GetFileName(path).StartsWith("ERP.Plugin."))
            .ToList();

        _logger.LogInformation("📁 Manual discovery found {PluginCount} potential plugin assemblies", pluginAssemblies.Count);

        foreach (var assemblyPath in pluginAssemblies)
        {
            try
            {
                await LoadPluginManually(assemblyPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to manually load plugin from '{AssemblyPath}'", assemblyPath);
            }
        }
        
        if (_loadedPlugins.Any())
        {
            _logger.LogInformation("✅ Manual fallback loaded {LoadedCount} plugins", _loadedPlugins.Count);
        }
        else
        {
            _logger.LogWarning("⚠️ No plugins loaded via manual discovery either - check plugin assemblies and IPluginModule implementations");
        }
    }

    /// <summary>
    /// Manually loads a plugin assembly for fallback compatibility.
    /// This method demonstrates traditional reflection-based plugin loading
    /// as a comparison to the RuntimePluggableClassFactory approach.
    /// </summary>
    /// <param name="assemblyPath">The path to the plugin assembly.</param>
    /// <returns>A task representing the loading operation.</returns>
    private async Task LoadPluginManually(string assemblyPath)
    {
        _logger.LogDebug("🔧 Manually loading plugin from '{AssemblyPath}'", assemblyPath);

        var assembly = Assembly.LoadFrom(assemblyPath);
        
        var pluginTypes = assembly.GetTypes()
            .Where(type => type.IsClass && !type.IsAbstract && typeof(IPluginModule).IsAssignableFrom(type))
            .ToList();

        if (!pluginTypes.Any())
        {
            _logger.LogWarning("⚠️ No IPluginModule implementations found in '{AssemblyPath}'", assemblyPath);
            return;
        }

        foreach (var pluginType in pluginTypes)
        {
            try
            {
                var plugin = (IPluginModule)Activator.CreateInstance(pluginType)!;
                _loadedPlugins.Add(plugin);
                
                _logger.LogInformation("✅ Manually loaded plugin: {ModuleId} ({DisplayName}) v{Version}", 
                    plugin.ModuleId, plugin.DisplayName, plugin.Version);

                // Initialize the plugin
                await plugin.InitializeAsync(_serviceProvider);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to instantiate plugin type '{PluginType}' from '{AssemblyPath}'", 
                    pluginType.Name, assemblyPath);
            }
        }
    }
}