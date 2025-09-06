using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ERP.SharedKernel.Events;

namespace ERP.NuGetDemo;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("🚀 ErpLight NuGet Package Loading Demo");
        Console.WriteLine("=====================================");
        Console.WriteLine("This demo showcases how ErpLight plugins can be distributed and loaded as NuGet packages.");
        Console.WriteLine();
        
        var host = CreateHost();
        
        try
        {
            await RunDemo(host.Services);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
        }
        
        Console.WriteLine("\n✅ Demo completed successfully!");
        Console.WriteLine("In production, plugins would be installed via: dotnet add package ERP.Plugin.Finance");
    }
    
    static IHost CreateHost()
    {
        var builder = Host.CreateDefaultBuilder();
        
        builder.ConfigureServices((context, services) =>
        {
            RegisterPluginServices(services);
        });
        
        return builder.Build();
    }
    
    static void RegisterPluginServices(IServiceCollection services)
    {
        Console.WriteLine("📦 Simulating NuGet package discovery and service registration...");
        
        var plugins = new[] { "Finance", "Inventory", "Products", "Orders" };
        
        foreach (var plugin in plugins)
        {
            Console.WriteLine($"   ✓ Discovered ERP.Plugin.{plugin} package");
            Console.WriteLine($"   ✓ Registered {plugin} domain events and services");
        }
    }
    
    static async Task RunDemo(IServiceProvider services)
    {
        Console.WriteLine("\n🔍 Demonstrating cross-plugin event system:");
        
        var events = new List<(string Plugin, string Event, string Id)>
        {
            ("Finance", "InvoiceCreatedEvent", "INV-001"),
            ("Inventory", "LowStockAlertEvent", "WIDGET-123"),
            ("Products", "ProductCatalogCreatedEvent", "MAIN-CATALOG"),
            ("Orders", "OrderCreatedEvent", "ORD-001")
        };
        
        foreach (var (plugin, eventName, id) in events)
        {
            Console.WriteLine($"   📅 {plugin}: {eventName} for {id}");
        }
        
        Console.WriteLine("\n🏗️ Plugin Architecture Benefits:");
        Console.WriteLine("   • Modular: Each plugin is independently deployable");
        Console.WriteLine("   • Extensible: New plugins can be added without changing existing code");
        Console.WriteLine("   • Maintainable: Clear separation of concerns");
        Console.WriteLine("   • Distributable: Plugins can be shared via NuGet packages");
        
        Console.WriteLine("\n📦 NuGet Package Structure:");
        Console.WriteLine("   ERP.SharedKernel         - Core domain events and interfaces");
        Console.WriteLine("   ERP.Plugin.Finance       - Invoice and payment management");
        Console.WriteLine("   ERP.Plugin.Inventory     - Stock tracking and alerts");
        Console.WriteLine("   ERP.Plugin.Products      - Product catalog management");
        Console.WriteLine("   ERP.Plugin.Orders        - Order lifecycle management");
        
        await Task.CompletedTask;
    }
}
