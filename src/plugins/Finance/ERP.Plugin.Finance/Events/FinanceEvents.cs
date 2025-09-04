using ERP.SharedKernel.Events;

namespace ERP.Plugin.Finance.Events;

/// <summary>
/// Event published when an invoice is created in the Finance module.
/// Other modules can subscribe to this event to react accordingly.
/// </summary>
public class InvoiceCreatedEvent : DomainEvent
{
    public Guid InvoiceId { get; }
    public string InvoiceNumber { get; }
    public decimal Amount { get; }
    public string CustomerId { get; }
    public DateTime DueDate { get; }

    public InvoiceCreatedEvent(Guid invoiceId, string invoiceNumber, decimal amount, string customerId, DateTime dueDate)
    {
        InvoiceId = invoiceId;
        InvoiceNumber = invoiceNumber;
        Amount = amount;
        CustomerId = customerId;
        DueDate = dueDate;
    }
}

/// <summary>
/// Event published when a payment is received in the Finance module.
/// </summary>
public class PaymentReceivedEvent : DomainEvent
{
    public Guid PaymentId { get; }
    public Guid InvoiceId { get; }
    public decimal Amount { get; }
    public DateTime PaymentDate { get; }
    public string PaymentMethod { get; }

    public PaymentReceivedEvent(Guid paymentId, Guid invoiceId, decimal amount, DateTime paymentDate, string paymentMethod)
    {
        PaymentId = paymentId;
        InvoiceId = invoiceId;
        Amount = amount;
        PaymentDate = paymentDate;
        PaymentMethod = paymentMethod;
    }
}