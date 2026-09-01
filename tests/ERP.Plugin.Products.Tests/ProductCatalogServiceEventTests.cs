using ERP.Plugin.Products.Events;
using ERP.Plugin.Products.Services;
using ERP.SharedKernel.Events;
using Moq;
using Xunit;

namespace ERP.Plugin.Products.Tests;

/// <summary>
/// Tests for ProductCatalogService event publishing functionality.
/// </summary>
public class ProductCatalogServiceEventTests
{
    [Fact]
    public async Task CreateProductAsync_ShouldPublishProductCatalogCreatedEvent()
    {
        // Arrange
        var mockEventPublisher = new Mock<IEventPublisher>();
        var service = new ProductCatalogService(mockEventPublisher.Object);

        ProductCatalogCreatedEvent? capturedEvent = null;
        mockEventPublisher.Setup(x => x.PublishAsync(It.IsAny<DomainEvent>()))
            .Callback<DomainEvent>(evt => capturedEvent = evt as ProductCatalogCreatedEvent);

        // Act
        var productId = await service.CreateProductAsync(
            "Laptop Computer",
            "LAP-001",
            "TechBrand",
            "Electronics",
            999.99m,
            "High-performance laptop");

        // Assert
        mockEventPublisher.Verify(x => x.PublishAsync(It.IsAny<ProductCatalogCreatedEvent>()), Times.Once);
        Assert.NotNull(capturedEvent);
        Assert.Equal(productId, capturedEvent.ProductId);
        Assert.Equal("Laptop Computer", capturedEvent.ProductName);
        Assert.Equal("LAP-001", capturedEvent.ProductCode);
        Assert.Equal("TechBrand", capturedEvent.Brand);
        Assert.Equal("Electronics", capturedEvent.Category);
        Assert.Equal(999.99m, capturedEvent.Price);
        Assert.Equal("High-performance laptop", capturedEvent.Description);
    }

    [Fact]
    public async Task UpdateProductAsync_ShouldPublishProductUpdatedEvent()
    {
        // Arrange
        var mockEventPublisher = new Mock<IEventPublisher>();
        var service = new ProductCatalogService(mockEventPublisher.Object);

        // Create a product first
        var productId = await service.CreateProductAsync(
            "Laptop Computer",
            "LAP-001",
            "TechBrand",
            "Electronics",
            999.99m,
            "High-performance laptop");

        ProductUpdatedEvent? capturedEvent = null;
        mockEventPublisher.Setup(x => x.PublishAsync(It.IsAny<DomainEvent>()))
            .Callback<DomainEvent>(evt => capturedEvent = evt as ProductUpdatedEvent);

        // Act
        await service.UpdateProductAsync(productId, "Price", 999.99m, 899.99m, "admin@example.com");

        // Assert
        mockEventPublisher.Verify(x => x.PublishAsync(It.IsAny<ProductUpdatedEvent>()), Times.Once);
        Assert.NotNull(capturedEvent);
        Assert.Equal(productId, capturedEvent.ProductId);
        Assert.Equal("LAP-001", capturedEvent.ProductCode);
        Assert.Equal("Price", capturedEvent.UpdatedField);
        Assert.Equal(999.99m, capturedEvent.OldValue);
        Assert.Equal(899.99m, capturedEvent.NewValue);
        Assert.Equal("admin@example.com", capturedEvent.UpdatedBy);
    }

    [Fact]
    public async Task DiscontinueProductAsync_ShouldPublishProductDiscontinuedEvent()
    {
        // Arrange
        var mockEventPublisher = new Mock<IEventPublisher>();
        var service = new ProductCatalogService(mockEventPublisher.Object);

        // Create a product first
        var productId = await service.CreateProductAsync(
            "Old Laptop",
            "LAP-OLD",
            "OldBrand",
            "Electronics",
            500.00m,
            "Old laptop model");

        ProductDiscontinuedEvent? capturedEvent = null;
        mockEventPublisher.Setup(x => x.PublishAsync(It.IsAny<DomainEvent>()))
            .Callback<DomainEvent>(evt => capturedEvent = evt as ProductDiscontinuedEvent);

        // Act
        await service.DiscontinueProductAsync(productId, "End of life", "manager@example.com");

        // Assert
        mockEventPublisher.Verify(x => x.PublishAsync(It.IsAny<ProductDiscontinuedEvent>()), Times.Once);
        Assert.NotNull(capturedEvent);
        Assert.Equal(productId, capturedEvent.ProductId);
        Assert.Equal("Old Laptop", capturedEvent.ProductName);
        Assert.Equal("LAP-OLD", capturedEvent.ProductCode);
        Assert.Equal("End of life", capturedEvent.Reason);
        Assert.Equal("manager@example.com", capturedEvent.DiscontinuedBy);
    }

    [Fact]
    public async Task UpdateProductAsync_ShouldNotPublishEvent_WhenProductNotFound()
    {
        // Arrange
        var mockEventPublisher = new Mock<IEventPublisher>();
        var service = new ProductCatalogService(mockEventPublisher.Object);

        var nonExistentProductId = Guid.NewGuid();

        // Act
        await service.UpdateProductAsync(nonExistentProductId, "Price", 100m, 200m, "admin@example.com");

        // Assert
        mockEventPublisher.Verify(x => x.PublishAsync(It.IsAny<ProductUpdatedEvent>()), Times.Never);
    }

    [Fact]
    public async Task DiscontinueProductAsync_ShouldNotPublishEvent_WhenProductNotFound()
    {
        // Arrange
        var mockEventPublisher = new Mock<IEventPublisher>();
        var service = new ProductCatalogService(mockEventPublisher.Object);

        var nonExistentProductId = Guid.NewGuid();

        // Act
        await service.DiscontinueProductAsync(nonExistentProductId, "Reason", "admin@example.com");

        // Assert
        mockEventPublisher.Verify(x => x.PublishAsync(It.IsAny<ProductDiscontinuedEvent>()), Times.Never);
    }

    [Fact]
    public async Task GetProductAsync_ShouldReturnNull_WhenProductNotFound()
    {
        // Arrange
        var mockEventPublisher = new Mock<IEventPublisher>();
        var service = new ProductCatalogService(mockEventPublisher.Object);

        var nonExistentProductId = Guid.NewGuid();

        // Act
        var product = await service.GetProductAsync(nonExistentProductId);

        // Assert
        Assert.Null(product);
    }

    [Fact]
    public async Task GetProductAsync_ShouldReturnProduct_WhenProductExists()
    {
        // Arrange
        var mockEventPublisher = new Mock<IEventPublisher>();
        var service = new ProductCatalogService(mockEventPublisher.Object);

        // Create a product
        var productId = await service.CreateProductAsync(
            "Test Product",
            "TEST-001",
            "TestBrand",
            "TestCategory",
            100.00m,
            "Test description");

        // Act
        var product = await service.GetProductAsync(productId);

        // Assert
        Assert.NotNull(product);
        Assert.Equal(productId, product.ProductId);
        Assert.Equal("Test Product", product.ProductName);
        Assert.Equal("TEST-001", product.ProductCode);
    }

    [Fact]
    public async Task GetProductsByCategoryAsync_ShouldReturnOnlyMatchingProducts()
    {
        // Arrange
        var mockEventPublisher = new Mock<IEventPublisher>();
        var service = new ProductCatalogService(mockEventPublisher.Object);

        // Create products in different categories
        await service.CreateProductAsync("Product 1", "PROD-001", "Brand", "Electronics", 100m, "Desc");
        await service.CreateProductAsync("Product 2", "PROD-002", "Brand", "Books", 50m, "Desc");
        await service.CreateProductAsync("Product 3", "PROD-003", "Brand", "Electronics", 200m, "Desc");

        // Act
        var electronicsProducts = (await service.GetProductsByCategoryAsync("Electronics")).ToList();

        // Assert
        Assert.Equal(2, electronicsProducts.Count);
        Assert.All(electronicsProducts, p => Assert.Equal("Electronics", p.Category));
    }

    [Fact]
    public async Task CreateProductAsync_ShouldSetProductAsNotDiscontinued()
    {
        // Arrange
        var mockEventPublisher = new Mock<IEventPublisher>();
        var service = new ProductCatalogService(mockEventPublisher.Object);

        // Act
        var productId = await service.CreateProductAsync(
            "Test Product",
            "TEST-001",
            "Brand",
            "Category",
            100m,
            "Description");

        var product = await service.GetProductAsync(productId);

        // Assert
        Assert.NotNull(product);
        Assert.False(product.IsDiscontinued);
        Assert.Null(product.DiscontinuedAt);
    }

    [Fact]
    public async Task DiscontinueProductAsync_ShouldMarkProductAsDiscontinued()
    {
        // Arrange
        var mockEventPublisher = new Mock<IEventPublisher>();
        var service = new ProductCatalogService(mockEventPublisher.Object);

        // Create a product
        var productId = await service.CreateProductAsync(
            "Test Product",
            "TEST-001",
            "Brand",
            "Category",
            100m,
            "Description");

        // Act
        await service.DiscontinueProductAsync(productId, "End of life", "admin@example.com");

        var product = await service.GetProductAsync(productId);

        // Assert
        Assert.NotNull(product);
        Assert.True(product.IsDiscontinued);
        Assert.NotNull(product.DiscontinuedAt);
    }

    [Fact]
    public async Task CreateProductAsync_ShouldSetCreatedTimestamp()
    {
        // Arrange
        var mockEventPublisher = new Mock<IEventPublisher>();
        var service = new ProductCatalogService(mockEventPublisher.Object);

        var beforeCreation = DateTime.UtcNow;

        // Act
        var productId = await service.CreateProductAsync(
            "Test Product",
            "TEST-001",
            "Brand",
            "Category",
            100m,
            "Description");

        var product = await service.GetProductAsync(productId);
        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.NotNull(product);
        Assert.True(product.CreatedAt >= beforeCreation);
        Assert.True(product.CreatedAt <= afterCreation);
    }

    [Fact]
    public async Task UpdateProductAsync_ShouldUpdatePrice()
    {
        // Arrange
        var mockEventPublisher = new Mock<IEventPublisher>();
        var service = new ProductCatalogService(mockEventPublisher.Object);

        // Create a product
        var productId = await service.CreateProductAsync(
            "Test Product",
            "TEST-001",
            "Brand",
            "Category",
            100m,
            "Description");

        // Act
        await service.UpdateProductAsync(productId, "Price", 100m, 150m, "admin@example.com");

        var product = await service.GetProductAsync(productId);

        // Assert
        Assert.NotNull(product);
        Assert.Equal(150m, product.Price);
    }

    [Fact]
    public async Task UpdateProductAsync_ShouldUpdateDescription()
    {
        // Arrange
        var mockEventPublisher = new Mock<IEventPublisher>();
        var service = new ProductCatalogService(mockEventPublisher.Object);

        // Create a product
        var productId = await service.CreateProductAsync(
            "Test Product",
            "TEST-001",
            "Brand",
            "Category",
            100m,
            "Original description");

        // Act
        await service.UpdateProductAsync(productId, "Description", "Original description", "New description", "admin@example.com");

        var product = await service.GetProductAsync(productId);

        // Assert
        Assert.NotNull(product);
        Assert.Equal("New description", product.Description);
    }
}
