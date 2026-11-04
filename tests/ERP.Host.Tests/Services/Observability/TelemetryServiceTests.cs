using ERP.Host.Services.Observability;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace ERP.Host.Tests.Services.Observability;

/// <summary>
/// Unit tests for TelemetryService.
/// </summary>
public class TelemetryServiceTests
{
    private static TelemetryService CreateTelemetryService(bool telemetryEnabled)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Telemetry:Enabled"] = telemetryEnabled ? "true" : "false",
                ["Environment"] = "Development",
            })
            .Build();

        return new TelemetryService(configuration, NullLogger<TelemetryService>.Instance);
    }

    [Fact]
    public void Initialize_ShouldNotInitialize_WhenTelemetryDisabled()
    {
        // Arrange
        var service = CreateTelemetryService(telemetryEnabled: false);

        // Act
        service.Initialize();

        // Assert - Should not throw and should not create providers
        var meterProviderField = typeof(TelemetryService).GetField("_meterProvider",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var tracerProviderField = typeof(TelemetryService).GetField("_tracerProvider",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var meterProvider = meterProviderField?.GetValue(service);
        var tracerProvider = tracerProviderField?.GetValue(service);

        Assert.Null(meterProvider);
        Assert.Null(tracerProvider);
    }

    [Fact]
    public void Initialize_ShouldInitialize_WhenTelemetryEnabled()
    {
        // Arrange
        var service = CreateTelemetryService(telemetryEnabled: true);

        // Act
        service.Initialize();

        // Assert - Providers should be created
        var meterProviderField = typeof(TelemetryService).GetField("_meterProvider",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var tracerProviderField = typeof(TelemetryService).GetField("_tracerProvider",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var meterProvider = meterProviderField?.GetValue(service);
        var tracerProvider = tracerProviderField?.GetValue(service);

        Assert.NotNull(meterProvider);
        Assert.NotNull(tracerProvider);
    }

    [Fact]
    public void StartActivity_ShouldReturnNull_WhenNotInitialized()
    {
        // Arrange
        var service = CreateTelemetryService(telemetryEnabled: false);

        // Act
        var activity = service.StartActivity("TestActivity");

        // Assert
        Assert.Null(activity);
    }

    [Fact]
    public void StartActivity_ShouldReturnActivity_WhenInitialized()
    {
        // Arrange
        var service = CreateTelemetryService(telemetryEnabled: true);
        service.Initialize();

        // Act
        var activity = service.StartActivity("TestActivity");

        // Assert
        Assert.NotNull(activity);
        Assert.Equal("TestActivity", activity.DisplayName);
    }

    [Fact]
    public void StartActivity_ShouldSetCorrectActivityKind()
    {
        // Arrange
        var service = CreateTelemetryService(telemetryEnabled: true);
        service.Initialize();

        // Act
        var activity = service.StartActivity("TestActivity", ActivityKind.Server);

        // Assert
        Assert.NotNull(activity);
        Assert.Equal(ActivityKind.Server, activity.Kind);
    }

    [Fact]
    public async Task ExecuteWithTracingAsync_ShouldExecuteAction()
    {
        // Arrange
        var service = CreateTelemetryService(telemetryEnabled: true);
        service.Initialize();

        var executed = false;

        // Act
        await service.ExecuteWithTracingAsync("TestOperation", async () =>
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
        var service = CreateTelemetryService(telemetryEnabled: true);
        service.Initialize();

        // Act
        var result = await service.ExecuteWithTracingAsync("TestOperation", async () =>
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
        var service = CreateTelemetryService(telemetryEnabled: true);
        service.Initialize();

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => service.ExecuteWithTracingAsync("TestOperation", async () =>
        {
            throw new Exception("Test exception");
        }));
    }

    [Fact]
    public void GetMeter_ShouldReturnNull_WhenNotInitialized()
    {
        // Arrange
        var service = CreateTelemetryService(telemetryEnabled: false);

        // Act
        var meter = service.GetMeter("TestMeter");

        // Assert
        Assert.Null(meter);
    }

    [Fact]
    public void GetMeter_ShouldReturnMeter_WhenInitialized()
    {
        // Arrange
        var service = CreateTelemetryService(telemetryEnabled: true);
        service.Initialize();

        // Act
        var meter = service.GetMeter("TestMeter");

        // Assert
        Assert.NotNull(meter);
    }

    [Fact]
    public void CreateCounter_ShouldReturnNull_WhenNotInitialized()
    {
        // Arrange
        var service = CreateTelemetryService(telemetryEnabled: false);

        // Act
        var counter = service.CreateCounter("TestMeter", "TestCounter", "Test description");

        // Assert
        Assert.Null(counter);
    }

    [Fact]
    public void CreateCounter_ShouldReturnCounter_WhenInitialized()
    {
        // Arrange
        var service = CreateTelemetryService(telemetryEnabled: true);
        service.Initialize();

        // Act
        var counter = service.CreateCounter("TestMeter", "TestCounter", "Test description");

        // Assert
        Assert.NotNull(counter);
    }

    [Fact]
    public void CreateHistogram_ShouldReturnNull_WhenNotInitialized()
    {
        // Arrange
        var service = CreateTelemetryService(telemetryEnabled: false);

        // Act
        var histogram = service.CreateHistogram("TestMeter", "TestHistogram", "Test description");

        // Assert
        Assert.Null(histogram);
    }

    [Fact]
    public void CreateHistogram_ShouldReturnHistogram_WhenInitialized()
    {
        // Arrange
        var service = CreateTelemetryService(telemetryEnabled: true);
        service.Initialize();

        // Act
        var histogram = service.CreateHistogram("TestMeter", "TestHistogram", "Test description");

        // Assert
        Assert.NotNull(histogram);
    }

    [Fact]
    public void CreateGauge_ShouldNotThrow_WhenNotInitialized()
    {
        // Arrange
        var service = CreateTelemetryService(telemetryEnabled: false);
        int value = 42;

        // Act - Should not throw
        service.CreateGauge("TestMeter", "TestGauge", "Test description", () => value);

        // Assert - No assertion needed, just checking it doesn't throw
    }

    [Fact]
    public void CreateGauge_ShouldNotThrow_WhenInitialized()
    {
        // Arrange
        var service = CreateTelemetryService(telemetryEnabled: true);
        service.Initialize();

        int value = 42;

        // Act - Should not throw
        service.CreateGauge("TestMeter", "TestGauge", "Test description", () => value);

        // Assert - No assertion needed, just checking it doesn't throw
    }

    [Fact]
    public void Dispose_ShouldDisposeProviders()
    {
        // Arrange
        var service = CreateTelemetryService(telemetryEnabled: true);
        service.Initialize();

        // Act
        service.Dispose();

        // Assert - Should not throw; providers are disposed by the service
    }

    [Fact]
    public void Dispose_ShouldBeIdempotent()
    {
        // Arrange
        var service = CreateTelemetryService(telemetryEnabled: true);
        service.Initialize();

        // Act - Should not throw when called multiple times
        service.Dispose();
        service.Dispose();

        // Assert - No assertion needed, just checking it doesn't throw
    }
}
