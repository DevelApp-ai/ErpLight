using ERP.Plugin.Orders.Events;
using ERP.Plugin.Orders.Services;
using ERP.SharedKernel.Events;
using Moq;
using Xunit;

namespace ERP.Plugin.Orders.Tests;

/// <summary>
/// Tests for OrderService event publishing functionality.
/// </summary>
public class OrderServiceEventTests
{
    [Fact]
    public async Task CreateOrderAsync_ShouldPublishOrderCreatedEvent()
    {
        // Arrange
        var mockEventPublisher = new Mock<IEventPublisher>();
        var service = new OrderService(mockEventPublisher.Object);

        var items = new List<OrderItem>
        {
            new OrderItem { ProductId = Guid.NewGuid(), ProductCode = "PROD-001", ProductName = "Product 1", Quantity = 2, UnitPrice = 10.00m }
        };

        OrderCreatedEvent? capturedEvent = null;
        mockEventPublisher.Setup(x => x.PublishAsync(It.IsAny<DomainEvent>()))
            .Callback<DomainEvent>(evt => capturedEvent = evt as OrderCreatedEvent);

        // Act
        var orderId = await service.CreateOrderAsync("Sales", "CUST-001", 20.00m, items);

        // Assert
        mockEventPublisher.Verify(x => x.PublishAsync(It.IsAny<OrderCreatedEvent>()), Times.Once);
        Assert.NotNull(capturedEvent);
        Assert.Equal(orderId, capturedEvent.OrderId);
        Assert.Equal("Sales", capturedEvent.OrderType);
        Assert.Equal("CUST-001", capturedEvent.CustomerId);
        Assert.Equal(20.00m, capturedEvent.TotalAmount);
        Assert.Equal("Created", capturedEvent.Status);
    }

    [Fact]
    public async Task UpdateOrderStatusAsync_ShouldPublishOrderUpdatedEvent()
    {
        // Arrange
        var mockEventPublisher = new Mock<IEventPublisher>();
        var service = new OrderService(mockEventPublisher.Object);

        // Create an order first
        var items = new List<OrderItem>
        {
            new OrderItem { ProductId = Guid.NewGuid(), ProductCode = "PROD-001", ProductName = "Product 1", Quantity = 1, UnitPrice = 10.00m }
        };
        var orderId = await service.CreateOrderAsync("Sales", "CUST-001", 10.00m, items);

        OrderUpdatedEvent? capturedEvent = null;
        mockEventPublisher.Setup(x => x.PublishAsync(It.IsAny<DomainEvent>()))
            .Callback<DomainEvent>(evt => capturedEvent = evt as OrderUpdatedEvent);

        // Act
        await service.UpdateOrderStatusAsync(orderId, "Processing", "admin@example.com", "Order approved");

        // Assert
        mockEventPublisher.Verify(x => x.PublishAsync(It.IsAny<OrderUpdatedEvent>()), Times.Once);
        Assert.NotNull(capturedEvent);
        Assert.Equal(orderId, capturedEvent.OrderId);
        Assert.Equal("Created", capturedEvent.PreviousStatus);
        Assert.Equal("Processing", capturedEvent.NewStatus);
        Assert.Equal("admin@example.com", capturedEvent.UpdatedBy);
        Assert.Equal("Order approved", capturedEvent.UpdateReason);
    }

    [Fact]
    public async Task FulfillOrderAsync_ShouldPublishOrderFulfilledEvent()
    {
        // Arrange
        var mockEventPublisher = new Mock<IEventPublisher>();
        var service = new OrderService(mockEventPublisher.Object);

        // Create an order first
        var items = new List<OrderItem>
        {
            new OrderItem { ProductId = Guid.NewGuid(), ProductCode = "PROD-001", ProductName = "Product 1", Quantity = 1, UnitPrice = 10.00m }
        };
        var orderId = await service.CreateOrderAsync("Sales", "CUST-001", 10.00m, items);

        OrderFulfilledEvent? capturedEvent = null;
        mockEventPublisher.Setup(x => x.PublishAsync(It.IsAny<DomainEvent>()))
            .Callback<DomainEvent>(evt => capturedEvent = evt as OrderFulfilledEvent);

        // Act
        await service.FulfillOrderAsync(orderId, "warehouse@example.com", "TRACK123", "FedEx");

        // Assert
        mockEventPublisher.Verify(x => x.PublishAsync(It.IsAny<OrderFulfilledEvent>()), Times.Once);
        Assert.NotNull(capturedEvent);
        Assert.Equal(orderId, capturedEvent.OrderId);
        Assert.Equal("warehouse@example.com", capturedEvent.FulfilledBy);
        Assert.Equal("TRACK123", capturedEvent.TrackingNumber);
        Assert.Equal("FedEx", capturedEvent.ShippingMethod);
    }

    [Fact]
    public async Task CancelOrderAsync_ShouldPublishOrderCancelledEvent()
    {
        // Arrange
        var mockEventPublisher = new Mock<IEventPublisher>();
        var service = new OrderService(mockEventPublisher.Object);

        // Create an order first
        var items = new List<OrderItem>
        {
            new OrderItem { ProductId = Guid.NewGuid(), ProductCode = "PROD-001", ProductName = "Product 1", Quantity = 1, UnitPrice = 10.00m }
        };
        var orderId = await service.CreateOrderAsync("Sales", "CUST-001", 10.00m, items);

        OrderCancelledEvent? capturedEvent = null;
        mockEventPublisher.Setup(x => x.PublishAsync(It.IsAny<DomainEvent>()))
            .Callback<DomainEvent>(evt => capturedEvent = evt as OrderCancelledEvent);

        // Act
        await service.CancelOrderAsync(orderId, "Customer requested cancellation", "support@example.com", true);

        // Assert
        mockEventPublisher.Verify(x => x.PublishAsync(It.IsAny<OrderCancelledEvent>()), Times.Once);
        Assert.NotNull(capturedEvent);
        Assert.Equal(orderId, capturedEvent.OrderId);
        Assert.Equal("Customer requested cancellation", capturedEvent.CancellationReason);
        Assert.Equal("support@example.com", capturedEvent.CancelledBy);
        Assert.True(capturedEvent.RefundIssued);
    }

    [Fact]
    public async Task CreateOrderAsync_ShouldGenerateOrderNumber()
    {
        // Arrange
        var mockEventPublisher = new Mock<IEventPublisher>();
        var service = new OrderService(mockEventPublisher.Object);

        var items = new List<OrderItem>
        {
            new OrderItem { ProductId = Guid.NewGuid(), ProductCode = "PROD-001", ProductName = "Product 1", Quantity = 1, UnitPrice = 10.00m }
        };

        // Act
        var orderId = await service.CreateOrderAsync("Sales", "CUST-001", 10.00m, items);
        var order = await service.GetOrderAsync(orderId);

        // Assert
        Assert.NotNull(order);
        Assert.StartsWith("ORD-", order.OrderNumber);
    }

    [Fact]
    public async Task UpdateOrderStatusAsync_ShouldNotPublishEvent_WhenOrderNotFound()
    {
        // Arrange
        var mockEventPublisher = new Mock<IEventPublisher>();
        var service = new OrderService(mockEventPublisher.Object);

        var nonExistentOrderId = Guid.NewGuid();

        // Act
        await service.UpdateOrderStatusAsync(nonExistentOrderId, "Processing", "admin@example.com");

        // Assert
        mockEventPublisher.Verify(x => x.PublishAsync(It.IsAny<OrderUpdatedEvent>()), Times.Never);
    }

    [Fact]
    public async Task GetOrdersByCustomerAsync_ShouldReturnOnlyCustomerOrders()
    {
        // Arrange
        var mockEventPublisher = new Mock<IEventPublisher>();
        var service = new OrderService(mockEventPublisher.Object);

        var items = new List<OrderItem>
        {
            new OrderItem { ProductId = Guid.NewGuid(), ProductCode = "PROD-001", ProductName = "Product 1", Quantity = 1, UnitPrice = 10.00m }
        };

        // Create orders for different customers
        await service.CreateOrderAsync("Sales", "CUST-001", 10.00m, items);
        await service.CreateOrderAsync("Sales", "CUST-002", 20.00m, items);
        await service.CreateOrderAsync("Sales", "CUST-001", 15.00m, items);

        // Act
        var orders = (await service.GetOrdersByCustomerAsync("CUST-001")).ToList();

        // Assert
        Assert.Equal(2, orders.Count);
        Assert.All(orders, o => Assert.Equal("CUST-001", o.CustomerId));
    }

    [Fact]
    public async Task GetOrdersByStatusAsync_ShouldReturnOnlyMatchingOrders()
    {
        // Arrange
        var mockEventPublisher = new Mock<IEventPublisher>();
        var service = new OrderService(mockEventPublisher.Object);

        var items = new List<OrderItem>
        {
            new OrderItem { ProductId = Guid.NewGuid(), ProductCode = "PROD-001", ProductName = "Product 1", Quantity = 1, UnitPrice = 10.00m }
        };

        // Create orders with different statuses
        var order1 = await service.CreateOrderAsync("Sales", "CUST-001", 10.00m, items);
        var order2 = await service.CreateOrderAsync("Sales", "CUST-002", 20.00m, items);
        await service.UpdateOrderStatusAsync(order2, "Processing", "admin@example.com");

        // Act
        var createdOrders = (await service.GetOrdersByStatusAsync("Created")).ToList();

        // Assert
        Assert.Single(createdOrders);
        Assert.Equal(order1, createdOrders[0].OrderId);
    }

    [Fact]
    public async Task OrderItem_ShouldCalculateTotalPrice()
    {
        // Arrange
        var item = new OrderItem
        {
            ProductId = Guid.NewGuid(),
            ProductCode = "PROD-001",
            ProductName = "Product 1",
            Quantity = 5,
            UnitPrice = 10.00m
        };

        // Act & Assert
        Assert.Equal(50.00m, item.TotalPrice);
    }
}
