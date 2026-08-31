using ERP.Host.Components;
using ERP.Host.Services;
using ERP.SharedKernel.Events;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddHealthChecks()
    .AddCheck<PluginSystemHealthCheck>("plugin_system_readiness", tags: new[] { "ready" });

// Register core services
builder.Services.AddSingleton<PluginManager>();
builder.Services.AddSingleton<IEventPublisher, EventPublisher>();

// Add logging
builder.Services.AddLogging();

// Initialize plugin system BEFORE building the app
var tempServiceProvider = builder.Services.BuildServiceProvider();
var pluginManager = tempServiceProvider.GetRequiredService<PluginManager>();
var pluginsDirectory = Path.Combine(builder.Environment.ContentRootPath, "plugins");
await pluginManager.DiscoverAndLoadPluginsAsync(pluginsDirectory);

// Configure plugin services BEFORE building the app
foreach (var plugin in pluginManager.LoadedPlugins)
{
    try
    {
        plugin.ConfigureServices(builder.Services);
    }
    catch (Exception ex)
    {
        var logger = tempServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Failed to configure services for plugin {ModuleId}", plugin.ModuleId);
    }
}

// Register the configured plugin manager as singleton
builder.Services.AddSingleton(pluginManager);

var app = builder.Build();

// Configure plugin middleware AFTER building the app
var finalPluginManager = app.Services.GetRequiredService<PluginManager>();
foreach (var plugin in finalPluginManager.LoadedPlugins)
{
    try
    {
        plugin.Configure(app);
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Failed to configure middleware for plugin {ModuleId}", plugin.ModuleId);
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();
app.Use(async (context, next) =>
{
    const string correlationIdHeader = "X-Correlation-ID";
    var correlationId = context.Request.Headers[correlationIdHeader].FirstOrDefault();
    if (string.IsNullOrWhiteSpace(correlationId))
    {
        correlationId = Guid.NewGuid().ToString("N");
    }

    context.Response.Headers[correlationIdHeader] = correlationId;
    context.TraceIdentifier = correlationId;
    await next();
});

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
    ResultStatusCodes =
    {
        [HealthStatus.Healthy] = StatusCodes.Status200OK,
        [HealthStatus.Degraded] = StatusCodes.Status200OK,
        [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
    }
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResultStatusCodes =
    {
        [HealthStatus.Healthy] = StatusCodes.Status200OK,
        [HealthStatus.Degraded] = StatusCodes.Status200OK,
        [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
    }
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Handle graceful shutdown of plugins
app.Lifetime.ApplicationStopping.Register(async () =>
{
    var shutdownPluginManager = app.Services.GetRequiredService<PluginManager>();
    await shutdownPluginManager.ShutdownAsync();
});

app.Run();

public partial class Program { }
