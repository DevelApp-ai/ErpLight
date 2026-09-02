using ERP.Host.Components;
using ERP.Host.Data;
using ERP.Host.Models.Auth;
using ERP.Host.Services;
using ERP.Host.Services.Auth;
using ERP.SharedKernel.Events;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddHealthChecks()
    .AddCheck<PluginSystemHealthCheck>("plugin_system_readiness", tags: new[] { "ready" });

// Register core services
builder.Services.AddSingleton<PluginManager>();
builder.Services.AddSingleton<IEventPublisher, EventPublisher>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<ICorrelationIdService, CorrelationIdService>();

// Add Identity and Authentication
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure JWT settings
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));

// Add JWT authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>() ?? new JwtSettings();
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtSettings.Key)),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtSettings.Audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// Add authorization
builder.Services.AddAuthorization(options =>
{
    // Define policies
    options.AddPolicy("RequireAdmin", policy => policy.RequireRole("Admin"));
    options.AddPolicy("RequireManager", policy => policy.RequireRole("Admin", "Manager"));
    
    // Default fallback policy - require authentication for API endpoints
    // Note: Razor pages use their own [Authorize] attribute
});

// Register auth services
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<JwtAuthService>();
builder.Services.AddScoped<IAuthService>(sp => sp.GetRequiredService<AuthService>());

// Add DbContext for Identity
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? "Server=(localdb)\SQLEXPRESS;Database=ERPLight;Trusted_Connection=True;MultipleActiveResultSets=true";
    options.UseSqlServer(connectionString);
});

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

// Add authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();

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
    
    // Store correlation ID in items for plugin access
    context.Items["CorrelationId"] = correlationId;
    
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
