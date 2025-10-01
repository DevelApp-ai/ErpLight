using ERP.Plugin.Finance.Events;
using ERP.SharedKernel.Events;

namespace ERP.Plugin.Finance.Services;

/// <summary>
/// Service interface for managing invoices
/// </summary>
public interface IInvoiceService
{
    Task<InvoiceDto> CreateInvoiceAsync(CreateInvoiceRequest request);
    Task<InvoiceDto?> GetInvoiceAsync(Guid invoiceId);
    Task<IEnumerable<InvoiceDto>> GetInvoicesAsync();
    Task<bool> MarkInvoiceAsPaidAsync(Guid invoiceId, decimal amount, string paymentMethod);
}

/// <summary>
/// Implementation of invoice service
/// </summary>
public class InvoiceService : IInvoiceService
{
    private readonly IEventPublisher _eventPublisher;
    private static readonly List<InvoiceDto> _invoices = new();

    public InvoiceService(IEventPublisher eventPublisher)
    {
        _eventPublisher = eventPublisher;
    }

    public async Task<InvoiceDto> CreateInvoiceAsync(CreateInvoiceRequest request)
    {
        var invoice = new InvoiceDto
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = GenerateInvoiceNumber(),
            CustomerId = request.CustomerId,
            CustomerName = request.CustomerName,
            Amount = request.Amount,
            DueDate = request.DueDate,
            Status = InvoiceStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _invoices.Add(invoice);

        // Publish domain event
        var invoiceCreatedEvent = new InvoiceCreatedEvent(
            invoice.Id,
            invoice.InvoiceNumber,
            invoice.Amount,
            invoice.CustomerId,
            invoice.DueDate
        );

        await _eventPublisher.PublishAsync(invoiceCreatedEvent);

        return invoice;
    }

    public Task<InvoiceDto?> GetInvoiceAsync(Guid invoiceId)
    {
        var invoice = _invoices.FirstOrDefault(i => i.Id == invoiceId);
        return Task.FromResult(invoice);
    }

    public Task<IEnumerable<InvoiceDto>> GetInvoicesAsync()
    {
        return Task.FromResult(_invoices.AsEnumerable());
    }

    public async Task<bool> MarkInvoiceAsPaidAsync(Guid invoiceId, decimal amount, string paymentMethod)
    {
        var invoice = _invoices.FirstOrDefault(i => i.Id == invoiceId);
        if (invoice == null) return false;

        invoice.Status = InvoiceStatus.Paid;
        invoice.PaidAt = DateTime.UtcNow;

        // Publish payment received event
        var paymentEvent = new PaymentReceivedEvent(
            Guid.NewGuid(),
            invoiceId,
            amount,
            DateTime.UtcNow,
            paymentMethod
        );

        await _eventPublisher.PublishAsync(paymentEvent);

        return true;
    }

    private static string GenerateInvoiceNumber()
    {
        return $"INV-{DateTime.Now:yyyyMMdd}-{_invoices.Count + 1:D3}";
    }
}

/// <summary>
/// Data transfer object for invoice
/// </summary>
public class InvoiceDto
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime DueDate { get; set; }
    public InvoiceStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PaidAt { get; set; }
}

/// <summary>
/// Request object for creating invoices
/// </summary>
public class CreateInvoiceRequest
{
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime DueDate { get; set; }
}

/// <summary>
/// Invoice status enumeration
/// </summary>
public enum InvoiceStatus
{
    Pending,
    Paid,
    Overdue,
    Cancelled
}