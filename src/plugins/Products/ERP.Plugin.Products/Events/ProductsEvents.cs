using ERP.SharedKernel.Events;

namespace ERP.Plugin.Products.Events;

/// <summary>
/// Event published when a product catalog entry is created.
/// Other modules can subscribe to this event to react accordingly.
/// </summary>
public class ProductCatalogCreatedEvent : DomainEvent
{
    public Guid ProductId { get; }
    public string ProductName { get; }
    public string ProductCode { get; }
    public string Brand { get; }
    public string Category { get; }
    public decimal Price { get; }
    public string Description { get; }

    public ProductCatalogCreatedEvent(Guid productId, string productName, string productCode, string brand, string category, decimal price, string description)
    {
        ProductId = productId;
        ProductName = productName;
        ProductCode = productCode;
        Brand = brand;
        Category = category;
        Price = price;
        Description = description;
    }
}

/// <summary>
/// Event published when a product in the catalog is updated.
/// </summary>
public class ProductUpdatedEvent : DomainEvent
{
    public Guid ProductId { get; }
    public string ProductCode { get; }
    public string UpdatedField { get; }
    public object? OldValue { get; }
    public object? NewValue { get; }
    public string UpdatedBy { get; }
    public DateTime UpdatedAt { get; }

    public ProductUpdatedEvent(Guid productId, string productCode, string updatedField, object? oldValue, object? newValue, string updatedBy)
    {
        ProductId = productId;
        ProductCode = productCode;
        UpdatedField = updatedField;
        OldValue = oldValue;
        NewValue = newValue;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Event published when a product is discontinued or removed from catalog.
/// </summary>
public class ProductDiscontinuedEvent : DomainEvent
{
    public Guid ProductId { get; }
    public string ProductName { get; }
    public string ProductCode { get; }
    public string Reason { get; }
    public DateTime DiscontinuedDate { get; }
    public string DiscontinuedBy { get; }

    public ProductDiscontinuedEvent(Guid productId, string productName, string productCode, string reason, string discontinuedBy)
    {
        ProductId = productId;
        ProductName = productName;
        ProductCode = productCode;
        Reason = reason;
        DiscontinuedBy = discontinuedBy;
        DiscontinuedDate = DateTime.UtcNow;
    }
}