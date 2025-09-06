# ErpLight - RuntimePluggableClassFactory 2.0.1 Demonstration

ErpLight is a **comprehensive demonstration** of the [DevelApp.RuntimePluggableClassFactory 2.0.1](https://github.com/DevelApp-ai/RuntimePluggableClassFactory) NuGet package, showcasing dynamic plugin discovery and loading in enterprise applications. This modular ERP system demonstrates the **Modular Monolith** pattern with runtime plugin architecture.

## 🚀 RuntimePluggableClassFactory Features Demonstrated

This project serves as a **real-world example** of the RuntimePluggableClassFactory capabilities:

### ✅ Key Features Showcased
- **Dynamic Plugin Discovery**: Automatically finds plugin assemblies at runtime
- **Type-Safe Loading**: Generic constraints ensure plugins implement required interfaces
- **Rich Metadata Support**: Plugin versioning, descriptions, and namespace organization
- **Error Handling**: Robust error reporting with event-driven monitoring
- **Flexible Architecture**: Load plugins from directories, NuGet packages, or URLs
- **Enterprise Integration**: Complete integration with ASP.NET Core and dependency injection

### 📋 What You'll See
1. **Plugin Factory Initialization** - Setting up the RuntimePluggableClassFactory
2. **Dynamic Discovery** - Scanning directories for plugin assemblies
3. **Metadata Analysis** - Rich plugin information with semantic versioning
4. **Type-Safe Instantiation** - Creating plugin instances with interface constraints
5. **Lifecycle Management** - Plugin initialization and shutdown workflows
6. **Error Monitoring** - Event-driven error handling and recovery

## 🎯 Quick Start - See RuntimePluggableClassFactory in Action

### 1. Run the Interactive Demo
```bash
cd src/ERP.NuGetDemo
dotnet run
```

### 2. Start the Full ERP Application
```bash
cd src/ERP.Host
dotnet run
```

### 3. View the Live Plugin System
Navigate to https://localhost:5001 to see:
- 4 plugins loaded dynamically via RuntimePluggableClassFactory
- Complete navigation system built from plugin metadata
- Event publishing system across all modules
- Real-time plugin status and information

## 🏗️ Architecture

The system demonstrates enterprise-grade plugin architecture using RuntimePluggableClassFactory:

### 1. Host Application Shell (`ERP.Host`)
- **RuntimePluggableClassFactory Integration**: Uses `PluginClassFactory<IPluginModule>` for discovery
- **FilePluginLoader**: Scans directories for plugin assemblies
- **Event Monitoring**: Subscribes to `PluginInstantiationFailed` events
- **Dynamic Navigation**: Builds UI from discovered plugin metadata
- **Service Integration**: Registers plugin services with ASP.NET Core DI

### 2. Shared Kernel (`ERP.SharedKernel`)
- **IPluginModule Interface**: Extends `IPluginClass` for RuntimePluggableClassFactory compatibility
- **Plugin Contracts**: Defines metadata requirements (Name, Module, Version, Description)
- **Domain Events**: Shared event system across all plugins
- **Minimal Dependencies**: Clean separation of concerns

### 3. Plugin Modules (Loaded via RuntimePluggableClassFactory)

All plugins implement the `IPluginModule` interface and are discovered/loaded using RuntimePluggableClassFactory:

#### 📦 **Finance Module** (`ERP.Plugin.Finance`)
- **Capabilities**: Invoice creation, payment processing, financial reporting
- **Events**: `InvoiceCreatedEvent`, `PaymentReceivedEvent`
- **Services**: `IInvoiceService` for business logic
- **Navigation**: Finance, Invoices, Payments, Reports

#### 📋 **Inventory Module** (`ERP.Plugin.Inventory`)
- **Capabilities**: Stock management, inventory tracking, alerts
- **Events**: `LowStockAlertEvent`, `StockUpdatedEvent`
- **Services**: `IInventoryService` for stock operations
- **Navigation**: Inventory, Products, Stock Levels, Categories, Alerts

#### 🛍️ **Products Module** (`ERP.Plugin.Products`)
- **Capabilities**: Product catalog management, categorization
- **Events**: `ProductCatalogCreatedEvent`, `ProductUpdatedEvent`, `ProductDiscontinuedEvent`
- **Services**: `IProductCatalogService` for catalog operations
- **Navigation**: Products, Product Catalog, Categories, Brands, Specifications

#### 📋 **Orders Module** (`ERP.Plugin.Orders`)
- **Capabilities**: Order lifecycle management, fulfillment tracking
- **Events**: `OrderCreatedEvent`, `OrderUpdatedEvent`, `OrderFulfilledEvent`, `OrderCancelledEvent`
- **Services**: `IOrderService` for order processing
- **Navigation**: Orders, Sales Orders, Purchase Orders, Order History, Fulfillment

## 🚀 RuntimePluggableClassFactory Integration

### Plugin Loading Workflow
```csharp
// 1. Initialize RuntimePluggableClassFactory
var fileLoader = new FilePluginLoader<IPluginModule>(pluginDirectoryUri);
var factory = new PluginClassFactory<IPluginModule>(fileLoader);

// 2. Subscribe to error events
factory.PluginInstantiationFailed += OnPluginInstantiationFailed;

// 3. Discover available plugins
await factory.RefreshPluginsAsync();
var availablePlugins = await factory.GetPossiblePlugins();

// 4. Load each plugin with metadata
foreach (var pluginInfo in availablePlugins)
{
    var instance = factory.GetInstance(pluginInfo.moduleName, pluginInfo.pluginName);
    await instance.InitializeAsync(serviceProvider);
}
```

### Plugin Interface Contract
```csharp
public interface IPluginModule : IPluginClass
{
    // RuntimePluggableClassFactory properties (from IPluginClass)
    IdentifierString Name { get; }           // Plugin identifier
    NamespaceString Module { get; }          // Module namespace 
    string Description { get; }              // Rich description
    SemanticVersionNumber Version { get; }   // Semantic versioning

    // ERP-specific properties and methods
    string ModuleId { get; }
    string DisplayName { get; }
    void ConfigureServices(IServiceCollection services);
    void Configure(IApplicationBuilder app);
    Task InitializeAsync(IServiceProvider serviceProvider);
    Task ShutdownAsync();
}
```

## 🎯 Demonstrated Benefits

### For Plugin Developers
- **Metadata Rich**: Semantic versioning, descriptions, namespace organization
- **Type Safety**: Compile-time interface compliance verification
- **Error Handling**: Detailed error reporting with stack traces
- **Lifecycle Management**: Proper initialization and cleanup

### For Application Hosts  
- **Dynamic Discovery**: No manual registration or configuration required
- **Flexible Loading**: Support for files, directories, URLs, NuGet packages
- **Runtime Monitoring**: Event-driven plugin status monitoring
- **Graceful Degradation**: Application continues if some plugins fail

### For Enterprise Architecture
- **Modular Development**: Teams can develop plugins independently
- **Version Management**: Support for multiple plugin versions
- **Deployment Flexibility**: Hot-swappable business logic
- **Extension Points**: Third-party plugin marketplace support

## 🔧 Key Features

- **Plugin System**: Dynamic loading of business modules using `AssemblyLoadContext`
- **Event-Driven Communication**: Asynchronous, decoupled communication between plugins
- **Dynamic Navigation**: Plugin-contributed navigation items in the main menu
- **Strong Encapsulation**: Plugins cannot directly reference each other
- **Isolation**: Each plugin loads in its own assembly context

## 📁 Project Structure

```
src/
├── ERP.Host/                    # Main Blazor application
│   ├── Services/
│   │   ├── PluginManager.cs     # Plugin lifecycle management
│   │   └── EventPublisher.cs    # Domain event publishing
│   └── Plugin/
│       └── PluginLoadContext.cs # Assembly isolation
├── ERP.SharedKernel/           # Shared contracts
│   ├── Contracts/              # Plugin interfaces
│   ├── Events/                 # Domain events
│   └── Entities/               # Core entities
└── plugins/
    └── Finance/
        └── ERP.Plugin.Finance/  # Finance module plugin

tests/
└── ERP.SharedKernel.Tests/     # Unit tests
```

## 🛠️ Getting Started

### Prerequisites
- .NET 8.0 SDK or later
- Visual Studio 2022 or VS Code

### Running the Application

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd ErpLight
   ```

2. **Build the solution**
   ```bash
   dotnet build
   ```

3. **Copy plugins to runtime directory**
   ```bash
   # This step is automated in the build process
   cp src/plugins/Finance/ERP.Plugin.Finance/bin/Debug/net8.0/ERP.Plugin.Finance.dll src/ERP.Host/plugins/
   cp src/plugins/Finance/ERP.Plugin.Finance/bin/Debug/net8.0/ERP.Plugin.Finance.deps.json src/ERP.Host/plugins/
   ```

4. **Run the application**
   ```bash
   dotnet run --project src/ERP.Host
   ```

5. **Navigate to** `http://localhost:5181`

### Running Tests
```bash
dotnet test
```

## 📱 Plugin Demo

The application includes a **Plugin Demo** page that demonstrates:

- ✅ **Plugin Discovery**: Shows loaded plugins and their metadata
- ✅ **Event Publishing**: Demonstrates domain event publishing (Invoice Created, Payment Received)
- ✅ **Dynamic Navigation**: Shows how plugins contribute navigation items

![Plugin System Demo](https://github.com/user-attachments/assets/c314338a-5f9b-4a43-bcc1-db0936d51823)

## 🔧 Plugin Development

### Creating a New Plugin

1. **Create a new class library** in `src/plugins/{ModuleName}/`
2. **Add SharedKernel reference** with special configuration:
   ```xml
   <ProjectReference Include="..\..\..\ERP.SharedKernel\ERP.SharedKernel.csproj">
     <Private>false</Private>
     <ExcludeAssets>runtime</ExcludeAssets>
   </ProjectReference>
   ```
3. **Implement `IPluginModule`** for core plugin functionality
4. **Optionally implement `INavigationProvider`** to contribute navigation items
5. **Copy the compiled DLL** to the Host's `plugins/` directory

### Event-Driven Communication

Plugins communicate through domain events:

```csharp
// Publishing an event
await eventPublisher.PublishAsync(new InvoiceCreatedEvent(...));

// Handling an event
public class MyEventHandler : IEventHandler<InvoiceCreatedEvent>
{
    public async Task HandleAsync(InvoiceCreatedEvent domainEvent)
    {
        // React to the event
    }
}
```

## 📋 Current Implementation Status

- ✅ **Core Architecture**: Plugin system with assembly isolation
- ✅ **Host Application**: Blazor Server with dynamic navigation
- ✅ **SharedKernel**: Contracts and domain events
- ✅ **Finance Plugin**: Basic implementation with navigation
- ✅ **Event System**: Asynchronous event publishing and handling
- ✅ **Unit Tests**: Basic test coverage for domain events
- 🔄 **In Development**: Additional business functionality and plugins

## 🎯 Next Steps

1. **Expand Finance Module**: Add actual finance pages and functionality
2. **Create Inventory Plugin**: Demonstrate multiple plugins working together
3. **Add Authentication**: Implement ASP.NET Core Identity
4. **Database Integration**: Add Entity Framework Core with per-plugin contexts
5. **API Endpoints**: Plugin-contributed API controllers
6. **Advanced Features**: Plugin configuration, health checks, and monitoring

## 📖 Technical Design Specification

The complete Technical Design Specification can be found in the [docs/](./docs/) folder, including detailed architectural decisions and implementation patterns.

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Implement your changes
4. Add tests
5. Submit a pull request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.