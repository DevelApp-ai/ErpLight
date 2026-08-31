using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace ERP.Host.Services.Resilience;

/// <summary>
/// Service for adding resilience patterns (retry, circuit breaker, timeout) to operations.
/// </summary>
public class ResilienceService
{
    private readonly ILogger<ResilienceService> _logger;

    public ResilienceService(ILogger<ResilienceService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Creates a retry policy with the specified number of retries.
    /// </summary>
    /// <param name="retryCount">The number of retry attempts.</param>
    /// <param name="delayMs">The delay between retries in milliseconds.</param>
    /// <returns>An async retry policy.</returns>
    public AsyncRetryPolicy CreateRetryPolicy(int retryCount = 3, int delayMs = 1000)
    {
        return Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount,
                retryAttempt => TimeSpan.FromMilliseconds(delayMs * retryAttempt),
                onRetry: (exception, delay, retryCount, context) =>
                {
                    _logger.LogWarning(exception, "Retry {RetryCount} of {RetryCount} due to: {ExceptionMessage}", 
                        retryCount, retryCount, exception.Message);
                });
    }

    /// <summary>
    /// Creates a retry policy with exponential backoff.
    /// </summary>
    /// <param name="retryCount">The number of retry attempts.</param>
    /// <param name="initialDelayMs">The initial delay in milliseconds.</param>
    /// <param name="maxDelayMs">The maximum delay in milliseconds.</param>
    /// <returns>An async retry policy.</returns>
    public AsyncRetryPolicy CreateExponentialBackoffPolicy(int retryCount = 3, int initialDelayMs = 1000, int maxDelayMs = 30000)
    {
        return Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount,
                retryAttempt => TimeSpan.FromMilliseconds(Math.Min(initialDelayMs * Math.Pow(2, retryAttempt - 1), maxDelayMs)),
                onRetry: (exception, delay, retryCount, context) =>
                {
                    _logger.LogWarning(exception, "Exponential backoff retry {RetryCount} of {RetryCount} due to: {ExceptionMessage}. Waiting {DelayMs}ms", 
                        retryCount, retryCount, exception.Message, delay.TotalMilliseconds);
                });
    }

    /// <summary>
    /// Creates a circuit breaker policy.
    /// </summary>
    /// <param name="exceptionsAllowedBeforeBreaking">The number of exceptions allowed before breaking the circuit.</param>
    /// <param name="breakDurationMs">The duration to break the circuit in milliseconds.</param>
    /// <returns>An async circuit breaker policy.</returns>
    public AsyncCircuitBreakerPolicy CreateCircuitBreakerPolicy(int exceptionsAllowedBeforeBreaking = 5, int breakDurationMs = 30000)
    {
        return Policy
            .Handle<Exception>()
            .CircuitBreakerAsync(
                exceptionsAllowedBeforeBreaking,
                TimeSpan.FromMilliseconds(breakDurationMs),
                onBreak: (exception, breakDelay) =>
                {
                    _logger.LogError(exception, "Circuit broken! Will not execute for {BreakDelayMs}ms", breakDelay.TotalMilliseconds);
                },
                onReset: () =>
                {
                    _logger.LogInformation("Circuit reset! Ready to execute again");
                },
                onHalfOpen: () =>
                {
                    _logger.LogInformation("Circuit half-open; next call is a trial");
                });
    }

    /// <summary>
    /// Creates a timeout policy.
    /// </summary>
    /// <param name="timeoutMs">The timeout duration in milliseconds.</param>
    /// <returns>An async timeout policy.</returns>
    public AsyncTimeoutPolicy CreateTimeoutPolicy(int timeoutMs = 5000)
    {
        return Policy.TimeoutAsync(TimeSpan.FromMilliseconds(timeoutMs), onTimeout: (context, timespan, task) =>
        {
            _logger.LogWarning("Operation timed out after {TimeoutMs}ms", timespan.TotalMilliseconds);
        });
    }

    /// <summary>
    /// Creates a combined policy with retry and circuit breaker.
    /// </summary>
    /// <param name="retryCount">The number of retry attempts.</param>
    /// <param name="exceptionsAllowedBeforeBreaking">The number of exceptions allowed before breaking the circuit.</param>
    /// <param name="breakDurationMs">The duration to break the circuit in milliseconds.</param>
    /// <returns>A combined async policy.</returns>
    public AsyncPolicy CreateResilientPolicy(int retryCount = 3, int exceptionsAllowedBeforeBreaking = 5, int breakDurationMs = 30000)
    {
        var retryPolicy = CreateRetryPolicy(retryCount);
        var circuitBreakerPolicy = CreateCircuitBreakerPolicy(exceptionsAllowedBeforeBreaking, breakDurationMs);
        
        return Policy.WrapAsync(retryPolicy, circuitBreakerPolicy);
    }

    /// <summary>
    /// Creates a policy with retry and timeout.
    /// </summary>
    /// <param name="retryCount">The number of retry attempts.</param>
    /// <param name="timeoutMs">The timeout duration in milliseconds.</param>
    /// <returns>A combined async policy.</returns>
    public AsyncPolicy CreateRetryWithTimeoutPolicy(int retryCount = 3, int timeoutMs = 5000)
    {
        var retryPolicy = CreateRetryPolicy(retryCount);
        var timeoutPolicy = CreateTimeoutPolicy(timeoutMs);
        
        return Policy.WrapAsync(retryPolicy, timeoutPolicy);
    }

    /// <summary>
    /// Executes an action with retry policy.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="action">The action to execute.</param>
    /// <param name="retryCount">The number of retry attempts.</param>
    /// <param name="delayMs">The delay between retries in milliseconds.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the execution with the result.</returns>
    public async Task<TResult> ExecuteWithRetryAsync<TResult>(
        Func<Task<TResult>> action,
        int retryCount = 3,
        int delayMs = 1000,
        CancellationToken cancellationToken = default)
    {
        var policy = CreateRetryPolicy(retryCount, delayMs);
        return await policy.ExecuteAsync(action, cancellationToken);
    }

    /// <summary>
    /// Executes an action with circuit breaker policy.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="action">The action to execute.</param>
    /// <param name="exceptionsAllowedBeforeBreaking">The number of exceptions allowed before breaking the circuit.</param>
    /// <param name="breakDurationMs">The duration to break the circuit in milliseconds.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the execution with the result.</returns>
    public async Task<TResult> ExecuteWithCircuitBreakerAsync<TResult>(
        Func<Task<TResult>> action,
        int exceptionsAllowedBeforeBreaking = 5,
        int breakDurationMs = 30000,
        CancellationToken cancellationToken = default)
    {
        var policy = CreateCircuitBreakerPolicy(exceptionsAllowedBeforeBreaking, breakDurationMs);
        return await policy.ExecuteAsync(action, cancellationToken);
    }

    /// <summary>
    /// Executes an action with timeout policy.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="action">The action to execute.</param>
    /// <param name="timeoutMs">The timeout duration in milliseconds.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the execution with the result.</returns>
    public async Task<TResult> ExecuteWithTimeoutAsync<TResult>(
        Func<Task<TResult>> action,
        int timeoutMs = 5000,
        CancellationToken cancellationToken = default)
    {
        var policy = CreateTimeoutPolicy(timeoutMs);
        return await policy.ExecuteAsync(action, cancellationToken);
    }

    /// <summary>
    /// Executes an action with combined resilience policies.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="action">The action to execute.</param>
    /// <param name="retryCount">The number of retry attempts.</param>
    /// <param name="exceptionsAllowedBeforeBreaking">The number of exceptions allowed before breaking the circuit.</param>
    /// <param name="breakDurationMs">The duration to break the circuit in milliseconds.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the execution with the result.</returns>
    public async Task<TResult> ExecuteWithResilienceAsync<TResult>(
        Func<Task<TResult>> action,
        int retryCount = 3,
        int exceptionsAllowedBeforeBreaking = 5,
        int breakDurationMs = 30000,
        CancellationToken cancellationToken = default)
    {
        var policy = CreateResilientPolicy(retryCount, exceptionsAllowedBeforeBreaking, breakDurationMs);
        return await policy.ExecuteAsync(action, cancellationToken);
    }

    /// <summary>
    /// Executes an action with retry and timeout policies.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="action">The action to execute.</param>
    /// <param name="retryCount">The number of retry attempts.</param>
    /// <param name="timeoutMs">The timeout duration in milliseconds.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the execution with the result.</returns>
    public async Task<TResult> ExecuteWithRetryAndTimeoutAsync<TResult>(
        Func<Task<TResult>> action,
        int retryCount = 3,
        int timeoutMs = 5000,
        CancellationToken cancellationToken = default)
    {
        var policy = CreateRetryWithTimeoutPolicy(retryCount, timeoutMs);
        return await policy.ExecuteAsync(action, cancellationToken);
    }

    /// <summary>
    /// Executes an action with fallback value on failure.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="action">The action to execute.</param>
    /// <param name="fallbackValue">The fallback value to return on failure.</param>
    /// <param name="retryCount">The number of retry attempts.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the execution with the result.</returns>
    public async Task<TResult> ExecuteWithFallbackAsync<TResult>(
        Func<Task<TResult>> action,
        TResult fallbackValue,
        int retryCount = 3,
        CancellationToken cancellationToken = default)
    {
        var policy = Policy<TResult>
            .Handle<Exception>()
            .OrResult(default(TResult)!)
            .FallbackAsync(fallbackValue);

        return await policy.ExecuteAsync(action, cancellationToken);
    }
}
