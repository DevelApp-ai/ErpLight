# ErpLight Documentation

This folder contains the technical documentation for the ErpLight modular ERP system.

## Technical Design Specification (TDS)

The complete Technical Design Specification for the modular ERP system with .NET 9 plugin architecture can be found in:
- [A Modular ERP System with a.NET 9 Plugin Architecture.docx](./A%20Modular%20ERP%20System%20with%20a.NET%209%20Plugin%20Architecture.docx)

## Architecture Overview

ErpLight is built using a modular monolith pattern with the following key components:

1. **Host Application Shell** - ASP.NET Core 9 Blazor Web App that manages plugin lifecycle
2. **Shared Kernel** - Common contracts and interfaces for plugin communication
3. **Plugin Modules** - Self-contained business capability modules (Finance, Inventory, etc.)

## Project Structure

```
src/
├── ERP.Host/                    # Main host application
├── ERP.SharedKernel/           # Shared contracts and interfaces
├── plugins/
│   ├── Finance/
│   │   └── ERP.Plugin.Finance/ # Finance module plugin
│   └── Inventory/
│       └── ERP.Plugin.Inventory/ # Inventory module plugin
└── tests/
    ├── ERP.Host.Tests/
    ├── ERP.SharedKernel.Tests/
    └── ERP.Plugin.Finance.Tests/
```

## Getting Started

1. Clone the repository
2. Ensure .NET 9 SDK is installed
3. Build the solution: `dotnet build`
4. Run the host application: `dotnet run --project src/ERP.Host`