# ErpLight - NuGet Package Guide

This document explains how to use ErpLight plugins as NuGet packages for modular ERP development.

## 🚀 Quick Start

### Install Core Package
```bash
dotnet add package ERP.SharedKernel
```

### Install Plugin Packages
```bash
# Finance module
dotnet add package ERP.Plugin.Finance

# Inventory management
dotnet add package ERP.Plugin.Inventory

# Product catalog
dotnet add package ERP.Plugin.Products

# Order management
dotnet add package ERP.Plugin.Orders
```

## 📦 Available Packages

| Package | Description | Features |
|---------|-------------|----------|
| `ERP.SharedKernel` | Core domain events and interfaces | DomainEvent, IDomainEventHandler, base classes |
| `ERP.Plugin.Finance` | Financial management | Invoices, Payments, Financial Reports |
| `ERP.Plugin.Inventory` | Inventory tracking | Stock levels, Product management, Alerts |
| `ERP.Plugin.Products` | Product catalog | Categories, Brands, Specifications |
| `ERP.Plugin.Orders` | Order lifecycle | Sales orders, Purchase orders, Fulfillment |

## 🏗️ Usage Example

```csharp
using ERP.SharedKernel.Events;
using ERP.Plugin.Finance.Events;
using ERP.Plugin.Inventory.Events;

// Create and publish events
var invoiceEvent = new InvoiceCreatedEvent("INV-001");
var stockEvent = new LowStockAlertEvent("PRODUCT-123");

// Events are automatically handled by registered handlers
```

## 🔧 Plugin Registration

Plugins automatically register their services when referenced:

```csharp
builder.Services.AddErpPlugins(); // Auto-discovers and registers all plugins
```

## 🌍 Cross-Platform Support

All packages support:
- ✅ Windows (.NET 8.0)
- ✅ Linux (.NET 8.0) 
- ✅ macOS (.NET 8.0)

## 📖 Documentation

For detailed documentation and examples, visit the [ErpLight repository](https://github.com/DevelApp-ai/ErpLight).

## 🚀 CI/CD Integration

The packages are automatically built and published through GitHub Actions on every release.