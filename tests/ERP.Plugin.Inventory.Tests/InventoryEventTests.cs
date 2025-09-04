using ERP.Plugin.Inventory.Events;
using Xunit;

namespace ERP.Plugin.Inventory.Tests;

public class InventoryEventTests
{
    [Fact]
    public void ProductAddedEvent_ShouldHaveCorrectProperties()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var productName = "Test Product";
        var productCode = "PROD-001";
        var quantity = 100;
        var unitPrice = 25.99m;
        var category = "Electronics";

        // Act
        var productEvent = new ProductAddedEvent(productId, productName, productCode, quantity, unitPrice, category);

        // Assert
        Assert.Equal(productId, productEvent.ProductId);
        Assert.Equal(productName, productEvent.ProductName);
        Assert.Equal(productCode, productEvent.ProductCode);
        Assert.Equal(quantity, productEvent.Quantity);
        Assert.Equal(unitPrice, productEvent.UnitPrice);
        Assert.Equal(category, productEvent.Category);
        Assert.Equal("ProductAddedEvent", productEvent.EventType);
        Assert.NotEqual(Guid.Empty, productEvent.EventId);
        Assert.True(productEvent.OccurredAt <= DateTime.UtcNow);
    }

    [Fact]
    public void StockUpdatedEvent_ShouldHaveCorrectProperties()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var productCode = "PROD-001";
        var previousQuantity = 50;
        var newQuantity = 75;
        var updateReason = "Purchase order received";

        // Act
        var stockEvent = new StockUpdatedEvent(productId, productCode, previousQuantity, newQuantity, updateReason);

        // Assert
        Assert.Equal(productId, stockEvent.ProductId);
        Assert.Equal(productCode, stockEvent.ProductCode);
        Assert.Equal(previousQuantity, stockEvent.PreviousQuantity);
        Assert.Equal(newQuantity, stockEvent.NewQuantity);
        Assert.Equal(updateReason, stockEvent.UpdateReason);
        Assert.Equal("StockUpdatedEvent", stockEvent.EventType);
        Assert.NotEqual(Guid.Empty, stockEvent.EventId);
        Assert.True(stockEvent.OccurredAt <= DateTime.UtcNow);
        Assert.True(stockEvent.UpdatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void LowStockAlertEvent_ShouldHaveCorrectProperties()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var productName = "Test Product";
        var productCode = "PROD-001";
        var currentQuantity = 5;
        var thresholdLevel = 10;

        // Act
        var alertEvent = new LowStockAlertEvent(productId, productName, productCode, currentQuantity, thresholdLevel);

        // Assert
        Assert.Equal(productId, alertEvent.ProductId);
        Assert.Equal(productName, alertEvent.ProductName);
        Assert.Equal(productCode, alertEvent.ProductCode);
        Assert.Equal(currentQuantity, alertEvent.CurrentQuantity);
        Assert.Equal(thresholdLevel, alertEvent.ThresholdLevel);
        Assert.Equal("LowStockAlertEvent", alertEvent.EventType);
        Assert.NotEqual(Guid.Empty, alertEvent.EventId);
        Assert.True(alertEvent.OccurredAt <= DateTime.UtcNow);
    }

    [Fact]
    public void InventoryEvents_ShouldHaveUniqueEventIds()
    {
        // Arrange & Act
        var event1 = new ProductAddedEvent(Guid.NewGuid(), "Product1", "PROD-001", 100, 25.99m, "Electronics");
        var event2 = new ProductAddedEvent(Guid.NewGuid(), "Product2", "PROD-002", 50, 15.99m, "Accessories");

        // Assert
        Assert.NotEqual(event1.EventId, event2.EventId);
    }
}