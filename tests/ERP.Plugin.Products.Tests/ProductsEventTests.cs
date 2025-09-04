using ERP.Plugin.Products.Events;
using Xunit;

namespace ERP.Plugin.Products.Tests;

public class ProductsEventTests
{
    [Fact]
    public void ProductCatalogCreatedEvent_ShouldHaveCorrectProperties()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var productName = "Laptop Computer";
        var productCode = "LAP-001";
        var brand = "TechBrand";
        var category = "Electronics";
        var price = 999.99m;
        var description = "High-performance laptop computer";

        // Act
        var productEvent = new ProductCatalogCreatedEvent(productId, productName, productCode, brand, category, price, description);

        // Assert
        Assert.Equal(productId, productEvent.ProductId);
        Assert.Equal(productName, productEvent.ProductName);
        Assert.Equal(productCode, productEvent.ProductCode);
        Assert.Equal(brand, productEvent.Brand);
        Assert.Equal(category, productEvent.Category);
        Assert.Equal(price, productEvent.Price);
        Assert.Equal(description, productEvent.Description);
        Assert.Equal("ProductCatalogCreatedEvent", productEvent.EventType);
        Assert.NotEqual(Guid.Empty, productEvent.EventId);
        Assert.True(productEvent.OccurredAt <= DateTime.UtcNow);
    }

    [Fact]
    public void ProductUpdatedEvent_ShouldHaveCorrectProperties()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var productCode = "LAP-001";
        var updatedField = "Price";
        var oldValue = 999.99m;
        var newValue = 899.99m;
        var updatedBy = "admin@example.com";

        // Act
        var updateEvent = new ProductUpdatedEvent(productId, productCode, updatedField, oldValue, newValue, updatedBy);

        // Assert
        Assert.Equal(productId, updateEvent.ProductId);
        Assert.Equal(productCode, updateEvent.ProductCode);
        Assert.Equal(updatedField, updateEvent.UpdatedField);
        Assert.Equal(oldValue, updateEvent.OldValue);
        Assert.Equal(newValue, updateEvent.NewValue);
        Assert.Equal(updatedBy, updateEvent.UpdatedBy);
        Assert.Equal("ProductUpdatedEvent", updateEvent.EventType);
        Assert.NotEqual(Guid.Empty, updateEvent.EventId);
        Assert.True(updateEvent.OccurredAt <= DateTime.UtcNow);
        Assert.True(updateEvent.UpdatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void ProductDiscontinuedEvent_ShouldHaveCorrectProperties()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var productName = "Old Laptop Model";
        var productCode = "LAP-OLD";
        var reason = "End of life product";
        var discontinuedBy = "manager@example.com";

        // Act
        var discontinuedEvent = new ProductDiscontinuedEvent(productId, productName, productCode, reason, discontinuedBy);

        // Assert
        Assert.Equal(productId, discontinuedEvent.ProductId);
        Assert.Equal(productName, discontinuedEvent.ProductName);
        Assert.Equal(productCode, discontinuedEvent.ProductCode);
        Assert.Equal(reason, discontinuedEvent.Reason);
        Assert.Equal(discontinuedBy, discontinuedEvent.DiscontinuedBy);
        Assert.Equal("ProductDiscontinuedEvent", discontinuedEvent.EventType);
        Assert.NotEqual(Guid.Empty, discontinuedEvent.EventId);
        Assert.True(discontinuedEvent.OccurredAt <= DateTime.UtcNow);
        Assert.True(discontinuedEvent.DiscontinuedDate <= DateTime.UtcNow);
    }

    [Fact]
    public void DomainEvent_ShouldHaveUniqueEventIds()
    {
        // Arrange & Act
        var event1 = new ProductCatalogCreatedEvent(Guid.NewGuid(), "Product 1", "PRD-001", "Brand1", "Category1", 100m, "Description 1");
        var event2 = new ProductCatalogCreatedEvent(Guid.NewGuid(), "Product 2", "PRD-002", "Brand2", "Category2", 200m, "Description 2");

        // Assert
        Assert.NotEqual(event1.EventId, event2.EventId);
    }
}