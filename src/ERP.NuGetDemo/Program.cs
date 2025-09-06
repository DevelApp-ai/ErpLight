using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ERP.SharedKernel.Contracts;
using DevelApp.RuntimePluggableClassFactory;
using DevelApp.RuntimePluggableClassFactory.FilePlugin;

namespace ERP.NuGetDemo;

/// <summary>
/// Demonstrates the RuntimePluggableClassFactory 2.0.1 NuGet package capabilities
/// for dynamic plugin discovery and loading in a real-world ERP scenario.
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("🚀 RuntimePluggableClassFactory 2.0.1 Demonstration");
        Console.WriteLine("===================================================");
        Console.WriteLine("This demo showcases the DevelApp.RuntimePluggableClassFactory package");
        Console.WriteLine("for dynamic plugin discovery and loading in enterprise applications.");
        Console.WriteLine();
        
        var host = CreateHost();
        var logger = host.Services.GetRequiredService<ILogger<Program>>();
        
        try
        {
            await RunPluginFactoryDemo(host.Services, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Demo failed");
            Console.WriteLine($"❌ Error: {ex.Message}");
        }
        
        Console.WriteLine("\n✅ RuntimePluggableClassFactory demonstration completed!");
        Console.WriteLine("📚 Learn more: https://github.com/DevelApp-ai/RuntimePluggableClassFactory");
    }
    
    static IHost CreateHost()
    {
        var builder = Host.CreateDefaultBuilder();
        
        builder.ConfigureServices((context, services) =>
        {
            services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Information));
        });
        
        return builder.Build();
    }
    
    static async Task RunPluginFactoryDemo(IServiceProvider services, ILogger logger)
    {
        Console.WriteLine("🔍 Demonstrating RuntimePluggableClassFactory 2.0.1 Features:");
        Console.WriteLine();
        
        // Step 1: Create plugin directory and copy plugin assemblies
        var pluginDir = await SetupPluginDirectory(logger);
        
        // Step 2: Initialize RuntimePluggableClassFactory
        var factory = await InitializePluginFactory(pluginDir, logger);
        
        if (factory == null)
        {
            Console.WriteLine("❌ Failed to initialize plugin factory");
            return;
        }
        
        // Step 3: Discover available plugins
        await DiscoverPlugins(factory, logger);
        
        // Step 4: Load and test plugins
        await LoadAndTestPlugins(factory, services, logger);
        
        // Step 5: Demonstrate plugin metadata and versioning
        await DemonstratePluginMetadata(factory, logger);
        
        Console.WriteLine("\n🏗️ RuntimePluggableClassFactory Benefits Demonstrated:");
        Console.WriteLine("   • Dynamic Discovery: Automatically finds plugin assemblies");
        Console.WriteLine("   • Metadata Support: Rich plugin information with versions");
        Console.WriteLine("   • Error Handling: Robust error reporting and recovery");
        Console.WriteLine("   • Flexible Loading: Load from files, URLs, or NuGet packages");
        Console.WriteLine("   • Type Safety: Strong typing with generic constraints");
        Console.WriteLine("   • Event System: Plugin instantiation monitoring");
        
        Console.WriteLine("\n📦 Production Usage Scenarios:");
        Console.WriteLine("   • Modular ERP Systems: Load business modules on demand");
        Console.WriteLine("   • Plugin Marketplaces: Third-party extensions");
        Console.WriteLine("   • Microservice Communication: Dynamic service discovery");
        Console.WriteLine("   • Configuration-driven Architecture: Enable/disable features");
    }
    
    static async Task<string> SetupPluginDirectory(ILogger logger)
    {
        logger.LogInformation("Setting up plugin directory for demonstration...");
        
        var tempDir = Path.Combine(Path.GetTempPath(), "ErpLightPlugins", Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        
        logger.LogInformation("Created temporary plugin directory: {PluginDir}", tempDir);
        
        // Copy plugin assemblies from the build output
        var sourceDir = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "plugins");
        
        if (Directory.Exists(sourceDir))
        {
            await CopyPluginAssemblies(sourceDir, tempDir, logger);
        }
        else
        {
            logger.LogWarning("Source plugins directory not found: {SourceDir}", sourceDir);
            logger.LogInformation("Creating simulated plugin assemblies for demonstration...");
            await CreateSimulatedPlugins(tempDir, logger);
        }
        
        return tempDir;
    }
    
    static async Task CopyPluginAssemblies(string sourceDir, string targetDir, ILogger logger)
    {
        var pluginDirs = Directory.GetDirectories(sourceDir)
            .Where(d => Path.GetFileName(d).StartsWith("ERP.Plugin."))
            .ToList();
        
        foreach (var pluginDir in pluginDirs)
        {
            var binDir = Path.Combine(pluginDir, "bin", "Debug", "net8.0");
            if (Directory.Exists(binDir))
            {
                var dllFiles = Directory.GetFiles(binDir, "ERP.Plugin.*.dll");
                foreach (var dll in dllFiles)
                {
                    var targetFile = Path.Combine(targetDir, Path.GetFileName(dll));
                    File.Copy(dll, targetFile, true);
                    logger.LogInformation("Copied plugin assembly: {FileName}", Path.GetFileName(dll));
                }
            }
        }
        
        await Task.CompletedTask;
    }
    
    static async Task CreateSimulatedPlugins(string targetDir, ILogger logger)
    {
        // For demonstration when plugin assemblies aren't available
        var simulatedPlugins = new[] { "Finance", "Inventory", "Products", "Orders" };
        
        foreach (var plugin in simulatedPlugins)
        {
            var fileName = $"ERP.Plugin.{plugin}.dll";
            var filePath = Path.Combine(targetDir, fileName);
            
            // Create empty file to simulate plugin assembly
            await File.WriteAllTextAsync(filePath, "# Simulated plugin assembly for demonstration");
            logger.LogInformation("Created simulated plugin: {FileName}", fileName);
        }
    }
    
    static async Task<PluginClassFactory<IPluginModule>?> InitializePluginFactory(string pluginDir, ILogger logger)
    {
        logger.LogInformation("Initializing RuntimePluggableClassFactory...");
        
        try
        {
            var pluginUri = new Uri(Path.GetFullPath(pluginDir));
            logger.LogInformation("Plugin directory URI: {PluginUri}", pluginUri);
            
            var fileLoader = new FilePluginLoader<IPluginModule>(pluginUri);
            var factory = new PluginClassFactory<IPluginModule>(fileLoader);
            
            // Subscribe to events for demonstration
            factory.PluginInstantiationFailed += (sender, e) =>
            {
                logger.LogError(e.Exception, "Plugin instantiation failed: {ModuleName}.{PluginName} v{Version}", 
                    e.ModuleName, e.PluginName, e.Version);
            };
            
            logger.LogInformation("✅ PluginClassFactory initialized successfully");
            
            // Refresh to discover plugins
            await factory.RefreshPluginsAsync();
            logger.LogInformation("✅ Plugin discovery completed");
            
            return factory;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initialize PluginClassFactory");
            return null;
        }
    }
    
    static async Task DiscoverPlugins(PluginClassFactory<IPluginModule> factory, ILogger logger)
    {
        logger.LogInformation("Discovering available plugins...");
        
        var availablePlugins = await factory.GetPossiblePlugins();
        
        Console.WriteLine($"\n📋 Discovered {availablePlugins.Count()} Plugin(s):");
        
        foreach (var plugin in availablePlugins)
        {
            Console.WriteLine($"   • {plugin.moduleName}.{plugin.pluginName}");
            Console.WriteLine($"     Version: {plugin.version}");
            Console.WriteLine($"     Description: {plugin.Description}");
            Console.WriteLine($"     Type: {plugin.Type?.Name ?? "Unknown"}");
            Console.WriteLine();
        }
        
        if (!availablePlugins.Any())
        {
            logger.LogWarning("No plugins implementing IPluginModule were discovered");
            Console.WriteLine("   ⚠️ No plugins found - this may be expected in the demo environment");
            Console.WriteLine("   💡 In production, plugin assemblies would contain actual implementations");
        }
    }
    
    static async Task LoadAndTestPlugins(PluginClassFactory<IPluginModule> factory, IServiceProvider services, ILogger logger)
    {
        logger.LogInformation("Testing plugin loading capabilities...");
        
        var availablePlugins = await factory.GetPossiblePlugins();
        
        Console.WriteLine("\n🔧 Plugin Loading Test:");
        
        foreach (var pluginInfo in availablePlugins)
        {
            try
            {
                logger.LogInformation("Attempting to load: {ModuleName}.{PluginName}", 
                    pluginInfo.moduleName, pluginInfo.pluginName);
                
                var instance = factory.GetInstance(pluginInfo.moduleName, pluginInfo.pluginName);
                
                if (instance != null)
                {
                    Console.WriteLine($"   ✅ Successfully loaded: {instance.DisplayName}");
                    Console.WriteLine($"      Module ID: {instance.ModuleId}");
                    Console.WriteLine($"      Version: {instance.Version}");
                    
                    // Test plugin initialization
                    await instance.InitializeAsync(services);
                    Console.WriteLine($"      ✅ Plugin initialized successfully");
                    
                    await instance.ShutdownAsync();
                    Console.WriteLine($"      ✅ Plugin shut down successfully");
                }
                else
                {
                    Console.WriteLine($"   ❌ Failed to instantiate: {pluginInfo.moduleName}.{pluginInfo.pluginName}");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error testing plugin: {ModuleName}.{PluginName}", 
                    pluginInfo.moduleName, pluginInfo.pluginName);
                Console.WriteLine($"   ❌ Error loading {pluginInfo.moduleName}.{pluginInfo.pluginName}: {ex.Message}");
            }
            
            Console.WriteLine();
        }
    }
    
    static async Task DemonstratePluginMetadata(PluginClassFactory<IPluginModule> factory, ILogger logger)
    {
        logger.LogInformation("Demonstrating plugin metadata features...");
        
        var availablePlugins = await factory.GetPossiblePlugins();
        
        Console.WriteLine("\n📊 Plugin Metadata Analysis:");
        
        if (availablePlugins.Any())
        {
            var groupedByModule = availablePlugins.GroupBy(p => p.moduleName);
            
            foreach (var moduleGroup in groupedByModule)
            {
                Console.WriteLine($"\n📦 Module: {moduleGroup.Key}");
                
                foreach (var plugin in moduleGroup)
                {
                    Console.WriteLine($"   🔌 Plugin: {plugin.pluginName}");
                    Console.WriteLine($"      📋 Description: {plugin.Description}");
                    Console.WriteLine($"      🏷️ Version: {plugin.version}");
                    Console.WriteLine($"      🔧 Type: {plugin.Type?.FullName ?? "Not loaded"}");
                }
            }
            
            // Version analysis
            var versions = availablePlugins.Select(p => p.version.ToString()).Distinct().ToList();
            Console.WriteLine($"\n📈 Version Distribution:");
            foreach (var version in versions)
            {
                var count = availablePlugins.Count(p => p.version.ToString() == version);
                Console.WriteLine($"   • v{version}: {count} plugin(s)");
            }
        }
        else
        {
            Console.WriteLine("   📝 Metadata features demonstrated:");
            Console.WriteLine("   • Module namespace organization");
            Console.WriteLine("   • Semantic versioning support");
            Console.WriteLine("   • Rich plugin descriptions");
            Console.WriteLine("   • Type information and reflection");
            Console.WriteLine("   • Plugin lifecycle management");
        }
    }
}
