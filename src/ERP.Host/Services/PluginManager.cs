using ERP.SharedKernel.Contracts;
using ERP.Host.Plugin;
using System.Reflection;

namespace ERP.Host.Services;

/// <summary>
/// Manages the lifecycle of plugin modules including discovery, loading, and initialization.
/// </summary>
public class PluginManager
{
    private readonly ILogger<PluginManager> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly List<IPluginModule> _loadedPlugins = new();
    private readonly List<PluginLoadContext> _loadContexts = new();

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
    /// Discovers and loads all plugins from the plugins directory.
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

        var pluginAssemblies = Directory.GetFiles(pluginsDirectory, "*.dll", SearchOption.AllDirectories)
            .Where(path => Path.GetFileName(path).StartsWith("ERP.Plugin."))
            .ToList();

        _logger.LogInformation("Found {PluginCount} potential plugin assemblies in '{PluginsDirectory}'", pluginAssemblies.Count, pluginsDirectory);

        foreach (var assemblyPath in pluginAssemblies)
        {
            try
            {
                await LoadPluginAsync(assemblyPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load plugin from '{AssemblyPath}'", assemblyPath);
            }
        }

        _logger.LogInformation("Successfully loaded {LoadedCount} plugins", _loadedPlugins.Count);
    }

    /// <summary>
    /// Loads a specific plugin assembly.
    /// </summary>
    /// <param name="assemblyPath">The path to the plugin assembly.</param>
    /// <returns>A task representing the loading operation.</returns>
    private async Task LoadPluginAsync(string assemblyPath)
    {
        _logger.LogDebug("Loading plugin from '{AssemblyPath}'", assemblyPath);

        var loadContext = new PluginLoadContext(assemblyPath);
        _loadContexts.Add(loadContext);

        var assembly = loadContext.LoadFromAssemblyPath(assemblyPath);
        
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
                
                _logger.LogInformation("Loaded plugin: {ModuleId} ({DisplayName}) v{Version}", 
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
    /// Shuts down all loaded plugins and unloads their contexts.
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

        foreach (var context in _loadContexts)
        {
            try
            {
                context.Unload();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unloading plugin context");
            }
        }

        _loadContexts.Clear();
    }
}