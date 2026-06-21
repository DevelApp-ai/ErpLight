using ERP.Host.Services;
using ERP.SharedKernel.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace ERP.Host.Tests;

public class EventPublisherTests
{
    [Fact]
    public async Task PublishAsync_ShouldInvokeAllRegisteredHandlers()
    {
        var services = new ServiceCollection();
        services.AddScoped<IEventHandler<TestDomainEvent>, RecordingHandler>();
        services.AddScoped<IEventHandler<TestDomainEvent>, RecordingHandler>();
        services.AddSingleton<EventRecorder>();
        var provider = services.BuildServiceProvider();
        var publisher = new EventPublisher(provider, NullLogger<EventPublisher>.Instance);

        await publisher.PublishAsync(new TestDomainEvent());

        var recorder = provider.GetRequiredService<EventRecorder>();
        Assert.Equal(2, recorder.HandledCount);
    }

    private sealed class TestDomainEvent : DomainEvent;

    private sealed class RecordingHandler : IEventHandler<TestDomainEvent>
    {
        private readonly EventRecorder _recorder;

        public RecordingHandler(EventRecorder recorder)
        {
            _recorder = recorder;
        }

        public Task HandleAsync(TestDomainEvent domainEvent)
        {
            _recorder.HandledCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class EventRecorder
    {
        public int HandledCount { get; set; }
    }
}
