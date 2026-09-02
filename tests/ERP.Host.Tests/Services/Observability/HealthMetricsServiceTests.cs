using ERP.Host.Services;
using ERP.Host.Services.Observability;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ERP.Host.Tests.Services.Observability;

/// <summary>
/// Unit tests for HealthMetricsService.
/// </summary>
public class HealthMetricsServiceTests : IDisposable
{
    private readonly Mock<TelemetryService> _telemetryServiceMock;
    private readonly Mock<PluginManager> _pluginManagerMock;
    private readonly ILogger<HealthMetricsService> _logger;
    private readonly HealthMetricsService _healthMetricsService;

    public HealthMetricsServiceTests()
    {
        _telemetryServiceMock = new Mock<TelemetryService>(
            Mock.Of<IConfiguration>(),
            NullLogger<TelemetryService>.Instance);
        
        _pluginManagerMock = new Mock<PluginManager>(
            NullLogger<PluginManager>.Instance,
            Mock.Of<IServiceProvider>(),
            Mock.Of<IConfiguration>());
        
        _logger = NullLogger<HealthMetricsService>.Instance;
        
        _healthMetricsService = new HealthMetricsService(
            _telemetryServiceMock.Object,
            _pluginManagerMock.Object,
            _logger);
    }

    public void Dispose()
    {
        _telemetryServiceMock?.Invoke();
        _pluginManagerMock?.Invoke();
    }

    [Fact]
    public void RecordHealthCheck_ShouldNotThrow_WhenCountersAreNull()
    {
        // Arrange
        _telemetryServiceMock.Setup(t => t.GetMeter(It.IsAny<string>())).Returns((Meter?)null);

        // Act - Should not throw
        _healthMetricsService.RecordHealthCheck("TestHealthCheck", HealthStatus.Healthy, 100);

        // Assert - No assertion needed, just checking it doesn't throw
    }

    [Fact]
    public void RecordHealthCheck_ShouldIncrementCounters_WhenInitialized()
    {
        // Arrange
        var meterMock = new Mock<Meter>();
        var counterMock = new Mock<Counter<long>>();
        var histogramMock = new Mock<Histogram<double>>();
        
        _telemetryServiceMock.Setup(t => t.GetMeter("ErpLight.Health")).Returns(meterMock.Object);
        _telemetryServiceMock.Setup(t => t.CreateCounter("ErpLight.Health", "health.checks.total", It.IsAny<string>()))
            .Returns(counterMock.Object);
        _telemetryServiceMock.Setup(t => t.CreateHistogram("ErpLight.Health", "health.check.duration.ms", It.IsAny<string>()))
            .Returns(histogramMock.Object);

        // Re-initialize the service to pick up the mocks
        _healthMetricsService = new HealthMetricsService(
            _telemetryServiceMock.Object,
            _pluginManagerMock.Object,
            _logger);

        // Act
        _healthMetricsService.RecordHealthCheck("TestHealthCheck", HealthStatus.Healthy, 100);

        // Assert
        counterMock.Verify(c => c.Add(1), Times.Once);
        histogramMock.Verify(h => h.Record(100), Times.Once);
    }

    [Fact]
    public void RecordHealthCheck_ShouldIncrementFailureCounter_WhenUnhealthy()
    {
        // Arrange
        var meterMock = new Mock<Meter>();
        var healthCheckCounterMock = new Mock<Counter<long>>();
        var healthCheckFailureCounterMock = new Mock<Counter<long>>();
        var histogramMock = new Mock<Histogram<double>>();
        
        _telemetryServiceMock.Setup(t => t.GetMeter("ErpLight.Health")).Returns(meterMock.Object);
        _telemetryServiceMock.Setup(t => t.CreateCounter("ErpLight.Health", "health.checks.total", It.IsAny<string>()))
            .Returns(healthCheckCounterMock.Object);
        _telemetryServiceMock.Setup(t => t.CreateCounter("ErpLight.Health", "health.checks.failed", It.IsAny<string>()))
            .Returns(healthCheckFailureCounterMock.Object);
        _telemetryServiceMock.Setup(t => t.CreateHistogram("ErpLight.Health", "health.check.duration.ms", It.IsAny<string>()))
            .Returns(histogramMock.Object);

        _healthMetricsService = new HealthMetricsService(
            _telemetryServiceMock.Object,
            _pluginManagerMock.Object,
            _logger);

        // Act
        _healthMetricsService.RecordHealthCheck("TestHealthCheck", HealthStatus.Unhealthy, 200);

        // Assert
        healthCheckCounterMock.Verify(c => c.Add(1), Times.Once);
        healthCheckFailureCounterMock.Verify(c => c.Add(1), Times.Once);
        histogramMock.Verify(h => h.Record(200), Times.Once);
    }

    [Fact]
    public void RecordHealthCheck_ShouldIncrementFailureCounter_WhenDegraded()
    {
        // Arrange
        var meterMock = new Mock<Meter>();
        var healthCheckCounterMock = new Mock<Counter<long>>();
        var healthCheckFailureCounterMock = new Mock<Counter<long>>();
        var histogramMock = new Mock<Histogram<double>>();
        
        _telemetryServiceMock.Setup(t => t.GetMeter("ErpLight.Health")).Returns(meterMock.Object);
        _telemetryServiceMock.Setup(t => t.CreateCounter("ErpLight.Health", "health.checks.total", It.IsAny<string>()))
            .Returns(healthCheckCounterMock.Object);
        _telemetryServiceMock.Setup(t => t.CreateCounter("ErpLight.Health", "health.checks.failed", It.IsAny<string>()))
            .Returns(healthCheckFailureCounterMock.Object);
        _telemetryServiceMock.Setup(t => t.CreateHistogram("ErpLight.Health", "health.check.duration.ms", It.IsAny<string>()))
            .Returns(histogramMock.Object);

        _healthMetricsService = new HealthMetricsService(
            _telemetryServiceMock.Object,
            _pluginManagerMock.Object,
            _logger);

        // Act
        _healthMetricsService.RecordHealthCheck("TestHealthCheck", HealthStatus.Degraded, 150);

        // Assert
        healthCheckCounterMock.Verify(c => c.Add(1), Times.Once);
        healthCheckFailureCounterMock.Verify(c => c.Add(1), Times.Once);
        histogramMock.Verify(h => h.Record(150), Times.Once);
    }

    [Fact]
    public void RecordPluginHealthCheck_ShouldNotThrow_WhenCountersAreNull()
    {
        // Arrange
        _telemetryServiceMock.Setup(t => t.GetMeter(It.IsAny<string>())).Returns((Meter?)null);

        // Act - Should not throw
        _healthMetricsService.RecordPluginHealthCheck("TestPlugin", HealthStatus.Healthy);

        // Assert - No assertion needed, just checking it doesn't throw
    }

    [Fact]
    public void RecordPluginHealthCheck_ShouldIncrementCounters_WhenInitialized()
    {
        // Arrange
        var meterMock = new Mock<Meter>();
        var pluginHealthCounterMock = new Mock<Counter<long>>();
        var pluginHealthFailureCounterMock = new Mock<Counter<long>>();
        
        _telemetryServiceMock.Setup(t => t.GetMeter("ErpLight.Health")).Returns(meterMock.Object);
        _telemetryServiceMock.Setup(t => t.CreateCounter("ErpLight.Health", "plugin.health.checks.total", It.IsAny<string>()))
            .Returns(pluginHealthCounterMock.Object);
        _telemetryServiceMock.Setup(t => t.CreateCounter("ErpLight.Health", "plugin.health.checks.failed", It.IsAny<string>()))
            .Returns(pluginHealthFailureCounterMock.Object);

        _healthMetricsService = new HealthMetricsService(
            _telemetryServiceMock.Object,
            _pluginManagerMock.Object,
            _logger);

        // Act
        _healthMetricsService.RecordPluginHealthCheck("TestPlugin", HealthStatus.Healthy);

        // Assert
        pluginHealthCounterMock.Verify(c => c.Add(1), Times.Once);
        pluginHealthFailureCounterMock.Verify(c => c.Add(1), Times.Never);
    }

    [Fact]
    public void RecordPluginHealthCheck_ShouldIncrementFailureCounter_WhenUnhealthy()
    {
        // Arrange
        var meterMock = new Mock<Meter>();
        var pluginHealthCounterMock = new Mock<Counter<long>>();
        var pluginHealthFailureCounterMock = new Mock<Counter<long>>();
        
        _telemetryServiceMock.Setup(t => t.GetMeter("ErpLight.Health")).Returns(meterMock.Object);
        _telemetryServiceMock.Setup(t => t.CreateCounter("ErpLight.Health", "plugin.health.checks.total", It.IsAny<string>()))
            .Returns(pluginHealthCounterMock.Object);
        _telemetryServiceMock.Setup(t => t.CreateCounter("ErpLight.Health", "plugin.health.checks.failed", It.IsAny<string>()))
            .Returns(pluginHealthFailureCounterMock.Object);

        _healthMetricsService = new HealthMetricsService(
            _telemetryServiceMock.Object,
            _pluginManagerMock.Object,
            _logger);

        // Act
        _healthMetricsService.RecordPluginHealthCheck("TestPlugin", HealthStatus.Unhealthy);

        // Assert
        pluginHealthCounterMock.Verify(c => c.Add(1), Times.Once);
        pluginHealthFailureCounterMock.Verify(c => c.Add(1), Times.Once);
    }

    [Fact]
    public void GetPluginHealthStatus_ShouldReturnEmpty_WhenNoPlugins()
    {
        // Arrange
        _pluginManagerMock.Setup(p => p.LoadedPlugins).Returns(new List<IPluginModule>());

        // Act
        var status = _healthMetricsService.GetPluginHealthStatus();

        // Assert
        Assert.Empty(status);
    }

    [Fact]
    public void GetPluginHealthStatus_ShouldReturnPluginStatuses()
    {
        // Arrange
        var plugin1 = new Mock<IPluginModule>();
        plugin1.Setup(p => p.ModuleId).Returns("Plugin1");
        plugin1.Setup(p => p.CheckHealthAsync()).ReturnsAsync(HealthStatus.Healthy);

        var plugin2 = new Mock<IPluginModule>();
        plugin2.Setup(p => p.ModuleId).Returns("Plugin2");
        plugin2.Setup(p => p.CheckHealthAsync()).ReturnsAsync(HealthStatus.Unhealthy);

        var plugins = new List<IPluginModule> { plugin1.Object, plugin2.Object };
        _pluginManagerMock.Setup(p => p.LoadedPlugins).Returns(plugins);

        // Act
        var status = _healthMetricsService.GetPluginHealthStatus();

        // Assert
        Assert.Equal(2, status.Count);
        Assert.Equal(HealthStatus.Healthy, status["Plugin1"]);
        Assert.Equal(HealthStatus.Unhealthy, status["Plugin2"]);
    }

    [Fact]
    public void GetPluginHealthStatus_ShouldHandleException_AndMarkUnhealthy()
    {
        // Arrange
        var plugin1 = new Mock<IPluginModule>();
        plugin1.Setup(p => p.ModuleId).Returns("Plugin1");
        plugin1.Setup(p => p.CheckHealthAsync()).ThrowsAsync(new Exception("Health check failed"));

        var plugin2 = new Mock<IPluginModule>();
        plugin2.Setup(p => p.ModuleId).Returns("Plugin2");
        plugin2.Setup(p => p.CheckHealthAsync()).ReturnsAsync(HealthStatus.Healthy);

        var plugins = new List<IPluginModule> { plugin1.Object, plugin2.Object };
        _pluginManagerMock.Setup(p => p.LoadedPlugins).Returns(plugins);

        // Act
        var status = _healthMetricsService.GetPluginHealthStatus();

        // Assert
        Assert.Equal(2, status.Count);
        Assert.Equal(HealthStatus.Unhealthy, status["Plugin1"]);
        Assert.Equal(HealthStatus.Healthy, status["Plugin2"]);
    }

    [Fact]
    public void GetOverallHealthStatus_ShouldReturnHealthy_WhenAllPluginsHealthy()
    {
        // Arrange
        var plugin1 = new Mock<IPluginModule>();
        plugin1.Setup(p => p.ModuleId).Returns("Plugin1");
        plugin1.Setup(p => p.CheckHealthAsync()).ReturnsAsync(HealthStatus.Healthy);

        var plugin2 = new Mock<IPluginModule>();
        plugin2.Setup(p => p.ModuleId).Returns("Plugin2");
        plugin2.Setup(p => p.CheckHealthAsync()).ReturnsAsync(HealthStatus.Healthy);

        var plugins = new List<IPluginModule> { plugin1.Object, plugin2.Object };
        _pluginManagerMock.Setup(p => p.LoadedPlugins).Returns(plugins);

        // Act
        var status = _healthMetricsService.GetOverallHealthStatus();

        // Assert
        Assert.Equal(HealthStatus.Healthy, status);
    }

    [Fact]
    public void GetOverallHealthStatus_ShouldReturnUnhealthy_WhenAnyPluginUnhealthy()
    {
        // Arrange
        var plugin1 = new Mock<IPluginModule>();
        plugin1.Setup(p => p.ModuleId).Returns("Plugin1");
        plugin1.Setup(p => p.CheckHealthAsync()).ReturnsAsync(HealthStatus.Healthy);

        var plugin2 = new Mock<IPluginModule>();
        plugin2.Setup(p => p.ModuleId).Returns("Plugin2");
        plugin2.Setup(p => p.CheckHealthAsync()).ReturnsAsync(HealthStatus.Unhealthy);

        var plugins = new List<IPluginModule> { plugin1.Object, plugin2.Object };
        _pluginManagerMock.Setup(p => p.LoadedPlugins).Returns(plugins);

        // Act
        var status = _healthMetricsService.GetOverallHealthStatus();

        // Assert
        Assert.Equal(HealthStatus.Unhealthy, status);
    }

    [Fact]
    public void GetOverallHealthStatus_ShouldReturnDegraded_WhenAnyPluginDegradedAndNoneUnhealthy()
    {
        // Arrange
        var plugin1 = new Mock<IPluginModule>();
        plugin1.Setup(p => p.ModuleId).Returns("Plugin1");
        plugin1.Setup(p => p.CheckHealthAsync()).ReturnsAsync(HealthStatus.Healthy);

        var plugin2 = new Mock<IPluginModule>();
        plugin2.Setup(p => p.ModuleId).Returns("Plugin2");
        plugin2.Setup(p => p.CheckHealthAsync()).ReturnsAsync(HealthStatus.Degraded);

        var plugins = new List<IPluginModule> { plugin1.Object, plugin2.Object };
        _pluginManagerMock.Setup(p => p.LoadedPlugins).Returns(plugins);

        // Act
        var status = _healthMetricsService.GetOverallHealthStatus();

        // Assert
        Assert.Equal(HealthStatus.Degraded, status);
    }

    [Fact]
    public void GetOverallHealthStatus_ShouldReturnUnhealthy_WhenAnyPluginUnhealthyEvenIfOthersDegraded()
    {
        // Arrange
        var plugin1 = new Mock<IPluginModule>();
        plugin1.Setup(p => p.ModuleId).Returns("Plugin1");
        plugin1.Setup(p => p.CheckHealthAsync()).ReturnsAsync(HealthStatus.Degraded);

        var plugin2 = new Mock<IPluginModule>();
        plugin2.Setup(p => p.ModuleId).Returns("Plugin2");
        plugin2.Setup(p => p.CheckHealthAsync()).ReturnsAsync(HealthStatus.Unhealthy);

        var plugins = new List<IPluginModule> { plugin1.Object, plugin2.Object };
        _pluginManagerMock.Setup(p => p.LoadedPlugins).Returns(plugins);

        // Act
        var status = _healthMetricsService.GetOverallHealthStatus();

        // Assert
        Assert.Equal(HealthStatus.Unhealthy, status);
    }

    [Fact]
    public void GetOverallHealthStatus_ShouldReturnHealthy_WhenNoPlugins()
    {
        // Arrange
        _pluginManagerMock.Setup(p => p.LoadedPlugins).Returns(new List<IPluginModule>());

        // Act
        var status = _healthMetricsService.GetOverallHealthStatus();

        // Assert
        Assert.Equal(HealthStatus.Healthy, status);
    }

    [Fact]
    public void GetOverallHealthStatus_ShouldHandleException_AndReturnUnhealthy()
    {
        // Arrange
        var plugin1 = new Mock<IPluginModule>();
        plugin1.Setup(p => p.ModuleId).Returns("Plugin1");
        plugin1.Setup(p => p.CheckHealthAsync()).ThrowsAsync(new Exception("Health check failed"));

        var plugins = new List<IPluginModule> { plugin1.Object };
        _pluginManagerMock.Setup(p => p.LoadedPlugins).Returns(plugins);

        // Act
        var status = _healthMetricsService.GetOverallHealthStatus();

        // Assert
        Assert.Equal(HealthStatus.Unhealthy, status);
    }
}
