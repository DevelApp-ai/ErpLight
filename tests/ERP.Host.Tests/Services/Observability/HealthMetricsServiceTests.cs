using ERP.Host.Services;
using System.Diagnostics.Metrics;
using ERP.SharedKernel.Contracts;
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
    private HealthMetricsService _healthMetricsService;

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

    private sealed class MetricsCapture : IDisposable
    {
        private readonly MeterListener _listener = new();
        public System.Collections.Concurrent.ConcurrentQueue<(string Instrument, object Value)> Measurements { get; } = new();

        public MetricsCapture()
        {
            _listener.InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == "ErpLight.Health")
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            };
            _listener.SetMeasurementEventCallback<long>((instrument, value, _, _) => Measurements.Enqueue((instrument.Name, value)));
            _listener.SetMeasurementEventCallback<double>((instrument, value, _, _) => Measurements.Enqueue((instrument.Name, value)));
            _listener.Start();
        }

        public void Dispose() => _listener.Dispose();
    }

    private HealthMetricsService CreateServiceWithRealMeter()
    {
        var meter = new Meter("ErpLight.Health");
        _telemetryServiceMock.Setup(t => t.GetMeter(It.IsAny<string>())).Returns(meter);
        return new HealthMetricsService(
            _telemetryServiceMock.Object,
            _pluginManagerMock.Object,
            _logger);
    }

    public void Dispose()
    {
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
        using var capture = new MetricsCapture();
        _healthMetricsService = CreateServiceWithRealMeter();

        // Act
        _healthMetricsService.RecordHealthCheck("TestHealthCheck", HealthStatus.Healthy, 100);

        // Assert
        Assert.Contains(capture.Measurements, m => m.Instrument == "health.checks.total" && Equals(m.Value, 1L));
        Assert.Contains(capture.Measurements, m => m.Instrument == "health.check.duration.ms" && Equals(m.Value, 100.0));
        Assert.DoesNotContain(capture.Measurements, m => m.Instrument == "health.checks.failed");
    }
    [Fact]
    public void RecordHealthCheck_ShouldIncrementFailureCounter_WhenUnhealthy()
    {
        // Arrange
        using var capture = new MetricsCapture();
        _healthMetricsService = CreateServiceWithRealMeter();

        // Act
        _healthMetricsService.RecordHealthCheck("TestHealthCheck", HealthStatus.Unhealthy, 200);

        // Assert
        Assert.Contains(capture.Measurements, m => m.Instrument == "health.checks.total" && Equals(m.Value, 1L));
        Assert.Contains(capture.Measurements, m => m.Instrument == "health.checks.failed" && Equals(m.Value, 1L));
        Assert.Contains(capture.Measurements, m => m.Instrument == "health.check.duration.ms" && Equals(m.Value, 200.0));
    }
    [Fact]
    public void RecordHealthCheck_ShouldIncrementFailureCounter_WhenDegraded()
    {
        // Arrange
        using var capture = new MetricsCapture();
        _healthMetricsService = CreateServiceWithRealMeter();

        // Act
        _healthMetricsService.RecordHealthCheck("TestHealthCheck", HealthStatus.Degraded, 150);

        // Assert
        Assert.Contains(capture.Measurements, m => m.Instrument == "health.checks.total" && Equals(m.Value, 1L));
        Assert.Contains(capture.Measurements, m => m.Instrument == "health.checks.failed" && Equals(m.Value, 1L));
        Assert.Contains(capture.Measurements, m => m.Instrument == "health.check.duration.ms" && Equals(m.Value, 150.0));
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
        using var capture = new MetricsCapture();
        _healthMetricsService = CreateServiceWithRealMeter();

        // Act
        _healthMetricsService.RecordPluginHealthCheck("TestPlugin", HealthStatus.Healthy);

        // Assert
        Assert.Contains(capture.Measurements, m => m.Instrument == "plugin.health.checks.total" && Equals(m.Value, 1L));
        Assert.DoesNotContain(capture.Measurements, m => m.Instrument == "plugin.health.checks.failed");
    }
    [Fact]
    public void RecordPluginHealthCheck_ShouldIncrementFailureCounter_WhenUnhealthy()
    {
        // Arrange
        using var capture = new MetricsCapture();
        _healthMetricsService = CreateServiceWithRealMeter();

        // Act
        _healthMetricsService.RecordPluginHealthCheck("TestPlugin", HealthStatus.Unhealthy);

        // Assert
        Assert.Contains(capture.Measurements, m => m.Instrument == "plugin.health.checks.total" && Equals(m.Value, 1L));
        Assert.Contains(capture.Measurements, m => m.Instrument == "plugin.health.checks.failed" && Equals(m.Value, 1L));
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
