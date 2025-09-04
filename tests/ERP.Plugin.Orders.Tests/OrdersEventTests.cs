using ERP.Plugin.Orders.Events;
using Xunit;

namespace ERP.Plugin.Orders.Tests;

public class OrdersEventTests
{
    [Fact]
    public void OrderCreatedEvent_ShouldHaveCorrectProperties()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var orderNumber = "ORD-000001";
        var orderType = "Sales";
        var customerId = "CUST-123";
        var totalAmount = 2500.00m;
        var orderDate = DateTime.Now;
        var status = "Created";

        // Act
        var orderEvent = new OrderCreatedEvent(orderId, orderNumber, orderType, customerId, totalAmount, orderDate, status);

        // Assert
        Assert.Equal(orderId, orderEvent.OrderId);
        Assert.Equal(orderNumber, orderEvent.OrderNumber);
        Assert.Equal(orderType, orderEvent.OrderType);
        Assert.Equal(customerId, orderEvent.CustomerId);
        Assert.Equal(totalAmount, orderEvent.TotalAmount);
        Assert.Equal(orderDate, orderEvent.OrderDate);
        Assert.Equal(status, orderEvent.Status);
        Assert.Equal("OrderCreatedEvent", orderEvent.EventType);
        Assert.NotEqual(Guid.Empty, orderEvent.EventId);
        Assert.True(orderEvent.OccurredAt <= DateTime.UtcNow);
    }

    [Fact]
    public void OrderUpdatedEvent_ShouldHaveCorrectProperties()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var orderNumber = "ORD-000001";
        var previousStatus = "Created";
        var newStatus = "Processing";
        var updatedBy = "admin@example.com";
        var updateReason = "Order approved";

        // Act
        var updateEvent = new OrderUpdatedEvent(orderId, orderNumber, previousStatus, newStatus, updatedBy, updateReason);

        // Assert
        Assert.Equal(orderId, updateEvent.OrderId);
        Assert.Equal(orderNumber, updateEvent.OrderNumber);
        Assert.Equal(previousStatus, updateEvent.PreviousStatus);
        Assert.Equal(newStatus, updateEvent.NewStatus);
        Assert.Equal(updatedBy, updateEvent.UpdatedBy);
        Assert.Equal(updateReason, updateEvent.UpdateReason);
        Assert.Equal("OrderUpdatedEvent", updateEvent.EventType);
        Assert.NotEqual(Guid.Empty, updateEvent.EventId);
        Assert.True(updateEvent.OccurredAt <= DateTime.UtcNow);
        Assert.True(updateEvent.UpdatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void OrderFulfilledEvent_ShouldHaveCorrectProperties()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var orderNumber = "ORD-000001";
        var fulfilledBy = "warehouse@example.com";
        var trackingNumber = "TRACK123456789";
        var shippingMethod = "FedEx Ground";

        // Act
        var fulfilledEvent = new OrderFulfilledEvent(orderId, orderNumber, fulfilledBy, trackingNumber, shippingMethod);

        // Assert
        Assert.Equal(orderId, fulfilledEvent.OrderId);
        Assert.Equal(orderNumber, fulfilledEvent.OrderNumber);
        Assert.Equal(fulfilledBy, fulfilledEvent.FulfilledBy);
        Assert.Equal(trackingNumber, fulfilledEvent.TrackingNumber);
        Assert.Equal(shippingMethod, fulfilledEvent.ShippingMethod);
        Assert.Equal("OrderFulfilledEvent", fulfilledEvent.EventType);
        Assert.NotEqual(Guid.Empty, fulfilledEvent.EventId);
        Assert.True(fulfilledEvent.OccurredAt <= DateTime.UtcNow);
        Assert.True(fulfilledEvent.FulfilledDate <= DateTime.UtcNow);
    }

    [Fact]
    public void OrderCancelledEvent_ShouldHaveCorrectProperties()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var orderNumber = "ORD-000001";
        var cancellationReason = "Customer requested cancellation";
        var cancelledBy = "support@example.com";
        var refundIssued = true;

        // Act
        var cancelledEvent = new OrderCancelledEvent(orderId, orderNumber, cancellationReason, cancelledBy, refundIssued);

        // Assert
        Assert.Equal(orderId, cancelledEvent.OrderId);
        Assert.Equal(orderNumber, cancelledEvent.OrderNumber);
        Assert.Equal(cancellationReason, cancelledEvent.CancellationReason);
        Assert.Equal(cancelledBy, cancelledEvent.CancelledBy);
        Assert.Equal(refundIssued, cancelledEvent.RefundIssued);
        Assert.Equal("OrderCancelledEvent", cancelledEvent.EventType);
        Assert.NotEqual(Guid.Empty, cancelledEvent.EventId);
        Assert.True(cancelledEvent.OccurredAt <= DateTime.UtcNow);
        Assert.True(cancelledEvent.CancelledDate <= DateTime.UtcNow);
    }

    [Fact]
    public void DomainEvent_ShouldHaveUniqueEventIds()
    {
        // Arrange & Act
        var event1 = new OrderCreatedEvent(Guid.NewGuid(), "ORD-001", "Sales", "CUST-001", 100m, DateTime.Now, "Created");
        var event2 = new OrderCreatedEvent(Guid.NewGuid(), "ORD-002", "Purchase", "CUST-002", 200m, DateTime.Now, "Created");

        // Assert
        Assert.NotEqual(event1.EventId, event2.EventId);
    }
}