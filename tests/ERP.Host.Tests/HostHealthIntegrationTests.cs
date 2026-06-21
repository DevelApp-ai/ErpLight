using Microsoft.AspNetCore.Mvc.Testing;

namespace ERP.Host.Tests;

public class HostHealthIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public HostHealthIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task LiveEndpoint_ShouldReturnSuccess()
    {
        var response = await _client.GetAsync("/health/live");

        Assert.True(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task ReadyEndpoint_ShouldReturnServiceUnavailable_WhenNoPluginsLoaded()
    {
        var response = await _client.GetAsync("/health/ready");

        Assert.Equal(System.Net.HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task Requests_ShouldIncludeCorrelationIdHeader()
    {
        var response = await _client.GetAsync("/health/live");

        Assert.True(response.Headers.Contains("X-Correlation-ID"));
        var correlationId = response.Headers.GetValues("X-Correlation-ID").SingleOrDefault();
        Assert.False(string.IsNullOrWhiteSpace(correlationId));
    }
}
