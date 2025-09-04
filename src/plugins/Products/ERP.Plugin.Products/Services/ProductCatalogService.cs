using ERP.Plugin.Products.Events;

namespace ERP.Plugin.Products.Services;

/// <summary>
/// Interface for product catalog management services.
/// </summary>
public interface IProductCatalogService
{
    /// <summary>
    /// Creates a new product in the catalog.
    /// </summary>
    Task<Guid> CreateProductAsync(string productName, string productCode, string brand, string category, decimal price, string description);

    /// <summary>
    /// Updates an existing product in the catalog.
    /// </summary>
    Task UpdateProductAsync(Guid productId, string field, object? oldValue, object? newValue, string updatedBy);

    /// <summary>
    /// Discontinues a product from the catalog.
    /// </summary>
    Task DiscontinueProductAsync(Guid productId, string reason, string discontinuedBy);

    /// <summary>
    /// Gets product information by ID.
    /// </summary>
    Task<ProductInfo?> GetProductAsync(Guid productId);

    /// <summary>
    /// Gets all products in a specific category.
    /// </summary>
    Task<IEnumerable<ProductInfo>> GetProductsByCategoryAsync(string category);
}

/// <summary>
/// Product information model.
/// </summary>
public class ProductInfo
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsDiscontinued { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DiscontinuedAt { get; set; }
}

/// <summary>
/// Implementation of product catalog management services.
/// </summary>
public class ProductCatalogService : IProductCatalogService
{
    private readonly Dictionary<Guid, ProductInfo> _products = new();

    public async Task<Guid> CreateProductAsync(string productName, string productCode, string brand, string category, decimal price, string description)
    {
        var productId = Guid.NewGuid();
        var product = new ProductInfo
        {
            ProductId = productId,
            ProductName = productName,
            ProductCode = productCode,
            Brand = brand,
            Category = category,
            Price = price,
            Description = description,
            IsDiscontinued = false,
            CreatedAt = DateTime.UtcNow
        };

        _products[productId] = product;

        // Publish domain event
        var evt = new ProductCatalogCreatedEvent(productId, productName, productCode, brand, category, price, description);
        // Note: In a real implementation, you would publish this event through an event bus
        
        await Task.CompletedTask;
        return productId;
    }

    public async Task UpdateProductAsync(Guid productId, string field, object? oldValue, object? newValue, string updatedBy)
    {
        if (_products.TryGetValue(productId, out var product))
        {
            // Update the field (simplified implementation)
            switch (field.ToLower())
            {
                case "price":
                    if (newValue is decimal newPrice)
                        product.Price = newPrice;
                    break;
                case "description":
                    if (newValue is string newDesc)
                        product.Description = newDesc;
                    break;
                // Add more field updates as needed
            }

            // Publish domain event
            var evt = new ProductUpdatedEvent(productId, product.ProductCode, field, oldValue, newValue, updatedBy);
            // Note: In a real implementation, you would publish this event through an event bus
        }

        await Task.CompletedTask;
    }

    public async Task DiscontinueProductAsync(Guid productId, string reason, string discontinuedBy)
    {
        if (_products.TryGetValue(productId, out var product))
        {
            product.IsDiscontinued = true;
            product.DiscontinuedAt = DateTime.UtcNow;

            // Publish domain event
            var evt = new ProductDiscontinuedEvent(productId, product.ProductName, product.ProductCode, reason, discontinuedBy);
            // Note: In a real implementation, you would publish this event through an event bus
        }

        await Task.CompletedTask;
    }

    public async Task<ProductInfo?> GetProductAsync(Guid productId)
    {
        _products.TryGetValue(productId, out var product);
        await Task.CompletedTask;
        return product;
    }

    public async Task<IEnumerable<ProductInfo>> GetProductsByCategoryAsync(string category)
    {
        var products = _products.Values.Where(p => p.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
        await Task.CompletedTask;
        return products;
    }
}