using ERP.SharedKernel.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using DevelApp.Utility.Model;
using ERP.Plugin.Inventory.Services;

namespace ERP.Plugin.Inventory;

/// <summary>
/// Inventory module plugin that provides inventory management capabilities.
/// </summary>
public class InventoryModule : IPluginModule, INavigationProvider
{
    // IPluginModule properties (for backward compatibility)
    public string ModuleId => "Inventory";
    public string DisplayName => "Inventory Module";
    
    // IPluginClass properties (required by RuntimePluggableClassFactory)
    public IdentifierString Name => "InventoryModule";
    public NamespaceString Module => "Inventory";
    public string Description => "Inventory Module - Provides inventory management capabilities including products, stock levels, and alerts";
    public SemanticVersionNumber Version => "1.0.0";

    public void ConfigureServices(IServiceCollection services)
    {
        // Register inventory-specific services
        services.AddScoped<IInventoryService, InventoryService>();
    }

    public void Configure(IApplicationBuilder app)
    {
        // Configure inventory-specific middleware or routes here
        // For now, this is a basic implementation
    }

    public async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        // Perform any one-time initialization here
        await Task.CompletedTask;
    }

    public async Task ShutdownAsync()
    {
        // Perform any cleanup here
        await Task.CompletedTask;
    }

    public IEnumerable<NavigationItem> GetNavigationItems()
    {
        return new List<NavigationItem>
        {
            new NavigationItem
            {
                Text = "Inventory",
                Href = "/inventory",
                Icon = "bi bi-box-seam",
                Order = 20,
                Children = new List<NavigationItem>
                {
                    new NavigationItem
                    {
                        Text = "Products",
                        Href = "/inventory/products",
                        Icon = "bi bi-box",
                        Order = 1
                    },
                    new NavigationItem
                    {
                        Text = "Stock Levels",
                        Href = "/inventory/stock",
                        Icon = "bi bi-graph-up-arrow",
                        Order = 2
                    },
                    new NavigationItem
                    {
                        Text = "Categories",
                        Href = "/inventory/categories",
                        Icon = "bi bi-tags",
                        Order = 3
                    },
                    new NavigationItem
                    {
                        Text = "Alerts",
                        Href = "/inventory/alerts",
                        Icon = "bi bi-exclamation-triangle",
                        Order = 4
                    }
                }
            }
        };
    }
}