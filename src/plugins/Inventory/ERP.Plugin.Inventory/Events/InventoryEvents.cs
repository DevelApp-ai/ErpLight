using ERP.SharedKernel.Events;

namespace ERP.Plugin.Inventory.Events;

/// <summary>
/// Event published when a product is added to inventory.
/// Other modules can subscribe to this event to react accordingly.
/// </summary>
public class ProductAddedEvent : DomainEvent
{
    public Guid ProductId { get; }
    public string ProductName { get; }
    public string ProductCode { get; }
    public int Quantity { get; }
    public decimal UnitPrice { get; }
    public string Category { get; }

    public ProductAddedEvent(Guid productId, string productName, string productCode, int quantity, decimal unitPrice, string category)
    {
        ProductId = productId;
        ProductName = productName;
        ProductCode = productCode;
        Quantity = quantity;
        UnitPrice = unitPrice;
        Category = category;
    }
}

/// <summary>
/// Event published when inventory stock is updated.
/// </summary>
public class StockUpdatedEvent : DomainEvent
{
    public Guid ProductId { get; }
    public string ProductCode { get; }
    public int PreviousQuantity { get; }
    public int NewQuantity { get; }
    public string UpdateReason { get; }
    public DateTime UpdatedAt { get; }

    public StockUpdatedEvent(Guid productId, string productCode, int previousQuantity, int newQuantity, string updateReason)
    {
        ProductId = productId;
        ProductCode = productCode;
        PreviousQuantity = previousQuantity;
        NewQuantity = newQuantity;
        UpdateReason = updateReason;
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Event published when a product's stock level falls below a threshold.
/// </summary>
public class LowStockAlertEvent : DomainEvent
{
    public Guid ProductId { get; }
    public string ProductName { get; }
    public string ProductCode { get; }
    public int CurrentQuantity { get; }
    public int ThresholdLevel { get; }

    public LowStockAlertEvent(Guid productId, string productName, string productCode, int currentQuantity, int thresholdLevel)
    {
        ProductId = productId;
        ProductName = productName;
        ProductCode = productCode;
        CurrentQuantity = currentQuantity;
        ThresholdLevel = thresholdLevel;
    }
}