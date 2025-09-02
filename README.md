# ErpLight - Modular ERP System

ErpLight is a modular Enterprise Resource Planning (ERP) system built with .NET 8 and a plugin architecture. It demonstrates the **Modular Monolith** pattern, where business capabilities are organized into independently developed plugins that are deployed together as a single application.

## 🏗️ Architecture

The system consists of three main components:

### 1. Host Application Shell (`ERP.Host`)
- ASP.NET Core 8 Blazor Server application
- Manages plugin lifecycle (discovery, loading, initialization)
- Provides core services (logging, configuration, authentication)
- Renders the main UI shell with dynamic navigation

### 2. Shared Kernel (`ERP.SharedKernel`)
- Contains contracts and interfaces for plugin communication
- Defines core entities and domain events
- Minimal by design to prevent tight coupling

### 3. Plugin Modules
- Self-contained business capability modules
- Currently implemented: **Finance Module**
- Each plugin can contribute navigation items, services, and UI components

## 🚀 Key Features

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