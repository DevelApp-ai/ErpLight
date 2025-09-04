using ERP.SharedKernel.Events;

namespace ERP.Plugin.Orders.Events;

/// <summary>
/// Event published when a new order is created.
/// Other modules can subscribe to this event to react accordingly.
/// </summary>
public class OrderCreatedEvent : DomainEvent
{
    public Guid OrderId { get; }
    public string OrderNumber { get; }
    public string OrderType { get; } // "Sales" or "Purchase"
    public string CustomerId { get; }
    public decimal TotalAmount { get; }
    public DateTime OrderDate { get; }
    public string Status { get; }

    public OrderCreatedEvent(Guid orderId, string orderNumber, string orderType, string customerId, decimal totalAmount, DateTime orderDate, string status)
    {
        OrderId = orderId;
        OrderNumber = orderNumber;
        OrderType = orderType;
        CustomerId = customerId;
        TotalAmount = totalAmount;
        OrderDate = orderDate;
        Status = status;
    }
}

/// <summary>
/// Event published when an order status is updated.
/// </summary>
public class OrderUpdatedEvent : DomainEvent
{
    public Guid OrderId { get; }
    public string OrderNumber { get; }
    public string PreviousStatus { get; }
    public string NewStatus { get; }
    public string UpdatedBy { get; }
    public DateTime UpdatedAt { get; }
    public string? UpdateReason { get; }

    public OrderUpdatedEvent(Guid orderId, string orderNumber, string previousStatus, string newStatus, string updatedBy, string? updateReason = null)
    {
        OrderId = orderId;
        OrderNumber = orderNumber;
        PreviousStatus = previousStatus;
        NewStatus = newStatus;
        UpdatedBy = updatedBy;
        UpdateReason = updateReason;
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Event published when an order is fulfilled/completed.
/// </summary>
public class OrderFulfilledEvent : DomainEvent
{
    public Guid OrderId { get; }
    public string OrderNumber { get; }
    public DateTime FulfilledDate { get; }
    public string FulfilledBy { get; }
    public string TrackingNumber { get; }
    public string ShippingMethod { get; }

    public OrderFulfilledEvent(Guid orderId, string orderNumber, string fulfilledBy, string trackingNumber, string shippingMethod)
    {
        OrderId = orderId;
        OrderNumber = orderNumber;
        FulfilledBy = fulfilledBy;
        TrackingNumber = trackingNumber;
        ShippingMethod = shippingMethod;
        FulfilledDate = DateTime.UtcNow;
    }
}

/// <summary>
/// Event published when an order is cancelled.
/// </summary>
public class OrderCancelledEvent : DomainEvent
{
    public Guid OrderId { get; }
    public string OrderNumber { get; }
    public string CancellationReason { get; }
    public string CancelledBy { get; }
    public DateTime CancelledDate { get; }
    public bool RefundIssued { get; }

    public OrderCancelledEvent(Guid orderId, string orderNumber, string cancellationReason, string cancelledBy, bool refundIssued)
    {
        OrderId = orderId;
        OrderNumber = orderNumber;
        CancellationReason = cancellationReason;
        CancelledBy = cancelledBy;
        RefundIssued = refundIssued;
        CancelledDate = DateTime.UtcNow;
    }
}