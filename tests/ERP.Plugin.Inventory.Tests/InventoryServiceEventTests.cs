using ERP.Plugin.Inventory.Events;
using ERP.Plugin.Inventory.Services;
using ERP.SharedKernel.Events;
using Moq;
using Xunit;

namespace ERP.Plugin.Inventory.Tests;

/// <summary>
/// Tests for InventoryService event publishing functionality.
/// </summary>
public class InventoryServiceEventTests
{
    [Fact]
    public async Task AddProductAsync_ShouldPublishProductAddedEvent()
    {
        // Arrange
        var mockEventPublisher = new Mock<IEventPublisher>();
        var service = new InventoryService(mockEventPublisher.Object);

        var request = new AddProductRequest
        {
            Name = "Test Product",
            Category = "Electronics",
            InitialQuantity = 100,
            UnitPrice = 25.99m,
            LowStockThreshold = 10
        };

        ProductAddedEvent? capturedEvent = null;
        mockEventPublisher.Setup(x => x.PublishAsync(It.IsAny<DomainEvent>()))
            .Callback<DomainEvent>(evt => capturedEvent = evt as ProductAddedEvent);

        // Act
        await service.AddProductAsync(request);

        // Assert
        mockEventPublisher.Verify(x => x.PublishAsync(It.IsAny<ProductAddedEvent>()), Times.Once);
        Assert.NotNull(capturedEvent);
        Assert.NotEqual(Guid.Empty, capturedEvent.ProductId);
        Assert.Equal("Test Product", capturedEvent.ProductName);
        Assert.Equal("Electronics", capturedEvent.Category);
        Assert.Equal(100, capturedEvent.Quantity);
        Assert.Equal(25.99m, capturedEvent.UnitPrice);
    }

    [Fact]
    public async Task UpdateStockAsync_ShouldPublishStockUpdatedEvent()
    {
        // Arrange
        var mockEventPublisher = new Mock<IEventPublisher>();
        var service = new InventoryService(mockEventPublisher.Object);

        // First add a product
        var request = new AddProductRequest
        {
            Name = "Test Product",
            Category = "Electronics",
            InitialQuantity = 100,
            UnitPrice = 25.99m,
            LowStockThreshold = 10
        };
        var product = await service.AddProductAsync(request);

        StockUpdatedEvent? capturedEvent = null;
        mockEventPublisher.Setup(x => x.PublishAsync(It.IsAny<DomainEvent>()))
            .Callback<DomainEvent>(evt => capturedEvent = evt as StockUpdatedEvent);

        // Act
        await service.UpdateStockAsync(product.Id, 75, "Manual adjustment");

        // Assert
        mockEventPublisher.Verify(x => x.PublishAsync(It.IsAny<StockUpdatedEvent>()), Times.Once);
        Assert.NotNull(capturedEvent);
        Assert.Equal(product.Id, capturedEvent.ProductId);
        Assert.Equal(100, capturedEvent.PreviousQuantity);
        Assert.Equal(75, capturedEvent.NewQuantity);
        Assert.Equal("Manual adjustment", capturedEvent.UpdateReason);
    }

    [Fact]
    public async Task UpdateStockAsync_ShouldPublishLowStockAlertEvent_WhenBelowThreshold()
    {
        // Arrange
        var mockEventPublisher = new Mock<IEventPublisher>();
        var service = new InventoryService(mockEventPublisher.Object);

        // Add a product with low stock threshold
        var request = new AddProductRequest
        {
            Name = "Low Stock Product",
            Category = "Test",
            InitialQuantity = 20,
            UnitPrice = 10.00m,
            LowStockThreshold = 10
        };
        var product = await service.AddProductAsync(request);

        LowStockAlertEvent? capturedAlertEvent = null;
        mockEventPublisher.Setup(x => x.PublishAsync(It.IsAny<DomainEvent>()))
            .Callback<DomainEvent>(evt => 
            {
                if (evt is LowStockAlertEvent alert) capturedAlertEvent = alert;
            });

        // Act - update stock to below threshold
        await service.UpdateStockAsync(product.Id, 5, "Sold items");

        // Assert
        Assert.NotNull(capturedAlertEvent);
        Assert.Equal(product.Id, capturedAlertEvent.ProductId);
        Assert.Equal(5, capturedAlertEvent.CurrentQuantity);
        Assert.Equal(10, capturedAlertEvent.ThresholdLevel);
    }

    [Fact]
    public async Task UpdateStockAsync_ShouldNotPublishLowStockAlert_WhenAboveThreshold()
    {
        // Arrange
        var mockEventPublisher = new Mock<IEventPublisher>();
        var service = new InventoryService(mockEventPublisher.Object);

        // Add a product
        var request = new AddProductRequest
        {
            Name = "Test Product",
            Category = "Test",
            InitialQuantity = 100,
            UnitPrice = 10.00m,
            LowStockThreshold = 10
        };
        var product = await service.AddProductAsync(request);

        LowStockAlertEvent? capturedAlertEvent = null;
        mockEventPublisher.Setup(x => x.PublishAsync(It.IsAny<DomainEvent>()))
            .Callback<DomainEvent>(evt => 
            {
                if (evt is LowStockAlertEvent alert) capturedAlertEvent = alert;
            });

        // Act - update stock but stay above threshold
        await service.UpdateStockAsync(product.Id, 50, "Restocked");

        // Assert
        Assert.Null(capturedAlertEvent);
    }

    [Fact]
    public async Task UpdateStockAsync_ShouldReturnFalse_WhenProductNotFound()
    {
        // Arrange
        var mockEventPublisher = new Mock<IEventPublisher>();
        var service = new InventoryService(mockEventPublisher.Object);

        var nonExistentProductId = Guid.NewGuid();

        // Act
        var result = await service.UpdateStockAsync(nonExistentProductId, 50, "Test");

        // Assert
        Assert.False(result);
        mockEventPublisher.Verify(x => x.PublishAsync(It.IsAny<StockUpdatedEvent>()), Times.Never);
    }

    [Fact]
    public async Task GetLowStockProductsAsync_ShouldReturnProductsBelowThreshold()
    {
        // Arrange
        var mockEventPublisher = new Mock<IEventPublisher>();
        var service = new InventoryService(mockEventPublisher.Object);

        // Add products with different stock levels
        await service.AddProductAsync(new AddProductRequest
        {
            Name = "Low Stock",
            Category = "Test",
            InitialQuantity = 5,
            UnitPrice = 10.00m,
            LowStockThreshold = 10
        });

        await service.AddProductAsync(new AddProductRequest
        {
            Name = "Adequate Stock",
            Category = "Test",
            InitialQuantity = 50,
            UnitPrice = 10.00m,
            LowStockThreshold = 10
        });

        // Act
        var lowStockProducts = (await service.GetLowStockProductsAsync()).ToList();

        // Assert
        Assert.Single(lowStockProducts);
        Assert.Equal("Low Stock", lowStockProducts[0].Name);
    }

    [Fact]
    public async Task AddProductAsync_ShouldGenerateProductCode()
    {
        // Arrange
        var mockEventPublisher = new Mock<IEventPublisher>();
        var service = new InventoryService(mockEventPublisher.Object);

        var request = new AddProductRequest
        {
            Name = "Test Product",
            Category = "Electronics",
            InitialQuantity = 100,
            UnitPrice = 25.99m,
            LowStockThreshold = 10
        };

        // Act
        var product = await service.AddProductAsync(request);

        // Assert
        Assert.NotEmpty(product.Code);
        Assert.StartsWith("ELE-", product.Code); // Category code prefix
    }

    [Fact]
    public async Task AddProductAsync_ShouldSetCreatedAndUpdatedTimestamps()
    {
        // Arrange
        var mockEventPublisher = new Mock<IEventPublisher>();
        var service = new InventoryService(mockEventPublisher.Object);

        var request = new AddProductRequest
        {
            Name = "Test Product",
            Category = "Test",
            InitialQuantity = 100,
            UnitPrice = 10.00m,
            LowStockThreshold = 10
        };

        var beforeCreation = DateTime.UtcNow;

        // Act
        var product = await service.AddProductAsync(request);

        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.True(product.CreatedAt >= beforeCreation);
        Assert.True(product.CreatedAt <= afterCreation);
        Assert.True(product.UpdatedAt >= beforeCreation);
        Assert.True(product.UpdatedAt <= afterCreation);
    }
}
