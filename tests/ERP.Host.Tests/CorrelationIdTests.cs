using ERP.Host.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace ERP.Host.Tests;

/// <summary>
/// Tests for correlation ID propagation functionality.
/// </summary>
public class CorrelationIdTests
{
    [Fact]
    public void CorrelationIdService_ShouldReturnNull_WhenNotInRequestContext()
    {
        // Arrange
        var httpContextAccessor = new HttpContextAccessor();
        var service = new CorrelationIdService(httpContextAccessor);

        // Act
        var correlationId = service.CurrentCorrelationId;

        // Assert
        Assert.Null(correlationId);
    }

    [Fact]
    public void CorrelationIdService_ShouldReturnCorrelationId_WhenInRequestContext()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Items["CorrelationId"] = "test-correlation-id-12345";
        
        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
        var service = new CorrelationIdService(httpContextAccessor);

        // Act
        var correlationId = service.CurrentCorrelationId;

        // Assert
        Assert.Equal("test-correlation-id-12345", correlationId);
    }

    [Fact]
    public void CorrelationIdService_ShouldReturnNull_WhenCorrelationIdNotInItems()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Items["SomeOtherKey"] = "some-value";
        
        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
        var service = new CorrelationIdService(httpContextAccessor);

        // Act
        var correlationId = service.CurrentCorrelationId;

        // Assert
        Assert.Null(correlationId);
    }

    [Fact]
    public void CorrelationIdService_ShouldReturnNull_WhenCorrelationIdIsNotString()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Items["CorrelationId"] = 12345; // Not a string
        
        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
        var service = new CorrelationIdService(httpContextAccessor);

        // Act
        var correlationId = service.CurrentCorrelationId;

        // Assert
        Assert.Null(correlationId);
    }

    [Fact]
    public void CorrelationIdService_ShouldBeInjectable()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddHttpContextAccessor();
        services.AddSingleton<ICorrelationIdService, CorrelationIdService>();
        
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var service = serviceProvider.GetRequiredService<ICorrelationIdService>();

        // Assert
        Assert.NotNull(service);
        Assert.IsType<CorrelationIdService>(service);
    }

    [Fact]
    public void CorrelationIdService_ShouldBeSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddHttpContextAccessor();
        services.AddSingleton<ICorrelationIdService, CorrelationIdService>();
        
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var service1 = serviceProvider.GetRequiredService<ICorrelationIdService>();
        var service2 = serviceProvider.GetRequiredService<ICorrelationIdService>();

        // Assert
        Assert.Same(service1, service2);
    }

    [Fact]
    public void CorrelationId_ShouldBeGuidFormat()
    {
        // Arrange
        var validCorrelationId = Guid.NewGuid().ToString("N");
        var httpContext = new DefaultHttpContext();
        httpContext.Items["CorrelationId"] = validCorrelationId;
        
        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
        var service = new CorrelationIdService(httpContextAccessor);

        // Act
        var correlationId = service.CurrentCorrelationId;

        // Assert
        Assert.Equal(validCorrelationId, correlationId);
        Assert.Equal(32, correlationId.Length); // Guid in N format is 32 chars
    }

    [Fact]
    public void CorrelationId_ShouldSupportCustomFormats()
    {
        // Arrange
        var customCorrelationId = "CUSTOM-CORR-12345";
        var httpContext = new DefaultHttpContext();
        httpContext.Items["CorrelationId"] = customCorrelationId;
        
        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
        var service = new CorrelationIdService(httpContextAccessor);

        // Act
        var correlationId = service.CurrentCorrelationId;

        // Assert
        Assert.Equal(customCorrelationId, correlationId);
    }
}
