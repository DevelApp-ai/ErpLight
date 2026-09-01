using ERP.Plugin.Orders.Events;
using ERP.SharedKernel.Events;

namespace ERP.Plugin.Orders.Services;

/// <summary>
/// Interface for order management services.
/// </summary>
public interface IOrderService
{
    /// <summary>
    /// Creates a new order.
    /// </summary>
    Task<Guid> CreateOrderAsync(string orderType, string customerId, decimal totalAmount, List<OrderItem> items);

    /// <summary>
    /// Updates order status.
    /// </summary>
    Task UpdateOrderStatusAsync(Guid orderId, string newStatus, string updatedBy, string? reason = null);

    /// <summary>
    /// Fulfills an order with shipping information.
    /// </summary>
    Task FulfillOrderAsync(Guid orderId, string fulfilledBy, string trackingNumber, string shippingMethod);

    /// <summary>
    /// Cancels an order.
    /// </summary>
    Task CancelOrderAsync(Guid orderId, string cancellationReason, string cancelledBy, bool issueRefund);

    /// <summary>
    /// Gets order information by ID.
    /// </summary>
    Task<OrderInfo?> GetOrderAsync(Guid orderId);

    /// <summary>
    /// Gets orders by customer ID.
    /// </summary>
    Task<IEnumerable<OrderInfo>> GetOrdersByCustomerAsync(string customerId);

    /// <summary>
    /// Gets orders by status.
    /// </summary>
    Task<IEnumerable<OrderInfo>> GetOrdersByStatusAsync(string status);
}

/// <summary>
/// Order item model.
/// </summary>
public class OrderItem
{
    public Guid ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice => Quantity * UnitPrice;
}

/// <summary>
/// Order information model.
/// </summary>
public class OrderInfo
{
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string OrderType { get; set; } = string.Empty; // "Sales" or "Purchase"
    public string CustomerId { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<OrderItem> Items { get; set; } = new();
    public DateTime? FulfilledDate { get; set; }
    public string? TrackingNumber { get; set; }
    public string? ShippingMethod { get; set; }
    public DateTime? CancelledDate { get; set; }
    public string? CancellationReason { get; set; }
}

/// <summary>
/// Implementation of order management services.
/// </summary>
public class OrderService : IOrderService
{
    private readonly IEventPublisher _eventPublisher;
    private readonly Dictionary<Guid, OrderInfo> _orders = new();
    private int _orderCounter = 1;

    public OrderService(IEventPublisher eventPublisher)
    {
        _eventPublisher = eventPublisher;
    }

    public async Task<Guid> CreateOrderAsync(string orderType, string customerId, decimal totalAmount, List<OrderItem> items)
    {
        var orderId = Guid.NewGuid();
        var orderNumber = $"ORD-{_orderCounter:D6}";
        _orderCounter++;

        var order = new OrderInfo
        {
            OrderId = orderId,
            OrderNumber = orderNumber,
            OrderType = orderType,
            CustomerId = customerId,
            TotalAmount = totalAmount,
            OrderDate = DateTime.UtcNow,
            Status = "Created",
            Items = items
        };

        _orders[orderId] = order;

        // Publish domain event
        var orderCreatedEvent = new OrderCreatedEvent(orderId, orderNumber, orderType, customerId, totalAmount, order.OrderDate, order.Status);
        await _eventPublisher.PublishAsync(orderCreatedEvent);

        return orderId;
        return orderId;
    }

    public async Task UpdateOrderStatusAsync(Guid orderId, string newStatus, string updatedBy, string? reason = null)
    {
        if (_orders.TryGetValue(orderId, out var order))
        {
            var previousStatus = order.Status;
            order.Status = newStatus;

            // Publish domain event
            var orderUpdatedEvent = new OrderUpdatedEvent(orderId, order.OrderNumber, previousStatus, newStatus, updatedBy, reason);
            await _eventPublisher.PublishAsync(orderUpdatedEvent);
        }
    }

    public async Task FulfillOrderAsync(Guid orderId, string fulfilledBy, string trackingNumber, string shippingMethod)
    {
        if (_orders.TryGetValue(orderId, out var order))
        {
            order.Status = "Fulfilled";
            order.FulfilledDate = DateTime.UtcNow;
            order.TrackingNumber = trackingNumber;
            order.ShippingMethod = shippingMethod;

            // Publish domain event
            var orderFulfilledEvent = new OrderFulfilledEvent(orderId, order.OrderNumber, fulfilledBy, trackingNumber, shippingMethod);
            await _eventPublisher.PublishAsync(orderFulfilledEvent);
        }
    }

    public async Task CancelOrderAsync(Guid orderId, string cancellationReason, string cancelledBy, bool issueRefund)
    {
        if (_orders.TryGetValue(orderId, out var order))
        {
            order.Status = "Cancelled";
            order.CancelledDate = DateTime.UtcNow;
            order.CancellationReason = cancellationReason;

            // Publish domain event
            var orderCancelledEvent = new OrderCancelledEvent(orderId, order.OrderNumber, cancellationReason, cancelledBy, issueRefund);
            await _eventPublisher.PublishAsync(orderCancelledEvent);
        }
    }

    public async Task<OrderInfo?> GetOrderAsync(Guid orderId)
    {
        _orders.TryGetValue(orderId, out var order);
        await Task.CompletedTask;
        return order;
    }

    public async Task<IEnumerable<OrderInfo>> GetOrdersByCustomerAsync(string customerId)
    {
        var orders = _orders.Values.Where(o => o.CustomerId.Equals(customerId, StringComparison.OrdinalIgnoreCase));
        await Task.CompletedTask;
        return orders;
    }

    public async Task<IEnumerable<OrderInfo>> GetOrdersByStatusAsync(string status)
    {
        var orders = _orders.Values.Where(o => o.Status.Equals(status, StringComparison.OrdinalIgnoreCase));
        await Task.CompletedTask;
        return orders;
    }
}