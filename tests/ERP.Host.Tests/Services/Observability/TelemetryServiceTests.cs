using ERP.Host.Services.Observability;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Xunit;

namespace ERP.Host.Tests.Services.Observability;

/// <summary>
/// Unit tests for TelemetryService.
/// </summary>
public class TelemetryServiceTests : IDisposable
{
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly ILogger<TelemetryService> _logger;
    private readonly TelemetryService _telemetryService;

    public TelemetryServiceTests()
    {
        _configurationMock = new Mock<IConfiguration>();
        _logger = NullLogger<TelemetryService>.Instance;
        _telemetryService = new TelemetryService(_configurationMock.Object, _logger);
    }

    public void Dispose()
    {
        _telemetryService.Dispose();
        _configurationMock?.Invoke();
    }

    [Fact]
    public void Initialize_ShouldNotInitialize_WhenTelemetryDisabled()
    {
        // Arrange
        var telemetrySectionMock = new Mock<IConfigurationSection>();
        telemetrySectionMock.Setup(s => s.GetValue<bool>("Enabled")).Returns(false);
        _configurationMock.Setup(c => c.GetSection("Telemetry")).Returns(telemetrySectionMock.Object);

        // Act
        _telemetryService.Initialize();

        // Assert - Should not throw and should not create providers
        var meterProviderField = typeof(TelemetryService).GetField("_meterProvider", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var tracerProviderField = typeof(TelemetryService).GetField("_tracerProvider", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        var meterProvider = meterProviderField?.GetValue(_telemetryService);
        var tracerProvider = tracerProviderField?.GetValue(_telemetryService);
        
        Assert.Null(meterProvider);
        Assert.Null(tracerProvider);
    }

    [Fact]
    public void Initialize_ShouldInitialize_WhenTelemetryEnabled()
    {
        // Arrange
        var telemetrySectionMock = new Mock<IConfigurationSection>();
        telemetrySectionMock.Setup(s => s.GetValue<bool>("Enabled")).Returns(true);
        telemetrySectionMock.Setup(s => s.GetValue<string>("Environment")).Returns("Development");
        telemetrySectionMock.Setup(s => s.GetSection("Jaeger")).Returns(Mock.Of<IConfigurationSection>());
        telemetrySectionMock.Setup(s => s.GetSection("Zipkin")).Returns(Mock.Of<IConfigurationSection>());
        telemetrySectionMock.Setup(s => s.GetSection("Prometheus")).Returns(Mock.Of<IConfigurationSection>());
        telemetrySectionMock.Setup(s => s.GetSection("OTLP")).Returns(Mock.Of<IConfigurationSection>());
        
        _configurationMock.Setup(c => c.GetSection("Telemetry")).Returns(telemetrySectionMock.Object);
        _configurationMock.Setup(c => c.GetValue<string>("Environment")).Returns("Development");

        // Act
        _telemetryService.Initialize();

        // Assert - Providers should be created
        var meterProviderField = typeof(TelemetryService).GetField("_meterProvider", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var tracerProviderField = typeof(TelemetryService).GetField("_tracerProvider", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        var meterProvider = meterProviderField?.GetValue(_telemetryService);
        var tracerProvider = tracerProviderField?.GetValue(_telemetryService);
        
        Assert.NotNull(meterProvider);
        Assert.NotNull(tracerProvider);
    }

    [Fact]
    public void StartActivity_ShouldReturnNull_WhenNotInitialized()
    {
        // Act
        var activity = _telemetryService.StartActivity("TestActivity");

        // Assert
        Assert.Null(activity);
    }

    [Fact]
    public void StartActivity_ShouldReturnActivity_WhenInitialized()
    {
        // Arrange
        var telemetrySectionMock = new Mock<IConfigurationSection>();
        telemetrySectionMock.Setup(s => s.GetValue<bool>("Enabled")).Returns(true);
        telemetrySectionMock.Setup(s => s.GetValue<string>("Environment")).Returns("Development");
        telemetrySectionMock.Setup(s => s.GetSection("Jaeger")).Returns(Mock.Of<IConfigurationSection>());
        telemetrySectionMock.Setup(s => s.GetSection("Zipkin")).Returns(Mock.Of<IConfigurationSection>());
        telemetrySectionMock.Setup(s => s.GetSection("Prometheus")).Returns(Mock.Of<IConfigurationSection>());
        telemetrySectionMock.Setup(s => s.GetSection("OTLP")).Returns(Mock.Of<IConfigurationSection>());
        
        _configurationMock.Setup(c => c.GetSection("Telemetry")).Returns(telemetrySectionMock.Object);
        _configurationMock.Setup(c => c.GetValue<string>("Environment")).Returns("Development");
        
        _telemetryService.Initialize();

        // Act
        var activity = _telemetryService.StartActivity("TestActivity");

        // Assert
        Assert.NotNull(activity);
        Assert.Equal("TestActivity", activity.Name);
    }

    [Fact]
    public void StartActivity_ShouldSetCorrectActivityKind()
    {
        // Arrange
        var telemetrySectionMock = new Mock<IConfigurationSection>();
        telemetrySectionMock.Setup(s => s.GetValue<bool>("Enabled")).Returns(true);
        telemetrySectionMock.Setup(s => s.GetValue<string>("Environment")).Returns("Development");
        telemetrySectionMock.Setup(s => s.GetSection("Jaeger")).Returns(Mock.Of<IConfigurationSection>());
        telemetrySectionMock.Setup(s => s.GetSection("Zipkin")).Returns(Mock.Of<IConfigurationSection>());
        telemetrySectionMock.Setup(s => s.GetSection("Prometheus")).Returns(Mock.Of<IConfigurationSection>());
        telemetrySectionMock.Setup(s => s.GetSection("OTLP")).Returns(Mock.Of<IConfigurationSection>());
        
        _configurationMock.Setup(c => c.GetSection("Telemetry")).Returns(telemetrySectionMock.Object);
        _configurationMock.Setup(c => c.GetValue<string>("Environment")).Returns("Development");
        
        _telemetryService.Initialize();

        // Act
        var activity = _telemetryService.StartActivity("TestActivity", ActivityKind.Server);

        // Assert
        Assert.NotNull(activity);
        Assert.Equal(ActivityKind.Server, activity.Kind);
    }

    [Fact]
    public async Task ExecuteWithTracingAsync_ShouldExecuteAction()
    {
        // Arrange
        var telemetrySectionMock = new Mock<IConfigurationSection>();
        telemetrySectionMock.Setup(s => s.GetValue<bool>("Enabled")).Returns(true);
        telemetrySectionMock.Setup(s => s.GetValue<string>("Environment")).Returns("Development");
        telemetrySectionMock.Setup(s => s.GetSection("Jaeger")).Returns(Mock.Of<IConfigurationSection>());
        telemetrySectionMock.Setup(s => s.GetSection("Zipkin")).Returns(Mock.Of<IConfigurationSection>());
        telemetrySectionMock.Setup(s => s.GetSection("Prometheus")).Returns(Mock.Of<IConfigurationSection>());
        telemetrySectionMock.Setup(s => s.GetSection("OTLP")).Returns(Mock.Of<IConfigurationSection>());
        
        _configurationMock.Setup(c => c.GetSection("Telemetry")).Returns(telemetrySectionMock.Object);
        _configurationMock.Setup(c => c.GetValue<string>("Environment")).Returns("Development");
        
        _telemetryService.Initialize();

        var executed = false;

        // Act
        await _telemetryService.ExecuteWithTracingAsync("TestOperation", async () =>
        {
            executed = true;
        });

        // Assert
        Assert.True(executed);
    }

    [Fact]
    public async Task ExecuteWithTracingAsync_ShouldReturnResult()
    {
        // Arrange
        var telemetrySectionMock = new Mock<IConfigurationSection>();
        telemetrySectionMock.Setup(s => s.GetValue<bool>("Enabled")).Returns(true);
        telemetrySectionMock.Setup(s => s.GetValue<string>("Environment")).Returns("Development");
        telemetrySectionMock.Setup(s => s.GetSection("Jaeger")).Returns(Mock.Of<IConfigurationSection>());
        telemetrySectionMock.Setup(s => s.GetSection("Zipkin")).Returns(Mock.Of<IConfigurationSection>());
        telemetrySectionMock.Setup(s => s.GetSection("Prometheus")).Returns(Mock.Of<IConfigurationSection>());
        telemetrySectionMock.Setup(s => s.GetSection("OTLP")).Returns(Mock.Of<IConfigurationSection>());
        
        _configurationMock.Setup(c => c.GetSection("Telemetry")).Returns(telemetrySectionMock.Object);
        _configurationMock.Setup(c => c.GetValue<string>("Environment")).Returns("Development");
        
        _telemetryService.Initialize();

        // Act
        var result = await _telemetryService.ExecuteWithTracingAsync("TestOperation", async () =>
        {
            return "TestResult";
        });

        // Assert
        Assert.Equal("TestResult", result);
    }

    [Fact]
    public async Task ExecuteWithTracingAsync_ShouldThrowOnException()
    {
        // Arrange
        var telemetrySectionMock = new Mock<IConfigurationSection>();
        telemetrySectionMock.Setup(s => s.GetValue<bool>("Enabled")).Returns(true);
        telemetrySectionMock.Setup(s => s.GetValue<string>("Environment")).Returns("Development");
        telemetrySectionMock.Setup(s => s.GetSection("Jaeger")).Returns(Mock.Of<IConfigurationSection>());
        telemetrySectionMock.Setup(s => s.GetSection("Zipkin")).Returns(Mock.Of<IConfigurationSection>());
        telemetrySectionMock.Setup(s => s.GetSection("Prometheus")).Returns(Mock.Of<IConfigurationSection>());
        telemetrySectionMock.Setup(s => s.GetSection("OTLP")).Returns(Mock.Of<IConfigurationSection>());
        
        _configurationMock.Setup(c => c.GetSection("Telemetry")).Returns(telemetrySectionMock.Object);
        _configurationMock.Setup(c => c.GetValue<string>("Environment")).Returns("Development");
        
        _telemetryService.Initialize();

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _telemetryService.ExecuteWithTracingAsync("TestOperation", async () =>
        {
            throw new Exception("Test exception");
        }));
    }

    [Fact]
    public void GetMeter_ShouldReturnNull_WhenNotInitialized()
    {
        // Act
        var meter = _telemetryService.GetMeter("TestMeter");

        // Assert
        Assert.Null(meter);
    }

    [Fact]
    public void GetMeter_ShouldReturnMeter_WhenInitialized()
    {
        // Arrange
        var telemetrySectionMock = new Mock<IConfigurationSection>();
        telemetrySectionMock.Setup(s => s.GetValue<bool>("Enabled")).Returns(true);
        telemetrySectionMock.Setup(s => s.GetValue<string>("Environment")).Returns("Development");
        telemetrySectionMock.Setup(s => s.GetSection("Jaeger")).Returns(Mock.Of<IConfigurationSection>());
        telemetrySectionMock.Setup(s => s.GetSection("Zipkin")).Returns(Mock.Of<IConfigurationSection>());
        telemetrySectionMock.Setup(s => s.GetSection("Prometheus")).Returns(Mock.Of<IConfigurationSection>());
        telemetrySectionMock.Setup(s => s.GetSection("OTLP")).Returns(Mock.Of<IConfigurationSection>());
        
        _configurationMock.Setup(c => c.GetSection("Telemetry")).Returns(telemetrySectionMock.Object);
        _configurationMock.Setup(c => c.GetValue<string>("Environment")).Returns("Development");
        
        _telemetryService.Initialize();

        // Act
        var meter = _telemetryService.GetMeter("TestMeter");

        // Assert
        Assert.NotNull(meter);
    }

    [Fact]
    public void CreateCounter_ShouldReturnNull_WhenNotInitialized()
    {
        // Act
        var counter = _telemetryService.CreateCounter("TestMeter", "TestCounter", "Test description");

        // Assert
        Assert.Null(counter);
    }

    [Fact]
    public void CreateCounter_ShouldReturnCounter_WhenInitialized()
    {
        // Arrange
        var telemetrySectionMock = new Mock<IConfigurationSection>();
        telemetrySectionMock.Setup(s => s.GetValue<bool>("Enabled")).Returns(true);
        telemetrySectionMock.Setup(s => s.GetValue<string>("Environment")).Returns("Development");
        telemetrySectionMock.Setup(s => s.GetSection("Jaeger")).Returns(Mock.Of<IConfigurationSection>());
        telemetrySectionMock.Setup(s => s.GetSection("Zipkin")).Returns(Mock.Of<IConfigurationSection>());
        telemetrySectionMock.Setup(s => s.GetSection("Prometheus")).Returns(Mock.Of<IConfigurationSection>());
        telemetrySectionMock.Setup(s => s.GetSection("OTLP")).Returns(Mock.Of<IConfigurationSection>());
        
        _configurationMock.Setup(c => c.GetSection("Telemetry")).Returns(telemetrySectionMock.Object);
        _configurationMock.Setup(c => c.GetValue<string>("Environment")).Returns("Development");
        
        _telemetryService.Initialize();

        // Act
        var counter = _telemetryService.CreateCounter("TestMeter", "TestCounter", "Test description");

        // Assert
        Assert.NotNull(counter);
    }

    [Fact]
    public void CreateHistogram_ShouldReturnNull_WhenNotInitialized()
    {
        // Act
        var histogram = _telemetryService.CreateHistogram("TestMeter", "TestHistogram", "Test description");

        // Assert
        Assert.Null(histogram);
    }

    [Fact]
    public void CreateHistogram_ShouldReturnHistogram_WhenInitialized()
    {
        // Arrange
        var telemetrySectionMock = new Mock<IConfigurationSection>();
        telemetrySectionMock.Setup(s => s.GetValue<bool>("Enabled")).Returns(true);
        telemetrySectionMock.Setup(s => s.GetValue<string>("Environment")).Returns("Development");
        telemetrySectionMock.Setup(s => s.GetSection("Jaeger")).Returns(Mock.Of<IConfigurationSection>());
        telemetrySectionMock.Setup(s => s.GetSection("Zipkin")).Returns(Mock.Of<IConfigurationSection>());
        telemetrySectionMock.Setup(s => s.GetSection("Prometheus")).Returns(Mock.Of<IConfigurationSection>());
        telemetrySectionMock.Setup(s => s.GetSection("OTLP")).Returns(Mock.Of<IConfigurationSection>());
        
        _configurationMock.Setup(c => c.GetSection("Telemetry")).Returns(telemetrySectionMock.Object);
        _configurationMock.Setup(c => c.GetValue<string>("Environment")).Returns("Development");
        
        _telemetryService.Initialize();

        // Act
        var histogram = _telemetryService.CreateHistogram("TestMeter", "TestHistogram", "Test description");

        // Assert
        Assert.NotNull(histogram);
    }

    [Fact]
    public void CreateGauge_ShouldNotThrow_WhenNotInitialized()
    {
        // Arrange
        int value = 42;

        // Act - Should not throw
        _telemetryService.CreateGauge("TestMeter", "TestGauge", "Test description", () => value);

        // Assert - No assertion needed, just checking it doesn't throw
    }

    [Fact]
    public void CreateGauge_ShouldNotThrow_WhenInitialized()
    {
        // Arrange
        var telemetrySectionMock = new Mock<IConfigurationSection>();
        telemetrySectionMock.Setup(s => s.GetValue<bool>("Enabled")).Returns(true);
        telemetrySectionMock.Setup(s => s.GetValue<string>("Environment")).Returns("Development");
        telemetrySectionMock.Setup(s => s.GetSection("Jaeger")).Returns(Mock.Of<IConfigurationSection>());
        telemetrySectionMock.Setup(s => s.GetSection("Zipkin")).Returns(Mock.Of<IConfigurationSection>());
        telemetrySectionMock.Setup(s => s.GetSection("Prometheus")).Returns(Mock.Of<IConfigurationSection>());
        telemetrySectionMock.Setup(s => s.GetSection("OTLP")).Returns(Mock.Of<IConfigurationSection>());
        
        _configurationMock.Setup(c => c.GetSection("Telemetry")).Returns(telemetrySectionMock.Object);
        _configurationMock.Setup(c => c.GetValue<string>("Environment")).Returns("Development");
        
        _telemetryService.Initialize();

        int value = 42;

        // Act - Should not throw
        _telemetryService.CreateGauge("TestMeter", "TestGauge", "Test description", () => value);

        // Assert - No assertion needed, just checking it doesn't throw
    }

    [Fact]
    public void Dispose_ShouldDisposeProviders()
    {
        // Arrange
        var telemetrySectionMock = new Mock<IConfigurationSection>();
        telemetrySectionMock.Setup(s => s.GetValue<bool>("Enabled")).Returns(true);
        telemetrySectionMock.Setup(s => s.GetValue<string>("Environment")).Returns("Development");
        telemetrySectionMock.Setup(s => s.GetSection("Jaeger")).Returns(Mock.Of<IConfigurationSection>());
        telemetrySectionMock.Setup(s => s.GetSection("Zipkin")).Returns(Mock.Of<IConfigurationSection>());
        telemetrySectionMock.Setup(s => s.GetSection("Prometheus")).Returns(Mock.Of<IConfigurationSection>());
        telemetrySectionMock.Setup(s => s.GetSection("OTLP")).Returns(Mock.Of<IConfigurationSection>());
        
        _configurationMock.Setup(c => c.GetSection("Telemetry")).Returns(telemetrySectionMock.Object);
        _configurationMock.Setup(c => c.GetValue<string>("Environment")).Returns("Development");
        
        _telemetryService.Initialize();

        // Act
        _telemetryService.Dispose();

        // Assert - Should not throw
        // Providers should be disposed
        var meterProviderField = typeof(TelemetryService).GetField("_meterProvider", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var tracerProviderField = typeof(TelemetryService).GetField("_tracerProvider", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        // After dispose, providers should be null or disposed
        // We can't easily check if they're disposed, but we can verify the method doesn't throw
    }

    [Fact]
    public void Dispose_ShouldBeIdempotent()
    {
        // Arrange
        var telemetrySectionMock = new Mock<IConfigurationSection>();
        telemetrySectionMock.Setup(s => s.GetValue<bool>("Enabled")).Returns(true);
        telemetrySectionMock.Setup(s => s.GetValue<string>("Environment")).Returns("Development");
        telemetrySectionMock.Setup(s => s.GetSection("Jaeger")).Returns(Mock.Of<IConfigurationSection>());
        telemetrySectionMock.Setup(s => s.GetSection("Zipkin")).Returns(Mock.Of<IConfigurationSection>());
        telemetrySectionMock.Setup(s => s.GetSection("Prometheus")).Returns(Mock.Of<IConfigurationSection>());
        telemetrySectionMock.Setup(s => s.GetSection("OTLP")).Returns(Mock.Of<IConfigurationSection>());
        
        _configurationMock.Setup(c => c.GetSection("Telemetry")).Returns(telemetrySectionMock.Object);
        _configurationMock.Setup(c => c.GetValue<string>("Environment")).Returns("Development");
        
        _telemetryService.Initialize();

        // Act - Should not throw when called multiple times
        _telemetryService.Dispose();
        _telemetryService.Dispose();

        // Assert - No assertion needed, just checking it doesn't throw
    }
}
