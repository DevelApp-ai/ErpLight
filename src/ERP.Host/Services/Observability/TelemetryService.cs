using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace ERP.Host.Services.Observability;

/// <summary>
/// Service for configuring OpenTelemetry telemetry (metrics and tracing).
/// </summary>
public class TelemetryService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<TelemetryService> _logger;
    private static readonly System.Diagnostics.ActivitySource ActivitySource = new("ErpLight");
    private MeterProvider? _meterProvider;
    private TracerProvider? _tracerProvider;

    public TelemetryService(IConfiguration configuration, ILogger<TelemetryService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Initializes the telemetry service.
    /// </summary>
    public void Initialize()
    {
        var telemetryConfig = _configuration.GetSection("Telemetry");
        var isEnabled = telemetryConfig.GetValue<bool>("Enabled");

        if (!isEnabled)
        {
            _logger.LogInformation("Telemetry is disabled");
            return;
        }

        var resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService("ErpLight")
            .AddAttributes(new[]
            {
                new KeyValuePair<string, object>("application", "ErpLight"),
                new KeyValuePair<string, object>("environment", _configuration.GetValue<string>("Environment") ?? "Development")
            });

        // Configure tracing
        var tracerProviderBuilder = Sdk.CreateTracerProviderBuilder()
            .SetResourceBuilder(resourceBuilder)
            .AddSource("ErpLight")
            .AddSource("ERP.*")
            .AddAspNetCoreInstrumentation(options =>
            {
                options.RecordException = true;
            })
            .AddHttpClientInstrumentation()
            .AddEntityFrameworkCoreInstrumentation();

        // Add console exporter for development
        tracerProviderBuilder.AddConsoleExporter();

        // Add Jaeger exporter if configured
        var jaegerEndpoint = telemetryConfig.GetValue<string>("Jaeger:Endpoint");
        if (!string.IsNullOrEmpty(jaegerEndpoint))
        {
            tracerProviderBuilder.AddJaegerExporter(options =>
            {
                options.AgentHost = jaegerEndpoint.Split(':')[0];
                options.AgentPort = int.Parse(jaegerEndpoint.Split(':')[1]);
            });
        }

        // Add Zipkin exporter if configured
        var zipkinEndpoint = telemetryConfig.GetValue<string>("Zipkin:Endpoint");
        if (!string.IsNullOrEmpty(zipkinEndpoint))
        {
            tracerProviderBuilder.AddZipkinExporter(options =>
            {
                options.Endpoint = new Uri(zipkinEndpoint);
            });
        }

        _tracerProvider = tracerProviderBuilder.Build();
        _logger.LogInformation("Tracing configured");

        // Configure metrics
        var meterProviderBuilder = Sdk.CreateMeterProviderBuilder()
            .SetResourceBuilder(resourceBuilder)
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddMeter("ErpLight.*");

        // Add console exporter for development
        meterProviderBuilder.AddConsoleExporter();

        // Add Prometheus exporter if configured
        var prometheusEnabled = telemetryConfig.GetValue<bool>("Prometheus:Enabled");
        if (prometheusEnabled)
        {
            var prometheusPort = telemetryConfig.GetValue<int>("Prometheus:Port");
            meterProviderBuilder.AddPrometheusExporter(options =>
            {
                options.StartHttpListener = true;
                options.HttpListenerPrefixes = new[] { $"http://*:{prometheusPort}/" };
            });
        }

        // Add OTLP exporter if configured
        var otlpEndpoint = telemetryConfig.GetValue<string>("OTLP:Endpoint");
        if (!string.IsNullOrEmpty(otlpEndpoint))
        {
            meterProviderBuilder.AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(otlpEndpoint);
            });
        }

        _meterProvider = meterProviderBuilder.Build();
        _logger.LogInformation("Metrics configured");
    }

    /// <summary>
    /// Creates a new activity for tracing.
    /// </summary>
    /// <param name="name">The activity name.</param>
    /// <param name="activityKind">The activity kind.</param>
    /// <param name="parentContext">The parent context.</param>
    /// <returns>An activity.</returns>
    public Activity? StartActivity(string name, ActivityKind activityKind = ActivityKind.Internal, ActivityContext? parentContext = null)
    {
        if (_tracerProvider == null)
        {
            return null;
        }

        var activity = ActivitySource.StartActivity(name, activityKind, parentContext ?? default);
        return activity;
    }

    /// <summary>
    /// Creates a new activity with the specified operation name.
    /// </summary>
    /// <param name="operationName">The operation name.</param>
    /// <param name="action">The action to execute.</param>
    /// <returns>A task representing the execution.</returns>
    public async Task ExecuteWithTracingAsync(string operationName, Func<Task> action)
    {
        using var activity = StartActivity(operationName);
        
        if (activity != null)
        {
            activity.SetTag("operation", operationName);
        }

        try
        {
            await action();
            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.RecordException(ex);
            throw;
        }
    }

    /// <summary>
    /// Creates a new activity with the specified operation name and returns a result.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="operationName">The operation name.</param>
    /// <param name="func">The function to execute.</param>
    /// <returns>A task representing the execution with the result.</returns>
    public async Task<TResult> ExecuteWithTracingAsync<TResult>(string operationName, Func<Task<TResult>> func)
    {
        using var activity = StartActivity(operationName);
        
        if (activity != null)
        {
            activity.SetTag("operation", operationName);
        }

        try
        {
            var result = await func();
            activity?.SetStatus(ActivityStatusCode.Ok);
            return result;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.RecordException(ex);
            throw;
        }
    }

    /// <summary>
    /// Creates a meter for the specified name.
    /// </summary>
    /// <param name="name">The meter name.</param>
    /// <returns>A meter.</returns>
    public virtual Meter? GetMeter(string name)
    {
        if (_meterProvider == null)
        {
            return null;
        }

        return new Meter(name);
    }

    /// <summary>
    /// Creates a counter metric.
    /// </summary>
    /// <param name="meterName">The meter name.</param>
    /// <param name="counterName">The counter name.</param>
    /// <param name="description">The counter description.</param>
    /// <returns>A counter.</returns>
    public virtual Counter<long>? CreateCounter(string meterName, string counterName, string description)
    {
        var meter = GetMeter(meterName);
        if (meter == null)
        {
            return null;
        }

        return meter.CreateCounter<long>(counterName, description);
    }

    /// <summary>
    /// Creates a histogram metric.
    /// </summary>
    /// <param name="meterName">The meter name.</param>
    /// <param name="histogramName">The histogram name.</param>
    /// <param name="description">The histogram description.</param>
    /// <returns>A histogram.</returns>
    public virtual Histogram<double>? CreateHistogram(string meterName, string histogramName, string description)
    {
        var meter = GetMeter(meterName);
        if (meter == null)
        {
            return null;
        }

        return meter.CreateHistogram<double>(histogramName, description);
    }

    /// <summary>
    /// Creates a gauge metric.
    /// </summary>
    /// <param name="meterName">The meter name.</param>
    /// <param name="gaugeName">The gauge name.</param>
    /// <param name="description">The gauge description.</param>
    /// <param name="observation">The observation function.</param>
    /// <returns>A gauge.</returns>
    public virtual void CreateGauge<T>(string meterName, string gaugeName, string description, Func<T> observation) where T : struct
    {
        var meter = GetMeter(meterName);
        if (meter == null)
        {
            return;
        }

        var gauge = meter.CreateObservableGauge(gaugeName, observation, description);
    }

    /// <summary>
    /// Disposes the telemetry service.
    /// </summary>
    public void Dispose()
    {
        _meterProvider?.Dispose();
        _tracerProvider?.Dispose();
    }
}
