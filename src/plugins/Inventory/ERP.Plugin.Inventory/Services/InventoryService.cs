using ERP.Plugin.Inventory.Events;
using ERP.SharedKernel.Events;

namespace ERP.Plugin.Inventory.Services;

/// <summary>
/// Service interface for managing inventory
/// </summary>
public interface IInventoryService
{
    Task<ProductDto> AddProductAsync(AddProductRequest request);
    Task<ProductDto?> GetProductAsync(Guid productId);
    Task<IEnumerable<ProductDto>> GetProductsAsync();
    Task<bool> UpdateStockAsync(Guid productId, int newQuantity, string reason);
    Task<IEnumerable<ProductDto>> GetLowStockProductsAsync();
}

/// <summary>
/// Implementation of inventory service
/// </summary>
public class InventoryService : IInventoryService
{
    private readonly IEventPublisher _eventPublisher;
    private static readonly List<ProductDto> _products = new();

    public InventoryService(IEventPublisher eventPublisher)
    {
        _eventPublisher = eventPublisher;
    }

    public async Task<ProductDto> AddProductAsync(AddProductRequest request)
    {
        var product = new ProductDto
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Code = GenerateProductCode(request.Category),
            Category = request.Category,
            Quantity = request.InitialQuantity,
            UnitPrice = request.UnitPrice,
            LowStockThreshold = request.LowStockThreshold,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _products.Add(product);

        // Publish domain event
        var productAddedEvent = new ProductAddedEvent(
            product.Id,
            product.Name,
            product.Code,
            product.Quantity,
            product.UnitPrice,
            product.Category
        );

        await _eventPublisher.PublishAsync(productAddedEvent);

        return product;
    }

    public Task<ProductDto?> GetProductAsync(Guid productId)
    {
        var product = _products.FirstOrDefault(p => p.Id == productId);
        return Task.FromResult(product);
    }

    public Task<IEnumerable<ProductDto>> GetProductsAsync()
    {
        return Task.FromResult(_products.AsEnumerable());
    }

    public async Task<bool> UpdateStockAsync(Guid productId, int newQuantity, string reason)
    {
        var product = _products.FirstOrDefault(p => p.Id == productId);
        if (product == null) return false;

        var previousQuantity = product.Quantity;
        product.Quantity = newQuantity;
        product.UpdatedAt = DateTime.UtcNow;

        // Publish stock updated event
        var stockUpdatedEvent = new StockUpdatedEvent(
            productId,
            product.Code,
            previousQuantity,
            newQuantity,
            reason
        );

        await _eventPublisher.PublishAsync(stockUpdatedEvent);

        // Check if low stock alert is needed
        if (newQuantity <= product.LowStockThreshold)
        {
            var lowStockEvent = new LowStockAlertEvent(
                productId,
                product.Name,
                product.Code,
                newQuantity,
                product.LowStockThreshold
            );

            await _eventPublisher.PublishAsync(lowStockEvent);
        }

        return true;
    }

    public Task<IEnumerable<ProductDto>> GetLowStockProductsAsync()
    {
        var lowStockProducts = _products.Where(p => p.Quantity <= p.LowStockThreshold);
        return Task.FromResult(lowStockProducts);
    }

    private static string GenerateProductCode(string category)
    {
        var categoryCode = category.ToUpper().Substring(0, Math.Min(3, category.Length));
        var count = _products.Count(p => p.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
        return $"{categoryCode}-{DateTime.Now:yyyyMMdd}-{count + 1:D3}";
    }
}

/// <summary>
/// Data transfer object for product
/// </summary>
public class ProductDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public int LowStockThreshold { get; set; } = 10;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Request object for adding products
/// </summary>
public class AddProductRequest
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int InitialQuantity { get; set; }
    public decimal UnitPrice { get; set; }
    public int LowStockThreshold { get; set; } = 10;
}