using ERP.SharedKernel.Events;

namespace ERP.SharedKernel.Tests;

public class DomainEventTests
{
    [Fact]
    public void DomainEvent_ShouldHaveUniqueEventIds()
    {
        // This test uses a simple implementation of DomainEvent for testing
        var event1 = new TestDomainEvent();
        var event2 = new TestDomainEvent();

        // Assert
        Assert.NotEqual(event1.EventId, event2.EventId);
    }
}

/// <summary>
/// Simple test implementation of DomainEvent for testing purposes
/// </summary>
public class TestDomainEvent : DomainEvent
{
}