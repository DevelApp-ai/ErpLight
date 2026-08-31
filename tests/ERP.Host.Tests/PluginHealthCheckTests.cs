using ERP.Host.Services;
using ERP.SharedKernel.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ERP.Host.Tests;

/// <summary>
/// Tests for plugin health check functionality.
/// </summary>
public class PluginHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_ShouldReturnUnhealthy_WhenNoPluginsLoaded()
    {
        // Arrange
        var pluginManager = new PluginManager(
            NullLogger<PluginManager>.Instance,
            Mock.Of<IServiceProvider>(),
            Mock.Of<IConfiguration>());

        var healthCheck = new PluginSystemHealthCheck(pluginManager);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Contains("No plugins are loaded", result.Description);
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldReturnHealthy_WhenPluginsLoaded()
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

        // Create mock plugins that are healthy
        var mockPlugin1 = new Mock<IPluginModule>();
        mockPlugin1.Setup(p => p.ModuleId).Returns("TestModule1");
        mockPlugin1.Setup(p => p.DisplayName).Returns("Test Module 1");
        mockPlugin1.Setup(p => p.CheckHealthAsync()).ReturnsAsync(HealthStatus.Healthy);

        var mockPlugin2 = new Mock<IPluginModule>();
        mockPlugin2.Setup(p => p.ModuleId).Returns("TestModule2");
        mockPlugin2.Setup(p => p.DisplayName).Returns("Test Module 2");
        mockPlugin2.Setup(p => p.CheckHealthAsync()).ReturnsAsync(HealthStatus.Healthy);

        // Manually add plugins to the manager
        var loadedPlugins = new List<IPluginModule> { mockPlugin1.Object, mockPlugin2.Object };
        typeof(PluginManager).GetField("_loadedPlugins", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(pluginManager, loadedPlugins);

        var healthCheck = new PluginSystemHealthCheck(pluginManager);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Contains("Plugin system is ready", result.Description);
        Assert.Equal(2, result.Data["LoadedPlugins"]);
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldReturnDegraded_WhenAnyPluginUnhealthy()
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

        // Create mock plugins - one healthy, one unhealthy
        var mockPlugin1 = new Mock<IPluginModule>();
        mockPlugin1.Setup(p => p.ModuleId).Returns("HealthyModule");
        mockPlugin1.Setup(p => p.DisplayName).Returns("Healthy Module");
        mockPlugin1.Setup(p => p.CheckHealthAsync()).ReturnsAsync(HealthStatus.Healthy);

        var mockPlugin2 = new Mock<IPluginModule>();
        mockPlugin2.Setup(p => p.ModuleId).Returns("UnhealthyModule");
        mockPlugin2.Setup(p => p.DisplayName).Returns("Unhealthy Module");
        mockPlugin2.Setup(p => p.CheckHealthAsync()).ReturnsAsync(HealthStatus.Unhealthy);

        // Manually add plugins to the manager
        var loadedPlugins = new List<IPluginModule> { mockPlugin1.Object, mockPlugin2.Object };
        typeof(PluginManager).GetField("_loadedPlugins", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(pluginManager, loadedPlugins);

        var healthCheck = new PluginSystemHealthCheck(pluginManager);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        Assert.Equal(HealthStatus.Degraded, result.Status);
        Assert.Contains("1 plugin(s) unhealthy", result.Description);
        Assert.Equal(2, result.Data["LoadedPlugins"]);
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldIncludePluginHealthData()
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

        // Create mock plugins
        var mockPlugin1 = new Mock<IPluginModule>();
        mockPlugin1.Setup(p => p.ModuleId).Returns("Finance");
        mockPlugin1.Setup(p => p.CheckHealthAsync()).ReturnsAsync(HealthStatus.Healthy);

        var mockPlugin2 = new Mock<IPluginModule>();
        mockPlugin2.Setup(p => p.ModuleId).Returns("Inventory");
        mockPlugin2.Setup(p => p.CheckHealthAsync()).ReturnsAsync(HealthStatus.Healthy);

        // Manually add plugins to the manager
        var loadedPlugins = new List<IPluginModule> { mockPlugin1.Object, mockPlugin2.Object };
        typeof(PluginManager).GetField("_loadedPlugins", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(pluginManager, loadedPlugins);

        var healthCheck = new PluginSystemHealthCheck(pluginManager);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        Assert.True(result.Data.ContainsKey("PluginHealth"));
        var pluginHealth = result.Data["PluginHealth"] as Dictionary<string, HealthStatus>;
        Assert.NotNull(pluginHealth);
        Assert.Equal(2, pluginHealth.Count);
        Assert.Equal(HealthStatus.Healthy, pluginHealth["Finance"]);
        Assert.Equal(HealthStatus.Healthy, pluginHealth["Inventory"]);
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldHandlePluginHealthCheckExceptions()
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

        // Create mock plugin that throws exception on health check
        var mockPlugin = new Mock<IPluginModule>();
        mockPlugin.Setup(p => p.ModuleId).Returns("FaultyModule");
        mockPlugin.Setup(p => p.CheckHealthAsync()).ThrowsAsync(new Exception("Health check failed"));

        // Manually add plugin to the manager
        var loadedPlugins = new List<IPluginModule> { mockPlugin.Object };
        typeof(PluginManager).GetField("_loadedPlugins", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(pluginManager, loadedPlugins);

        var healthCheck = new PluginSystemHealthCheck(pluginManager);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert - Should still complete and mark as unhealthy
        Assert.Equal(HealthStatus.Degraded, result.Status);
    }

    [Fact]
    public void PluginModule_ShouldHaveCheckHealthAsyncMethod()
    {
        // Arrange & Act
        var mockPlugin = new Mock<IPluginModule>();
        mockPlugin.Setup(p => p.CheckHealthAsync()).ReturnsAsync(HealthStatus.Healthy);

        // Assert
        // Verify the method exists on the interface
        var checkHealthMethod = typeof(IPluginModule).GetMethod("CheckHealthAsync");
        Assert.NotNull(checkHealthMethod);
        Assert.Equal(typeof(Task<HealthStatus>), checkHealthMethod.ReturnType);
    }
}
