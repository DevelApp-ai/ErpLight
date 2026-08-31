using System;

namespace ERP.Host.Services;

/// <summary>
/// Service for accessing the current request's correlation ID.
/// This allows plugins to include correlation IDs in their logs and operations.
/// </summary>
public interface ICorrelationIdService
{
    /// <summary>
    /// Gets the current correlation ID for the request, or null if not in a request context.
    /// </summary>
    string? CurrentCorrelationId { get; }
}

/// <summary>
/// Implementation of ICorrelationIdService that reads from HttpContext.Items.
/// </summary>
public class CorrelationIdService : ICorrelationIdService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CorrelationIdService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? CurrentCorrelationId => _httpContextAccessor.HttpContext?.Items["CorrelationId"] as string;
}
