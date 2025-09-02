using ERP.SharedKernel.Events;

namespace ERP.SharedKernel.Tests;

public class DomainEventTests
{
    [Fact]
    public void InvoiceCreatedEvent_ShouldHaveCorrectProperties()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var invoiceNumber = "INV-2024-001";
        var amount = 1500.00m;
        var customerId = "CUST-123";
        var dueDate = DateTime.Today.AddDays(30);

        // Act
        var invoiceEvent = new InvoiceCreatedEvent(invoiceId, invoiceNumber, amount, customerId, dueDate);

        // Assert
        Assert.Equal(invoiceId, invoiceEvent.InvoiceId);
        Assert.Equal(invoiceNumber, invoiceEvent.InvoiceNumber);
        Assert.Equal(amount, invoiceEvent.Amount);
        Assert.Equal(customerId, invoiceEvent.CustomerId);
        Assert.Equal(dueDate, invoiceEvent.DueDate);
        Assert.Equal("InvoiceCreatedEvent", invoiceEvent.EventType);
        Assert.NotEqual(Guid.Empty, invoiceEvent.EventId);
        Assert.True(invoiceEvent.OccurredAt <= DateTime.UtcNow);
    }

    [Fact]
    public void PaymentReceivedEvent_ShouldHaveCorrectProperties()
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();
        var amount = 750.00m;
        var paymentDate = DateTime.Now;
        var paymentMethod = "Credit Card";

        // Act
        var paymentEvent = new PaymentReceivedEvent(paymentId, invoiceId, amount, paymentDate, paymentMethod);

        // Assert
        Assert.Equal(paymentId, paymentEvent.PaymentId);
        Assert.Equal(invoiceId, paymentEvent.InvoiceId);
        Assert.Equal(amount, paymentEvent.Amount);
        Assert.Equal(paymentDate, paymentEvent.PaymentDate);
        Assert.Equal(paymentMethod, paymentEvent.PaymentMethod);
        Assert.Equal("PaymentReceivedEvent", paymentEvent.EventType);
        Assert.NotEqual(Guid.Empty, paymentEvent.EventId);
        Assert.True(paymentEvent.OccurredAt <= DateTime.UtcNow);
    }

    [Fact]
    public void DomainEvent_ShouldHaveUniqueEventIds()
    {
        // Arrange & Act
        var event1 = new InvoiceCreatedEvent(Guid.NewGuid(), "INV-001", 100m, "CUST-001", DateTime.Today);
        var event2 = new InvoiceCreatedEvent(Guid.NewGuid(), "INV-002", 200m, "CUST-002", DateTime.Today);

        // Assert
        Assert.NotEqual(event1.EventId, event2.EventId);
    }
}