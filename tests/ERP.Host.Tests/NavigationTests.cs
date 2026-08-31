using ERP.Host.Services;
using ERP.SharedKernel.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ERP.Host.Tests;

/// <summary>
/// Tests for plugin navigation system integration.
/// </summary>
public class NavigationTests
{
    [Fact]
    public void GetPlugins_ShouldReturnNavigationProviders()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<ILogger<PluginManager>>(NullLogger<PluginManager>.Instance);
        services.AddSingleton<IServiceProvider>(sp => sp);
        services.AddSingleton(Mock.Of<IConfiguration>());
        
        var serviceProvider = services.BuildServiceProvider();
        var pluginManager = new PluginManager(
            NullLogger<PluginManager>.Instance,
            serviceProvider,
            Mock.Of<IConfiguration>());

        // Create mock plugins with navigation
        var mockPlugin1 = new Mock<IPluginModule>();
        mockPlugin1.Setup(p => p.ModuleId).Returns("TestModule1");
        mockPlugin1.Setup(p => p.DisplayName).Returns("Test Module 1");
        
        var mockNavPlugin1 = mockPlugin1.As<INavigationProvider>();
        mockNavPlugin1.Setup(p => p.GetNavigationItems()).Returns(new List<NavigationItem>
        {
            new NavigationItem { Text = "Home", Href = "/", Order = 1 },
            new NavigationItem { Text = "About", Href = "/about", Order = 2 }
        });

        var mockPlugin2 = new Mock<IPluginModule>();
        mockPlugin2.Setup(p => p.ModuleId).Returns("TestModule2");
        mockPlugin2.Setup(p => p.DisplayName).Returns("Test Module 2");
        
        var mockNavPlugin2 = mockPlugin2.As<INavigationProvider>();
        mockNavPlugin2.Setup(p => p.GetNavigationItems()).Returns(new List<NavigationItem>
        {
            new NavigationItem
            {
                Text = "Dashboard",
                Href = "/dashboard",
                Order = 1,
                Children = new List<NavigationItem>
                {
                    new NavigationItem { Text = "Overview", Href = "/dashboard/overview", Order = 1 },
                    new NavigationItem { Text = "Stats", Href = "/dashboard/stats", Order = 2 }
                }
            }
        });

        // Manually add plugins to the manager (bypassing discovery for test)
        var loadedPlugins = new List<IPluginModule> { mockPlugin1.Object, mockPlugin2.Object };
        typeof(PluginManager).GetField("_loadedPlugins", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(pluginManager, loadedPlugins);

        // Act
        var navPlugins = pluginManager.GetPlugins<INavigationProvider>().ToList();

        // Assert
        Assert.Equal(2, navPlugins.Count);
    }

    [Fact]
    public void GetNavigationItems_ShouldReturnAllItemsOrdered()
    {
        // Arrange
        var mockPlugin = new Mock<IPluginModule>();
        var mockNavPlugin = mockPlugin.As<INavigationProvider>();
        
        var navItems = new List<NavigationItem>
        {
            new NavigationItem { Text = "Third", Href = "/third", Order = 3 },
            new NavigationItem { Text = "First", Href = "/first", Order = 1 },
            new NavigationItem { Text = "Second", Href = "/second", Order = 2 }
        };
        
        mockNavPlugin.Setup(p => p.GetNavigationItems()).Returns(navItems);

        // Act
        var result = mockNavPlugin.Object.GetNavigationItems().OrderBy(x => x.Order).ToList();

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("First", result[0].Text);
        Assert.Equal("Second", result[1].Text);
        Assert.Equal("Third", result[2].Text);
    }

    [Fact]
    public void NavigationItem_ShouldSupportNestedChildren()
    {
        // Arrange & Act
        var parentItem = new NavigationItem
        {
            Text = "Parent",
            Href = "/parent",
            Order = 1,
            Icon = "bi bi-folder",
            Children = new List<NavigationItem>
            {
                new NavigationItem { Text = "Child 1", Href = "/parent/child1", Order = 1 },
                new NavigationItem { Text = "Child 2", Href = "/parent/child2", Order = 2 }
            }
        };

        // Assert
        Assert.True(parentItem.Children.Any());
        Assert.Equal(2, parentItem.Children.Count);
        Assert.Equal("Child 1", parentItem.Children[0].Text);
        Assert.Equal("Child 2", parentItem.Children[1].Text);
    }

    [Fact]
    public void NavigationItem_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var item = new NavigationItem();

        // Assert
        Assert.Equal(string.Empty, item.Text);
        Assert.Equal(string.Empty, item.Href);
        Assert.Null(item.Icon);
        Assert.Equal(100, item.Order);
        Assert.False(item.IsActive);
        Assert.NotNull(item.Children);
        Assert.Empty(item.Children);
    }

    [Fact]
    public void NavigationItem_ShouldSupportBootstrapIcons()
    {
        // Arrange & Act
        var item = new NavigationItem
        {
            Text = "Finance",
            Href = "/finance",
            Icon = "bi bi-cash-stack"
        };

        // Assert
        Assert.Equal("bi bi-cash-stack", item.Icon);
    }

    [Fact]
    public void MultiplePlugins_ShouldProvideDistinctNavigationItems()
    {
        // Arrange
        var financeNav = new NavigationItem
        {
            Text = "Finance",
            Href = "/finance",
            Order = 10,
            Icon = "bi bi-cash-stack"
        };

        var inventoryNav = new NavigationItem
        {
            Text = "Inventory",
            Href = "/inventory",
            Order = 20,
            Icon = "bi bi-box-seam"
        };

        var ordersNav = new NavigationItem
        {
            Text = "Orders",
            Href = "/orders",
            Order = 30,
            Icon = "bi bi-clipboard-check"
        };

        var productsNav = new NavigationItem
        {
            Text = "Products",
            Href = "/products",
            Order = 40,
            Icon = "bi bi-collection"
        };

        var allNavItems = new List<NavigationItem> { financeNav, inventoryNav, ordersNav, productsNav };

        // Act
        var ordered = allNavItems.OrderBy(x => x.Order).ToList();

        // Assert
        Assert.Equal(4, ordered.Count);
        Assert.Equal("Finance", ordered[0].Text);
        Assert.Equal("Inventory", ordered[1].Text);
        Assert.Equal("Orders", ordered[2].Text);
        Assert.Equal("Products", ordered[3].Text);
    }
}
