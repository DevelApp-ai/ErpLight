using ERP.Plugin.Finance.Events;
using ERP.Plugin.Finance.Services;
using ERP.SharedKernel.Events;
using Moq;
using Xunit;

namespace ERP.Plugin.Finance.Tests;

/// <summary>
/// Tests for InvoiceService event publishing functionality.
/// </summary>
public class InvoiceServiceEventTests
{
    [Fact]
    public async Task CreateInvoiceAsync_ShouldPublishInvoiceCreatedEvent()
    {
        // Arrange
        var mockEventPublisher = new Mock<IEventPublisher>();
        var service = new InvoiceService(mockEventPublisher.Object);

        var request = new CreateInvoiceRequest
        {
            CustomerId = "CUST-001",
            CustomerName = "Test Customer",
            Amount = 1000.00m,
            DueDate = DateTime.Today.AddDays(30)
        };

        InvoiceCreatedEvent? capturedEvent = null;
        mockEventPublisher.Setup(x => x.PublishAsync(It.IsAny<DomainEvent>()))
            .Callback<DomainEvent>(evt => capturedEvent = evt as InvoiceCreatedEvent);

        // Act
        await service.CreateInvoiceAsync(request);

        // Assert
        mockEventPublisher.Verify(x => x.PublishAsync(It.IsAny<InvoiceCreatedEvent>()), Times.Once);
        Assert.NotNull(capturedEvent);
        Assert.NotEqual(Guid.Empty, capturedEvent.InvoiceId);
        Assert.Equal("CUST-001", capturedEvent.CustomerId);
        Assert.Equal(1000.00m, capturedEvent.Amount);
        Assert.Equal(request.DueDate, capturedEvent.DueDate);
    }

    [Fact]
    public async Task MarkInvoiceAsPaidAsync_ShouldPublishPaymentReceivedEvent()
    {
        // Arrange
        var mockEventPublisher = new Mock<IEventPublisher>();
        var service = new InvoiceService(mockEventPublisher.Object);

        // First create an invoice
        var request = new CreateInvoiceRequest
        {
            CustomerId = "CUST-001",
            CustomerName = "Test Customer",
            Amount = 1000.00m,
            DueDate = DateTime.Today.AddDays(30)
        };
        var invoice = await service.CreateInvoiceAsync(request);

        PaymentReceivedEvent? capturedEvent = null;
        mockEventPublisher.Setup(x => x.PublishAsync(It.IsAny<DomainEvent>()))
            .Callback<DomainEvent>(evt => capturedEvent = evt as PaymentReceivedEvent);

        // Act
        var result = await service.MarkInvoiceAsPaidAsync(invoice.Id, 1000.00m, "Credit Card");

        // Assert
        Assert.True(result);
        mockEventPublisher.Verify(x => x.PublishAsync(It.IsAny<PaymentReceivedEvent>()), Times.Once);
        Assert.NotNull(capturedEvent);
        Assert.Equal(invoice.Id, capturedEvent.InvoiceId);
        Assert.Equal(1000.00m, capturedEvent.Amount);
        Assert.Equal("Credit Card", capturedEvent.PaymentMethod);
        Assert.NotEqual(Guid.Empty, capturedEvent.PaymentId);
    }

    [Fact]
    public async Task CreateInvoiceAsync_ShouldPublishEventWithCorrectProperties()
    {
        // Arrange
        var mockEventPublisher = new Mock<IEventPublisher>();
        var service = new InvoiceService(mockEventPublisher.Object);

        var request = new CreateInvoiceRequest
        {
            CustomerId = "CUST-123",
            CustomerName = "John Doe",
            Amount = 1500.50m,
            DueDate = new DateTime(2024, 12, 31)
        };

        InvoiceCreatedEvent? capturedEvent = null;
        mockEventPublisher.Setup(x => x.PublishAsync(It.IsAny<DomainEvent>()))
            .Callback<DomainEvent>(evt => capturedEvent = evt as InvoiceCreatedEvent);

        // Act
        var invoice = await service.CreateInvoiceAsync(request);

        // Assert
        Assert.NotNull(capturedEvent);
        Assert.Equal(invoice.Id, capturedEvent.InvoiceId);
        Assert.StartsWith("INV-", capturedEvent.InvoiceNumber);
        Assert.Equal(request.Amount, capturedEvent.Amount);
        Assert.Equal(request.CustomerId, capturedEvent.CustomerId);
        Assert.Equal(request.DueDate, capturedEvent.DueDate);
        Assert.Equal("InvoiceCreatedEvent", capturedEvent.EventType);
        Assert.NotEqual(Guid.Empty, capturedEvent.EventId);
    }

    [Fact]
    public async Task MarkInvoiceAsPaidAsync_ShouldNotPublishEvent_WhenInvoiceNotFound()
    {
        // Arrange
        var mockEventPublisher = new Mock<IEventPublisher>();
        var service = new InvoiceService(mockEventPublisher.Object);

        var nonExistentInvoiceId = Guid.NewGuid();

        // Act
        var result = await service.MarkInvoiceAsPaidAsync(nonExistentInvoiceId, 100.00m, "PayPal");

        // Assert
        Assert.False(result);
        mockEventPublisher.Verify(x => x.PublishAsync(It.IsAny<PaymentReceivedEvent>()), Times.Never);
    }

    [Fact]
    public async Task CreateInvoiceAsync_ShouldGenerateUniqueInvoiceNumbers()
    {
        // Arrange
        var mockEventPublisher = new Mock<IEventPublisher>();
        var service = new InvoiceService(mockEventPublisher.Object);

        var request1 = new CreateInvoiceRequest
        {
            CustomerId = "CUST-001",
            Amount = 100m,
            DueDate = DateTime.Today
        };

        var request2 = new CreateInvoiceRequest
        {
            CustomerId = "CUST-002",
            Amount = 200m,
            DueDate = DateTime.Today
        };

        // Act
        var invoice1 = await service.CreateInvoiceAsync(request1);
        var invoice2 = await service.CreateInvoiceAsync(request2);

        // Assert
        Assert.NotEqual(invoice1.InvoiceNumber, invoice2.InvoiceNumber);
    }

    [Fact]
    public async Task CreateInvoiceAsync_ShouldSetInvoiceStatusToPending()
    {
        // Arrange
        var mockEventPublisher = new Mock<IEventPublisher>();
        var service = new InvoiceService(mockEventPublisher.Object);

        var request = new CreateInvoiceRequest
        {
            CustomerId = "CUST-001",
            Amount = 100m,
            DueDate = DateTime.Today
        };

        // Act
        var invoice = await service.CreateInvoiceAsync(request);

        // Assert
        Assert.Equal(InvoiceStatus.Pending, invoice.Status);
    }

    [Fact]
    public async Task MarkInvoiceAsPaidAsync_ShouldUpdateInvoiceStatus()
    {
        // Arrange
        var mockEventPublisher = new Mock<IEventPublisher>();
        var service = new InvoiceService(mockEventPublisher.Object);

        var request = new CreateInvoiceRequest
        {
            CustomerId = "CUST-001",
            Amount = 100m,
            DueDate = DateTime.Today
        };
        var invoice = await service.CreateInvoiceAsync(request);

        // Act
        await service.MarkInvoiceAsPaidAsync(invoice.Id, 100m, "Credit Card");

        // Assert
        var updatedInvoice = await service.GetInvoiceAsync(invoice.Id);
        Assert.NotNull(updatedInvoice);
        Assert.Equal(InvoiceStatus.Paid, updatedInvoice.Status);
        Assert.NotNull(updatedInvoice.PaidAt);
    }
}
