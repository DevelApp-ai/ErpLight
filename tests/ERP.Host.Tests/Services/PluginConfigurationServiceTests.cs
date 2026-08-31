using ERP.Host.Services;
using ERP.SharedKernel.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ERP.Host.Tests.Services;

/// <summary>
/// Unit tests for PluginConfigurationService.
/// </summary>
public class PluginConfigurationServiceTests : IDisposable
{
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly Mock<PluginManager> _pluginManagerMock;
    private readonly ILogger<PluginConfigurationService> _logger;
    private readonly PluginConfigurationService _configurationService;

    public PluginConfigurationServiceTests()
    {
        _configurationMock = new Mock<IConfiguration>();
        _serviceProviderMock = new Mock<IServiceProvider>();
        _pluginManagerMock = new Mock<PluginManager>(
            NullLogger<PluginManager>.Instance,
            _serviceProviderMock.Object,
            _configurationMock.Object);
        _logger = NullLogger<PluginConfigurationService>.Instance;
        
        _configurationService = new PluginConfigurationService(
            _configurationMock.Object,
            _serviceProviderMock.Object,
            _logger);
    }

    public void Dispose()
    {
        _configurationMock?.Invoke();
        _serviceProviderMock?.Invoke();
        _pluginManagerMock?.Invoke();
    }

    [Fact]
    public void GetConfiguration_ShouldReturnNull_WhenNotLoaded()
    {
        // Act
        var config = _configurationService.GetConfiguration<TestPluginConfiguration>("TestPlugin");

        // Assert
        Assert.Null(config);
    }

    [Fact]
    public void GetConfiguration_ShouldReturnLoadedConfiguration()
    {
        // Arrange
        var testConfig = new TestPluginConfiguration("TestPlugin", "PluginSettings:TestPlugin", _logger);
        
        // Manually add to dictionary
        var configDictField = typeof(PluginConfigurationService).GetField("_pluginConfigurations", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var dict = (Dictionary<string, IPluginConfiguration>)configDictField!.GetValue(_configurationService)!;
        dict["TestPlugin"] = testConfig;

        // Act
        var config = _configurationService.GetConfiguration<TestPluginConfiguration>("TestPlugin");

        // Assert
        Assert.NotNull(config);
        Assert.Same(testConfig, config);
    }

    [Fact]
    public void GetConfiguration_ShouldReturnNull_WhenTypeMismatch()
    {
        // Arrange
        var testConfig = new TestPluginConfiguration("TestPlugin", "PluginSettings:TestPlugin", _logger);
        
        var configDictField = typeof(PluginConfigurationService).GetField("_pluginConfigurations", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var dict = (Dictionary<string, IPluginConfiguration>)configDictField!.GetValue(_configurationService)!;
        dict["TestPlugin"] = testConfig;

        // Act
        var config = _configurationService.GetConfiguration<OtherPluginConfiguration>("TestPlugin");

        // Assert
        Assert.Null(config);
    }

    [Fact]
    public void GetConfiguration_ByPluginModuleId_ShouldReturnConfiguration()
    {
        // Arrange
        var testConfig = new TestPluginConfiguration("TestPlugin", "PluginSettings:TestPlugin", _logger);
        
        var configDictField = typeof(PluginConfigurationService).GetField("_pluginConfigurations", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var dict = (Dictionary<string, IPluginConfiguration>)configDictField!.GetValue(_configurationService)!;
        dict["TestPlugin"] = testConfig;

        // Act
        var config = _configurationService.GetConfiguration("TestPlugin");

        // Assert
        Assert.NotNull(config);
        Assert.Same(testConfig, config);
    }

    [Fact]
    public void GetConfiguration_ByPluginModuleId_ShouldReturnNull_WhenNotFound()
    {
        // Act
        var config = _configurationService.GetConfiguration("NonExistentPlugin");

        // Assert
        Assert.Null(config);
    }

    [Fact]
    public void LoadPluginConfigurations_ShouldLoadAllPluginConfigurations()
    {
        // Arrange
        var plugin1 = new Mock<IPluginModule>();
        plugin1.Setup(p => p.ModuleId).Returns("Plugin1");
        
        var plugin2 = new Mock<IPluginModule>();
        plugin2.Setup(p => p.ModuleId).Returns("Plugin2");
        
        var plugins = new List<IPluginModule> { plugin1.Object, plugin2.Object };
        _pluginManagerMock.Setup(p => p.LoadedPlugins).Returns(plugins);

        // Act
        _configurationService.LoadPluginConfigurations(_pluginManagerMock.Object);

        // Assert - Should not throw and should have tried to load configurations
        var configDictField = typeof(PluginConfigurationService).GetField("_pluginConfigurations", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var dict = (Dictionary<string, IPluginConfiguration>)configDictField!.GetValue(_configurationService)!;
        
        // Since the plugins don't implement IPluginConfiguration, the dict should be empty
        Assert.Empty(dict);
    }

    [Fact]
    public void LoadPluginConfiguration_ShouldLoadPluginImplementingIPluginConfiguration()
    {
        // Arrange
        var pluginMock = new Mock<IPluginModule>();
        var pluginConfigMock = new Mock<IPluginConfiguration>();
        pluginMock.Setup(p => p.ModuleId).Returns("TestPlugin");
        
        // Make the plugin also implement IPluginConfiguration
        pluginMock.As<IPluginConfiguration>().Setup(p => p.PluginModuleId).Returns("TestPlugin");
        pluginMock.As<IPluginConfiguration>().Setup(p => p.ConfigurationSection).Returns("PluginSettings:TestPlugin");
        pluginMock.As<IPluginConfiguration>().Setup(p => p.Configure(It.IsAny<IConfiguration>()))
            .Callback<IConfiguration>(c => { });
        
        var plugin = pluginMock.Object;

        // Act
        _configurationService.LoadPluginConfiguration(plugin);

        // Assert
        var configDictField = typeof(PluginConfigurationService).GetField("_pluginConfigurations", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var dict = (Dictionary<string, IPluginConfiguration>)configDictField!.GetValue(_configurationService)!;
        
        Assert.Contains("TestPlugin", dict.Keys);
    }

    [Fact]
    public void GetDatabaseConfiguration_ShouldReturnDefault_WhenNotConfigured()
    {
        // Arrange
        _configurationMock.Setup(c => c.GetSection("PluginSettings:TestPlugin:Database"))
            .Returns(Mock.Of<IConfigurationSection>());

        // Act
        var config = _configurationService.GetDatabaseConfiguration("TestPlugin");

        // Assert
        Assert.NotNull(config);
        Assert.Null(config.ConnectionString);
        Assert.Equal("SqlServer", config.Provider);
    }

    [Fact]
    public void GetDatabaseConfiguration_ShouldReturnConfiguredValues()
    {
        // Arrange
        var sectionMock = new Mock<IConfigurationSection>();
        sectionMock.Setup(s => s.Get<PluginDatabaseConfiguration>()).Returns(new PluginDatabaseConfiguration
        {
            ConnectionString = "Server=localhost;Database=TestDB",
            Provider = "SqlServer",
            EnableMigrations = true,
            MaxRetryCount = 5,
            RetryDelayMs = 2000
        });
        
        _configurationMock.Setup(c => c.GetSection("PluginSettings:TestPlugin:Database"))
            .Returns(sectionMock.Object);

        // Act
        var config = _configurationService.GetDatabaseConfiguration("TestPlugin");

        // Assert
        Assert.NotNull(config);
        Assert.Equal("Server=localhost;Database=TestDB", config.ConnectionString);
        Assert.Equal("SqlServer", config.Provider);
        Assert.True(config.EnableMigrations);
        Assert.Equal(5, config.MaxRetryCount);
        Assert.Equal(2000, config.RetryDelayMs);
    }

    [Fact]
    public void GetFeatureConfiguration_ShouldReturnDefault_WhenNotConfigured()
    {
        // Arrange
        _configurationMock.Setup(c => c.GetSection("PluginSettings:TestPlugin:Features"))
            .Returns(Mock.Of<IConfigurationSection>());

        // Act
        var config = _configurationService.GetFeatureConfiguration("TestPlugin");

        // Assert
        Assert.NotNull(config);
        Assert.True(config.Enabled);
        Assert.False(config.DebugEnabled);
        Assert.True(config.AuditLoggingEnabled);
    }

    [Fact]
    public void GetFeatureConfiguration_ShouldReturnConfiguredValues()
    {
        // Arrange
        var sectionMock = new Mock<IConfigurationSection>();
        sectionMock.Setup(s => s.Get<PluginFeatureConfiguration>()).Returns(new PluginFeatureConfiguration
        {
            Enabled = true,
            DebugEnabled = true,
            AuditLoggingEnabled = false,
            CachingEnabled = false,
            CacheDurationMinutes = 60,
            MaxCacheItems = 500
        });
        
        _configurationMock.Setup(c => c.GetSection("PluginSettings:TestPlugin:Features"))
            .Returns(sectionMock.Object);

        // Act
        var config = _configurationService.GetFeatureConfiguration("TestPlugin");

        // Assert
        Assert.NotNull(config);
        Assert.True(config.Enabled);
        Assert.True(config.DebugEnabled);
        Assert.False(config.AuditLoggingEnabled);
        Assert.False(config.CachingEnabled);
        Assert.Equal(60, config.CacheDurationMinutes);
        Assert.Equal(500, config.MaxCacheItems);
    }

    [Fact]
    public void GetApiConfiguration_ShouldReturnDefault_WhenNotConfigured()
    {
        // Arrange
        _configurationMock.Setup(c => c.GetSection("PluginSettings:TestPlugin:Api"))
            .Returns(Mock.Of<IConfigurationSection>());

        // Act
        var config = _configurationService.GetApiConfiguration("TestPlugin");

        // Assert
        Assert.NotNull(config);
        Assert.Equal("api/plugins/{moduleId}", config.ApiPrefix);
        Assert.True(config.Enabled);
        Assert.False(config.SwaggerEnabled);
    }

    [Fact]
    public void GetApiConfiguration_ShouldReturnConfiguredValues()
    {
        // Arrange
        var sectionMock = new Mock<IConfigurationSection>();
        sectionMock.Setup(s => s.Get<PluginApiConfiguration>()).Returns(new PluginApiConfiguration
        {
            ApiPrefix = "api/test",
            Enabled = true,
            SwaggerEnabled = true,
            Version = "v2",
            RateLimitPerMinute = 200
        });
        
        _configurationMock.Setup(c => c.GetSection("PluginSettings:TestPlugin:Api"))
            .Returns(sectionMock.Object);

        // Act
        var config = _configurationService.GetApiConfiguration("TestPlugin");

        // Assert
        Assert.NotNull(config);
        Assert.Equal("api/test", config.ApiPrefix);
        Assert.True(config.Enabled);
        Assert.True(config.SwaggerEnabled);
        Assert.Equal("v2", config.Version);
        Assert.Equal(200, config.RateLimitPerMinute);
    }

    [Fact]
    public void GetValue_ShouldReturnDefaultValue_WhenNotConfigured()
    {
        // Arrange
        var sectionMock = new Mock<IConfigurationSection>();
        sectionMock.Setup(s => s.GetValue<int>(It.IsAny<string>(), It.IsAny<int>()))
            .Returns(42);
        
        _configurationMock.Setup(c => c.GetSection("PluginSettings:TestPlugin"))
            .Returns(sectionMock.Object);

        // Act
        var value = _configurationService.GetValue("TestPlugin", "SomeKey", 100);

        // Assert
        Assert.Equal(42, value);
    }

    [Fact]
    public void GetValue_ShouldReturnDefaultValue_WhenKeyNotFound()
    {
        // Arrange
        var sectionMock = new Mock<IConfigurationSection>();
        sectionMock.Setup(s => s.GetValue<int>(It.IsAny<string>(), It.IsAny<int>()))
            .Returns(100); // Returns default when not found
        
        _configurationMock.Setup(c => c.GetSection("PluginSettings:TestPlugin"))
            .Returns(sectionMock.Object);

        // Act
        var value = _configurationService.GetValue("TestPlugin", "NonExistentKey", 100);

        // Assert
        Assert.Equal(100, value);
    }

    [Fact]
    public void GetValue_ShouldReturnStringValue()
    {
        // Arrange
        var sectionMock = new Mock<IConfigurationSection>();
        sectionMock.Setup(s => s.GetValue<string>(It.IsAny<string>(), It.IsAny<string>()))
            .Returns("test-value");
        
        _configurationMock.Setup(c => c.GetSection("PluginSettings:TestPlugin"))
            .Returns(sectionMock.Object);

        // Act
        var value = _configurationService.GetValue("TestPlugin", "StringKey", "default");

        // Assert
        Assert.Equal("test-value", value);
    }
}

/// <summary>
/// Test implementation of IPluginConfiguration for testing purposes.
/// </summary>
public class TestPluginConfiguration : PluginConfigurationBase<TestPluginConfigurationSettings>
{
    public TestPluginConfiguration(string pluginModuleId, string configurationSection, ILogger logger)
        : base(pluginModuleId, configurationSection, logger)
    {
    }

    public override bool ValidateConfiguration()
    {
        return Configuration != null && !string.IsNullOrEmpty(Configuration.Setting1);
    }
}

/// <summary>
/// Test configuration settings.
/// </summary>
public class TestPluginConfigurationSettings
{
    public string Setting1 { get; set; } = string.Empty;
    public string Setting2 { get; set; } = string.Empty;
    public int Setting3 { get; set; } = 0;
}

/// <summary>
/// Another test implementation for type mismatch testing.
/// </summary>
public class OtherPluginConfiguration : IPluginConfiguration
{
    public string PluginModuleId { get; } = "OtherPlugin";
    public string ConfigurationSection { get; } = "PluginSettings:OtherPlugin";

    public void Configure(IConfiguration configuration)
    {
    }

    public bool ValidateConfiguration()
    {
        return true;
    }

    public T GetConfiguration<T>() where T : class, new()
    {
        return new T();
    }
}
