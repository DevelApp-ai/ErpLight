using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace ERP.SharedKernel.Contracts;

/// <summary>
/// Core interface that all plugin modules must implement to integrate with the host application.
/// This is the primary contract for plugin lifecycle management.
/// </summary>
public interface IPluginModule
{
    /// <summary>
    /// Gets the unique identifier for this plugin module.
    /// </summary>
    string ModuleId { get; }

    /// <summary>
    /// Gets the display name for this plugin module.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Gets the version of this plugin module.
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Called during application startup to register services with the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to register services with.</param>
    void ConfigureServices(IServiceCollection services);

    /// <summary>
    /// Called during application startup to configure the application pipeline.
    /// </summary>
    /// <param name="app">The application builder to configure.</param>
    void Configure(IApplicationBuilder app);

    /// <summary>
    /// Called when the plugin is being initialized. Used for any one-time setup logic.
    /// </summary>
    /// <param name="serviceProvider">The service provider for resolving dependencies.</param>
    /// <returns>A task representing the initialization operation.</returns>
    Task InitializeAsync(IServiceProvider serviceProvider);

    /// <summary>
    /// Called when the application is shutting down. Used for cleanup logic.
    /// </summary>
    /// <returns>A task representing the shutdown operation.</returns>
    Task ShutdownAsync();
}