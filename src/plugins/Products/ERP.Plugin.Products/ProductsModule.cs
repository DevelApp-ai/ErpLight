using ERP.SharedKernel.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using DevelApp.Utility.Model;
using ERP.Plugin.Products.Services;

namespace ERP.Plugin.Products;

/// <summary>
/// Products module plugin that provides product catalog management capabilities.
/// </summary>
public class ProductsModule : IPluginModule, INavigationProvider
{
    // IPluginModule properties (for backward compatibility)
    public string ModuleId => "Products";
    public string DisplayName => "Products Module";
    
    // IPluginClass properties (required by RuntimePluggableClassFactory)
    public IdentifierString Name => "ProductsModule";
    public NamespaceString Module => "Products";
    public string Description => "Products Module - Provides product catalog management capabilities including product information, brands, categories, and specifications";
    public SemanticVersionNumber Version => "1.0.0";

    public void ConfigureServices(IServiceCollection services)
    {
        // Register product-specific services
        services.AddScoped<IProductCatalogService, ProductCatalogService>();
    }

    public void Configure(IApplicationBuilder app)
    {
        // Configure product-specific middleware or routes here
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
                Text = "Products",
                Href = "/products",
                Icon = "bi bi-collection",
                Order = 30,
                Children = new List<NavigationItem>
                {
                    new NavigationItem
                    {
                        Text = "Product Catalog",
                        Href = "/products/catalog",
                        Icon = "bi bi-book",
                        Order = 1
                    },
                    new NavigationItem
                    {
                        Text = "Categories",
                        Href = "/products/categories",
                        Icon = "bi bi-grid-3x3-gap",
                        Order = 2
                    },
                    new NavigationItem
                    {
                        Text = "Brands",
                        Href = "/products/brands",
                        Icon = "bi bi-award",
                        Order = 3
                    },
                    new NavigationItem
                    {
                        Text = "Specifications",
                        Href = "/products/specifications",
                        Icon = "bi bi-list-check",
                        Order = 4
                    }
                }
            }
        };
    }
}