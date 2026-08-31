using ERP.Host.Services.Resilience;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Polly;
using Xunit;

namespace ERP.Host.Tests.Services;

/// <summary>
/// Unit tests for ResilienceService.
/// </summary>
public class ResilienceServiceTests : IDisposable
{
    private readonly ILogger<ResilienceService> _logger;
    private readonly ResilienceService _resilienceService;

    public ResilienceServiceTests()
    {
        _logger = NullLogger<ResilienceService>.Instance;
        _resilienceService = new ResilienceService(_logger);
    }

    public void Dispose()
    {
    }

    [Fact]
    public void CreateRetryPolicy_ShouldReturnRetryPolicy()
    {
        // Act
        var policy = _resilienceService.CreateRetryPolicy(3, 1000);

        // Assert
        Assert.NotNull(policy);
        Assert.IsType<AsyncRetryPolicy>(policy);
    }

    [Fact]
    public async Task CreateRetryPolicy_ShouldRetryOnFailure()
    {
        // Arrange
        var attemptCount = 0;
        var policy = _resilienceService.CreateRetryPolicy(3, 100);

        // Act
        await policy.ExecuteAsync(async () =>
        {
            attemptCount++;
            if (attemptCount < 3)
            {
                throw new Exception("Test exception");
            }
        });

        // Assert
        Assert.Equal(3, attemptCount);
    }

    [Fact]
    public async Task CreateRetryPolicy_ShouldThrowAfterMaxRetries()
    {
        // Arrange
        var attemptCount = 0;
        var policy = _resilienceService.CreateRetryPolicy(3, 100);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => policy.ExecuteAsync(async () =>
        {
            attemptCount++;
            throw new Exception("Test exception");
        }));

        Assert.Equal(4, attemptCount); // Initial + 3 retries
    }

    [Fact]
    public void CreateExponentialBackoffPolicy_ShouldReturnRetryPolicy()
    {
        // Act
        var policy = _resilienceService.CreateExponentialBackoffPolicy(3, 1000, 30000);

        // Assert
        Assert.NotNull(policy);
        Assert.IsType<AsyncRetryPolicy>(policy);
    }

    [Fact]
    public async Task CreateExponentialBackoffPolicy_ShouldRetryWithIncreasingDelay()
    {
        // Arrange
        var attemptTimes = new List<DateTime>();
        var policy = _resilienceService.CreateExponentialBackoffPolicy(3, 100, 30000);

        // Act
        await policy.ExecuteAsync(async () =>
        {
            attemptTimes.Add(DateTime.UtcNow);
            if (attemptTimes.Count < 3)
            {
                throw new Exception("Test exception");
            }
        });

        // Assert
        Assert.Equal(3, attemptTimes.Count);
        
        // Check that delays are increasing (approximately exponential)
        var delay1 = (attemptTimes[1] - attemptTimes[0]).TotalMilliseconds;
        var delay2 = (attemptTimes[2] - attemptTimes[1]).TotalMilliseconds;
        
        // First retry: ~100ms, Second retry: ~200ms (exponential)
        Assert.InRange(delay1, 90, 150); // Allow some tolerance
        Assert.InRange(delay2, 180, 250); // Allow some tolerance
    }

    [Fact]
    public void CreateCircuitBreakerPolicy_ShouldReturnCircuitBreakerPolicy()
    {
        // Act
        var policy = _resilienceService.CreateCircuitBreakerPolicy(5, 30000);

        // Assert
        Assert.NotNull(policy);
        Assert.IsType<AsyncCircuitBreakerPolicy>(policy);
    }

    [Fact]
    public async Task CreateCircuitBreakerPolicy_ShouldBreakAfterThreshold()
    {
        // Arrange
        var attemptCount = 0;
        var policy = _resilienceService.CreateCircuitBreakerPolicy(3, 1000);

        // Act & Assert - First 3 attempts should work, then circuit breaks
        for (int i = 0; i < 3; i++)
        {
            await Assert.ThrowsAsync<Exception>(() => policy.ExecuteAsync(async () =>
            {
                attemptCount++;
                throw new Exception("Test exception");
            }));
        }

        // After 3 failures, circuit should be broken
        // The 4th attempt should fail immediately without executing the action
        await Assert.ThrowsAsync<BrokenCircuitException>(() => policy.ExecuteAsync(async () =>
        {
            attemptCount++;
        }));

        // attemptCount should still be 3
        Assert.Equal(3, attemptCount);
    }

    [Fact]
    public void CreateTimeoutPolicy_ShouldReturnTimeoutPolicy()
    {
        // Act
        var policy = _resilienceService.CreateTimeoutPolicy(5000);

        // Assert
        Assert.NotNull(policy);
        Assert.IsType<AsyncTimeoutPolicy>(policy);
    }

    [Fact]
    public async Task CreateTimeoutPolicy_ShouldTimeoutAfterSpecifiedTime()
    {
        // Arrange
        var policy = _resilienceService.CreateTimeoutPolicy(100); // 100ms timeout

        // Act & Assert
        await Assert.ThrowsAsync<TimeoutRejectedException>(() => policy.ExecuteAsync(async () =>
        {
            await Task.Delay(500); // This takes longer than the timeout
        }));
    }

    [Fact]
    public async Task CreateTimeoutPolicy_ShouldCompleteWithinTimeout()
    {
        // Arrange
        var completed = false;
        var policy = _resilienceService.CreateTimeoutPolicy(5000); // 5s timeout

        // Act
        await policy.ExecuteAsync(async () =>
        {
            await Task.Delay(100);
            completed = true;
        });

        // Assert
        Assert.True(completed);
    }

    [Fact]
    public void CreateResilientPolicy_ShouldReturnCombinedPolicy()
    {
        // Act
        var policy = _resilienceService.CreateResilientPolicy(3, 5, 30000);

        // Assert
        Assert.NotNull(policy);
        Assert.IsType<AsyncPolicy>(policy);
    }

    [Fact]
    public void CreateRetryWithTimeoutPolicy_ShouldReturnCombinedPolicy()
    {
        // Act
        var policy = _resilienceService.CreateRetryWithTimeoutPolicy(3, 5000);

        // Assert
        Assert.NotNull(policy);
        Assert.IsType<AsyncPolicy>(policy);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_ShouldReturnResultOnSuccess()
    {
        // Act
        var result = await _resilienceService.ExecuteWithRetryAsync(async () =>
        {
            return "Success";
        });

        // Assert
        Assert.Equal("Success", result);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_ShouldRetryOnFailure()
    {
        // Arrange
        var attemptCount = 0;

        // Act
        var result = await _resilienceService.ExecuteWithRetryAsync(async () =>
        {
            attemptCount++;
            if (attemptCount < 2)
            {
                throw new Exception("Test exception");
            }
            return "Success";
        }, retryCount: 3, delayMs: 100);

        // Assert
        Assert.Equal("Success", result);
        Assert.Equal(2, attemptCount);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_ShouldThrowAfterMaxRetries()
    {
        // Arrange
        var attemptCount = 0;

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _resilienceService.ExecuteWithRetryAsync(async () =>
        {
            attemptCount++;
            throw new Exception("Test exception");
        }, retryCount: 3, delayMs: 100));

        Assert.Equal(4, attemptCount);
    }

    [Fact]
    public async Task ExecuteWithCircuitBreakerAsync_ShouldReturnResultOnSuccess()
    {
        // Act
        var result = await _resilienceService.ExecuteWithCircuitBreakerAsync(async () =>
        {
            return "Success";
        });

        // Assert
        Assert.Equal("Success", result);
    }

    [Fact]
    public async Task ExecuteWithCircuitBreakerAsync_ShouldBreakAfterThreshold()
    {
        // Arrange
        var attemptCount = 0;

        // Act - Execute until circuit breaks
        for (int i = 0; i < 5; i++)
        {
            await Assert.ThrowsAnyAsync<Exception>(() => _resilienceService.ExecuteWithCircuitBreakerAsync(async () =>
            {
                attemptCount++;
                throw new Exception("Test exception");
            }, exceptionsAllowedBeforeBreaking: 3, breakDurationMs: 1000));
        }

        // After 5 attempts, circuit should be broken
        await Assert.ThrowsAsync<BrokenCircuitException>(() => _resilienceService.ExecuteWithCircuitBreakerAsync(async () =>
        {
            attemptCount++;
            return "Should not reach here";
        }, exceptionsAllowedBeforeBreaking: 3, breakDurationMs: 1000));

        // attemptCount should be 5
        Assert.Equal(5, attemptCount);
    }

    [Fact]
    public async Task ExecuteWithTimeoutAsync_ShouldReturnResultOnSuccess()
    {
        // Act
        var result = await _resilienceService.ExecuteWithTimeoutAsync(async () =>
        {
            return "Success";
        });

        // Assert
        Assert.Equal("Success", result);
    }

    [Fact]
    public async Task ExecuteWithTimeoutAsync_ShouldTimeout()
    {
        // Act & Assert
        await Assert.ThrowsAsync<TimeoutRejectedException>(() => _resilienceService.ExecuteWithTimeoutAsync(async () =>
        {
            await Task.Delay(500);
            return "Should timeout";
        }, timeoutMs: 100));
    }

    [Fact]
    public async Task ExecuteWithResilienceAsync_ShouldHandleRetryAndCircuitBreaker()
    {
        // Arrange
        var attemptCount = 0;

        // Act
        var result = await _resilienceService.ExecuteWithResilienceAsync(async () =>
        {
            attemptCount++;
            if (attemptCount < 2)
            {
                throw new Exception("Test exception");
            }
            return "Success";
        }, retryCount: 3, exceptionsAllowedBeforeBreaking: 5, breakDurationMs: 1000);

        // Assert
        Assert.Equal("Success", result);
        Assert.Equal(2, attemptCount);
    }

    [Fact]
    public async Task ExecuteWithRetryAndTimeoutAsync_ShouldReturnResultOnSuccess()
    {
        // Act
        var result = await _resilienceService.ExecuteWithRetryAndTimeoutAsync(async () =>
        {
            return "Success";
        });

        // Assert
        Assert.Equal("Success", result);
    }

    [Fact]
    public async Task ExecuteWithRetryAndTimeoutAsync_ShouldRetryOnFailure()
    {
        // Arrange
        var attemptCount = 0;

        // Act
        var result = await _resilienceService.ExecuteWithRetryAndTimeoutAsync(async () =>
        {
            attemptCount++;
            if (attemptCount < 2)
            {
                throw new Exception("Test exception");
            }
            return "Success";
        }, retryCount: 3, timeoutMs: 5000);

        // Assert
        Assert.Equal("Success", result);
        Assert.Equal(2, attemptCount);
    }

    [Fact]
    public async Task ExecuteWithRetryAndTimeoutAsync_ShouldTimeout()
    {
        // Arrange
        var attemptCount = 0;

        // Act & Assert
        await Assert.ThrowsAsync<TimeoutRejectedException>(() => _resilienceService.ExecuteWithRetryAndTimeoutAsync(async () =>
        {
            attemptCount++;
            await Task.Delay(500);
            return "Should timeout";
        }, retryCount: 3, timeoutMs: 100));

        // Should have tried once and timed out
        Assert.Equal(1, attemptCount);
    }

    [Fact]
    public async Task ExecuteWithFallbackAsync_ShouldReturnResultOnSuccess()
    {
        // Act
        var result = await _resilienceService.ExecuteWithFallbackAsync(async () =>
        {
            return "Success";
        }, "Fallback");

        // Assert
        Assert.Equal("Success", result);
    }

    [Fact]
    public async Task ExecuteWithFallbackAsync_ShouldReturnFallbackOnFailure()
    {
        // Act
        var result = await _resilienceService.ExecuteWithFallbackAsync(async () =>
        {
            throw new Exception("Test exception");
        }, "Fallback");

        // Assert
        Assert.Equal("Fallback", result);
    }

    [Fact]
    public async Task ExecuteWithFallbackAsync_ShouldReturnFallbackOnRetryExhaustion()
    {
        // Arrange
        var attemptCount = 0;

        // Act
        var result = await _resilienceService.ExecuteWithFallbackAsync(async () =>
        {
            attemptCount++;
            throw new Exception("Test exception");
        }, "Fallback", retryCount: 3);

        // Assert
        Assert.Equal("Fallback", result);
        Assert.Equal(4, attemptCount); // Initial + 3 retries
    }

    [Fact]
    public async Task ExecuteWithFallbackAsync_ShouldReturnDefaultFallbackForDefaultValue()
    {
        // Act
        var result = await _resilienceService.ExecuteWithFallbackAsync(async () =>
        {
            throw new Exception("Test exception");
        }, 42);

        // Assert
        Assert.Equal(42, result);
    }
}
