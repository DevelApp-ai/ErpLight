using ERP.SharedKernel.Contracts;
using DevelApp.RuntimePluggableClassFactory;
using DevelApp.RuntimePluggableClassFactory.FilePlugin;
using System.Reflection;

namespace ERP.Host.Services;

/// <summary>
/// Manages the lifecycle of plugin modules using RuntimePluggableClassFactory.
/// Replaces the custom plugin loading implementation with the standardized factory approach.
/// </summary>
public class PluginManager
{
    private readonly ILogger<PluginManager> _logger;
    private readonly IServiceProvider _serviceProvider;
    private PluginClassFactory<IPluginModule>? _pluginFactory;
    private readonly List<IPluginModule> _loadedPlugins = new();

    public PluginManager(ILogger<PluginManager> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Gets all currently loaded plugin modules.
    /// </summary>
    public IReadOnlyList<IPluginModule> LoadedPlugins => _loadedPlugins.AsReadOnly();

    /// <summary>
    /// Discovers and loads all plugins from the plugins directory using RuntimePluggableClassFactory.
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
            // Initialize the RuntimePluggableClassFactory with the actual plugins directory
            var absolutePath = Path.GetFullPath(pluginsDirectory);
            _logger.LogInformation("Attempting to initialize plugin factory with directory: {AbsolutePath}", absolutePath);
            
            // Try different URI formats to find the one that works
            var pluginPathUri = new Uri(absolutePath);
            _logger.LogDebug("Initializing plugin factory with URI: {PluginPath}", pluginPathUri);
            
            var fileLoader = new FilePluginLoader<IPluginModule>(pluginPathUri);
            _pluginFactory = new PluginClassFactory<IPluginModule>(fileLoader);
            
            // Subscribe to error events
            _pluginFactory.PluginInstantiationFailed += OnPluginInstantiationFailed;

            // Refresh plugins from the specified directory
            await _pluginFactory.RefreshPluginsAsync();
            
            // Get all available plugins from the factory
            var availablePlugins = await _pluginFactory.GetPossiblePlugins();
            
            _logger.LogInformation("Found {PluginCount} potential plugin classes via RuntimePluggableClassFactory", availablePlugins.Count());

            // If no plugins found via factory, try manual discovery as fallback
            if (!availablePlugins.Any())
            {
                _logger.LogWarning("No plugins found via RuntimePluggableClassFactory, attempting manual discovery as fallback...");
                await DiscoverPluginsManually(pluginsDirectory);
                return;
            }

            // Load each plugin using the factory
            foreach (var pluginInfo in availablePlugins)
            {
                try
                {
                    // pluginInfo is a tuple (NamespaceString moduleName, IdentifierString pluginName, SemanticVersionNumber version, string Description, Type Type)
                    await LoadPluginAsync(pluginInfo.moduleName, pluginInfo.pluginName, pluginInfo.version);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load plugin '{Module}.{Name}' v{Version}", 
                        pluginInfo.moduleName, pluginInfo.pluginName, pluginInfo.version);
                }
            }

            _logger.LogInformation("Successfully loaded {LoadedCount} plugins", _loadedPlugins.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to discover plugins from '{PluginsDirectory}'", pluginsDirectory);
        }
    }

    /// <summary>
    /// Loads a specific plugin using the RuntimePluggableClassFactory.
    /// </summary>
    /// <param name="moduleName">The module name of the plugin.</param>
    /// <param name="pluginName">The name of the plugin.</param>
    /// <param name="version">The version of the plugin.</param>
    /// <returns>A task representing the loading operation.</returns>
    private async Task LoadPluginAsync(string moduleName, string pluginName, string version)
    {
        if (_pluginFactory == null)
        {
            _logger.LogError("Plugin factory is not initialized");
            return;
        }

        _logger.LogDebug("Loading plugin '{Module}.{Name}' v{Version}", moduleName, pluginName, version);

        // Get plugin instance from the factory
        var plugin = _pluginFactory.GetInstance(moduleName, pluginName);
        
        if (plugin == null)
        {
            _logger.LogWarning("Failed to instantiate plugin '{Module}.{Name}' v{Version}", moduleName, pluginName, version);
            return;
        }

        _loadedPlugins.Add(plugin);
        
        _logger.LogInformation("Loaded plugin: {ModuleId} ({DisplayName}) v{Version}", 
            plugin.ModuleId, plugin.DisplayName, version);

        // Initialize the plugin
        await plugin.InitializeAsync(_serviceProvider);
    }

    /// <summary>
    /// Gets all plugins that implement the specified interface.
    /// </summary>
    /// <typeparam name="T">The interface type to filter by.</typeparam>
    /// <returns>A collection of plugins implementing the interface.</returns>
    public IEnumerable<T> GetPlugins<T>() where T : class
    {
        return _loadedPlugins.OfType<T>();
    }

    /// <summary>
    /// Shuts down all loaded plugins and releases resources.
    /// </summary>
    /// <returns>A task representing the shutdown operation.</returns>
    public async Task ShutdownAsync()
    {
        _logger.LogInformation("Shutting down {PluginCount} plugins", _loadedPlugins.Count);

        foreach (var plugin in _loadedPlugins)
        {
            try
            {
                await plugin.ShutdownAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error shutting down plugin '{ModuleId}'", plugin.ModuleId);
            }
        }

        _loadedPlugins.Clear();
        
        // Unsubscribe from events
        if (_pluginFactory != null)
        {
            _pluginFactory.PluginInstantiationFailed -= OnPluginInstantiationFailed;
        }
    }

    /// <summary>
    /// Event handler for plugin instantiation failures.
    /// </summary>
    /// <param name="sender">The sender of the event.</param>
    /// <param name="e">The event arguments containing error information.</param>
    private void OnPluginInstantiationFailed(object? sender, PluginInstantiationErrorEventArgs e)
    {
        _logger.LogError(e.Exception, "Plugin instantiation failed for '{ModuleName}.{PluginName}' v{Version} at {Timestamp}", 
            e.ModuleName, e.PluginName, e.Version, e.Timestamp);
    }

    /// <summary>
    /// Manual plugin discovery fallback when RuntimePluggableClassFactory doesn't find plugins.
    /// This helps us understand what's happening and ensures backward compatibility.
    /// </summary>
    /// <param name="pluginsDirectory">The directory to scan for plugins.</param>
    /// <returns>A task representing the discovery operation.</returns>
    private async Task DiscoverPluginsManually(string pluginsDirectory)
    {
        var pluginAssemblies = Directory.GetFiles(pluginsDirectory, "*.dll", SearchOption.AllDirectories)
            .Where(path => Path.GetFileName(path).StartsWith("ERP.Plugin."))
            .ToList();

        _logger.LogInformation("Manual discovery found {PluginCount} potential plugin assemblies", pluginAssemblies.Count);

        foreach (var assemblyPath in pluginAssemblies)
        {
            try
            {
                await LoadPluginManually(assemblyPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to manually load plugin from '{AssemblyPath}'", assemblyPath);
            }
        }
    }

    /// <summary>
    /// Manually loads a plugin assembly for fallback compatibility.
    /// </summary>
    /// <param name="assemblyPath">The path to the plugin assembly.</param>
    /// <returns>A task representing the loading operation.</returns>
    private async Task LoadPluginManually(string assemblyPath)
    {
        _logger.LogDebug("Manually loading plugin from '{AssemblyPath}'", assemblyPath);

        var assembly = Assembly.LoadFrom(assemblyPath);
        
        var pluginTypes = assembly.GetTypes()
            .Where(type => type.IsClass && !type.IsAbstract && typeof(IPluginModule).IsAssignableFrom(type))
            .ToList();

        if (!pluginTypes.Any())
        {
            _logger.LogWarning("No plugin module implementations found in '{AssemblyPath}'", assemblyPath);
            return;
        }

        foreach (var pluginType in pluginTypes)
        {
            try
            {
                var plugin = (IPluginModule)Activator.CreateInstance(pluginType)!;
                _loadedPlugins.Add(plugin);
                
                _logger.LogInformation("Manually loaded plugin: {ModuleId} ({DisplayName}) v{Version}", 
                    plugin.ModuleId, plugin.DisplayName, plugin.Version);

                // Initialize the plugin
                await plugin.InitializeAsync(_serviceProvider);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to instantiate plugin type '{PluginType}' from '{AssemblyPath}'", pluginType.Name, assemblyPath);
            }
        }
    }
}