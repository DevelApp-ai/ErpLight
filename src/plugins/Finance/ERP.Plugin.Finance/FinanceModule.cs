using ERP.SharedKernel.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using DevelApp.Utility.Model;
using ERP.Plugin.Finance.Services;

namespace ERP.Plugin.Finance;

/// <summary>
/// Finance module plugin that provides financial management capabilities.
/// </summary>
public class FinanceModule : IPluginModule, INavigationProvider
{
    // IPluginModule properties (for backward compatibility)
    public string ModuleId => "Finance";
    public string DisplayName => "Finance Module";
    
    // IPluginClass properties (required by RuntimePluggableClassFactory)
    public IdentifierString Name => "FinanceModule";
    public NamespaceString Module => "Finance";
    public string Description => "Finance Module - Provides financial management capabilities including invoices, payments, and reports";
    public SemanticVersionNumber Version => "1.0.0";

    public void ConfigureServices(IServiceCollection services)
    {
        // Register finance-specific services
        services.AddScoped<IInvoiceService, InvoiceService>();
    }

    public void Configure(IApplicationBuilder app)
    {
        // Configure finance-specific middleware or routes here
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
                Text = "Finance",
                Href = "/finance",
                Icon = "bi bi-cash-stack",
                Order = 10,
                Children = new List<NavigationItem>
                {
                    new NavigationItem
                    {
                        Text = "Invoices",
                        Href = "/finance/invoices",
                        Icon = "bi bi-receipt",
                        Order = 1
                    },
                    new NavigationItem
                    {
                        Text = "Payments",
                        Href = "/finance/payments",
                        Icon = "bi bi-credit-card",
                        Order = 2
                    },
                    new NavigationItem
                    {
                        Text = "Reports",
                        Href = "/finance/reports",
                        Icon = "bi bi-graph-up",
                        Order = 3
                    }
                }
            }
        };
    }
}