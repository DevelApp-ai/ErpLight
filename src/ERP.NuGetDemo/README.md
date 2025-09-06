# RuntimePluggableClassFactory 2.0.1 Demonstration

This project demonstrates the capabilities of the [DevelApp.RuntimePluggableClassFactory](https://github.com/DevelApp-ai/RuntimePluggableClassFactory) NuGet package for dynamic plugin discovery and loading in enterprise applications.

## Overview

The RuntimePluggableClassFactory is a powerful .NET library that enables:

- **Dynamic Plugin Discovery**: Automatically find and load plugin assemblies at runtime
- **Metadata Support**: Rich plugin information with versioning and descriptions
- **Type Safety**: Strong typing with generic constraints and interface contracts
- **Flexible Loading**: Load plugins from files, directories, URLs, or NuGet packages
- **Error Handling**: Robust error reporting and recovery mechanisms
- **Event System**: Monitor plugin instantiation and lifecycle events

## What This Demo Shows

### 1. Plugin Factory Initialization
```csharp
var fileLoader = new FilePluginLoader<IPluginModule>(pluginUri);
var factory = new PluginClassFactory<IPluginModule>(fileLoader);
```

### 2. Plugin Discovery
```csharp
await factory.RefreshPluginsAsync();
var availablePlugins = await factory.GetPossiblePlugins();
```

### 3. Plugin Loading
```csharp
var instance = factory.GetInstance(moduleName, pluginName);
await instance.InitializeAsync(serviceProvider);
```

### 4. Error Handling
```csharp
factory.PluginInstantiationFailed += (sender, e) =>
{
    logger.LogError(e.Exception, "Plugin failed: {ModuleName}.{PluginName}", 
        e.ModuleName, e.PluginName);
};
```

## Plugin Interface Contract

All plugins implement the `IPluginModule` interface which extends `IPluginClass`:

```csharp
public interface IPluginModule : IPluginClass
{
    string ModuleId { get; }
    string DisplayName { get; }
    void ConfigureServices(IServiceCollection services);
    void Configure(IApplicationBuilder app);
    Task InitializeAsync(IServiceProvider serviceProvider);
    Task ShutdownAsync();
}
```

Required `IPluginClass` properties:
- `IdentifierString Name` - Plugin identifier
- `NamespaceString Module` - Module namespace
- `string Description` - Plugin description
- `SemanticVersionNumber Version` - Semantic version

## Running the Demo

```bash
cd src/ERP.NuGetDemo
dotnet run
```

The demo will:

1. **Setup Plugin Directory**: Create temporary directory and copy plugin assemblies
2. **Initialize Factory**: Create PluginClassFactory with FilePluginLoader
3. **Discover Plugins**: Find all available plugins implementing IPluginModule
4. **Load and Test**: Instantiate plugins and test their lifecycle
5. **Analyze Metadata**: Display plugin information, versions, and descriptions

## Expected Output

```
🚀 RuntimePluggableClassFactory 2.0.1 Demonstration
===================================================
This demo showcases the DevelApp.RuntimePluggableClassFactory package
for dynamic plugin discovery and loading in enterprise applications.

🔍 Demonstrating RuntimePluggableClassFactory 2.0.1 Features:

📋 Discovered 4 Plugin(s):
   • Finance.FinanceModule
     Version: 1.0.0
     Description: Finance Module - Provides financial management capabilities
     Type: ERP.Plugin.Finance.FinanceModule

   • Inventory.InventoryModule
     Version: 1.0.0
     Description: Inventory Module - Provides stock management capabilities
     Type: ERP.Plugin.Inventory.InventoryModule

🔧 Plugin Loading Test:
   ✅ Successfully loaded: Finance Module
      Module ID: Finance
      Version: 1.0.0
      ✅ Plugin initialized successfully
      ✅ Plugin shut down successfully
```

## Production Usage

In production scenarios, you would:

1. **Distribute plugins as NuGet packages**:
   ```bash
   dotnet add package ERP.Plugin.Finance
   dotnet add package ERP.Plugin.Inventory
   ```

2. **Load plugins from package directories**:
   ```csharp
   var packageDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), 
       ".nuget", "packages");
   var fileLoader = new FilePluginLoader<IPluginModule>(new Uri(packageDir));
   ```

3. **Configure plugin discovery paths**:
   ```json
   {
     "PluginSettings": {
       "DiscoveryPaths": [
         "~/plugins",
         "~/.nuget/packages",
         "/opt/company/plugins"
       ]
     }
   }
   ```

## Benefits Demonstrated

- **Modular Architecture**: Plugins are completely independent assemblies
- **Runtime Discovery**: No compile-time dependencies on plugin implementations
- **Version Management**: Support for semantic versioning and compatibility checks
- **Error Isolation**: Plugin failures don't crash the host application
- **Metadata Rich**: Detailed plugin information for management and diagnostics
- **Flexible Deployment**: Plugins can be deployed independently of the host

## Integration with ERP System

The main ERP Host application (`ERP.Host`) uses this same RuntimePluggableClassFactory to load business modules:

- **Finance Module**: Invoice and payment management
- **Inventory Module**: Stock tracking and alerts  
- **Products Module**: Product catalog management
- **Orders Module**: Order lifecycle management

Each module is discovered and loaded dynamically, allowing for:
- Hot-swappable business logic
- Third-party extensions
- Feature toggles through configuration
- Independent module updates

## Learn More

- [RuntimePluggableClassFactory GitHub](https://github.com/DevelApp-ai/RuntimePluggableClassFactory)
- [NuGet Package](https://www.nuget.org/packages/DevelApp.RuntimePluggableClassFactory/)
- [Plugin Architecture Best Practices](https://docs.microsoft.com/en-us/dotnet/core/tutorials/creating-app-with-plugin-support)