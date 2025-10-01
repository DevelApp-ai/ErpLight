using ERP.SharedKernel.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using DevelApp.Utility.Model;
using ERP.Plugin.Orders.Services;

namespace ERP.Plugin.Orders;

/// <summary>
/// Orders module plugin that provides order management capabilities.
/// </summary>
public class OrdersModule : IPluginModule, INavigationProvider
{
    // IPluginModule properties (for backward compatibility)
    public string ModuleId => "Orders";
    public string DisplayName => "Orders Module";
    
    // IPluginClass properties (required by RuntimePluggableClassFactory)
    public IdentifierString Name => "OrdersModule";
    public NamespaceString Module => "Orders";
    public string Description => "Orders Module - Provides order management capabilities including sales orders, purchase orders, order fulfillment, and order history";
    public SemanticVersionNumber Version => "1.0.0";

    public void ConfigureServices(IServiceCollection services)
    {
        // Register order-specific services
        services.AddScoped<IOrderService, OrderService>();
    }

    public void Configure(IApplicationBuilder app)
    {
        // Configure order-specific middleware or routes here
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
                Text = "Orders",
                Href = "/orders",
                Icon = "bi bi-clipboard-check",
                Order = 40,
                Children = new List<NavigationItem>
                {
                    new NavigationItem
                    {
                        Text = "Sales Orders",
                        Href = "/orders/sales",
                        Icon = "bi bi-cart-check",
                        Order = 1
                    },
                    new NavigationItem
                    {
                        Text = "Purchase Orders",
                        Href = "/orders/purchase",
                        Icon = "bi bi-bag-check",
                        Order = 2
                    },
                    new NavigationItem
                    {
                        Text = "Order History",
                        Href = "/orders/history",
                        Icon = "bi bi-clock-history",
                        Order = 3
                    },
                    new NavigationItem
                    {
                        Text = "Fulfillment",
                        Href = "/orders/fulfillment",
                        Icon = "bi bi-truck",
                        Order = 4
                    }
                }
            }
        };
    }
}