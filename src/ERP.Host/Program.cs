using ERP.Host.Components;
using ERP.Host.Services;
using ERP.SharedKernel.Events;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Register core services
builder.Services.AddSingleton<PluginManager>();
builder.Services.AddSingleton<IEventPublisher, EventPublisher>();

// Add logging
builder.Services.AddLogging();

var app = builder.Build();

// Initialize plugin system
var pluginManager = app.Services.GetRequiredService<PluginManager>();
var pluginsDirectory = Path.Combine(app.Environment.ContentRootPath, "plugins");
await pluginManager.DiscoverAndLoadPluginsAsync(pluginsDirectory);

// Configure plugins
foreach (var plugin in pluginManager.LoadedPlugins)
{
    try
    {
        plugin.ConfigureServices(builder.Services);
        plugin.Configure(app);
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Failed to configure plugin {ModuleId}", plugin.ModuleId);
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

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Handle graceful shutdown of plugins
app.Lifetime.ApplicationStopping.Register(async () =>
{
    await pluginManager.ShutdownAsync();
});

app.Run();
