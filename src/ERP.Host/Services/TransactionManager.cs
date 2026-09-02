using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ERP.SharedKernel.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace ERP.Host.Services;

/// <summary>
/// Manages distributed transactions across multiple plugin databases.
/// Implements the Saga pattern for cross-plugin transaction management.
/// </summary>
public class TransactionManager : ITransactionManager
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TransactionManager> _logger;
    private readonly Stack<IDbContextTransaction> _activeTransactions = new();

    public TransactionManager(IServiceProvider serviceProvider, ILogger<TransactionManager> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Gets whether there is an active transaction.
    /// </summary>
    public bool HasActiveTransaction => _activeTransactions.Count > 0;

    /// <summary>
    /// Begins a new distributed transaction.
    /// </summary>
    /// <returns>A transaction scope.</returns>
    public ITransactionScope BeginTransaction()
    {
        return new TransactionScope(this);
    }

    /// <summary>
    /// Begins a new transaction on the specified DbContext.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    /// <returns>A task representing the transaction.</returns>
    public async Task BeginTransactionAsync<TContext>() where TContext : DbContext
    {
        var dbContext = _serviceProvider.GetRequiredService<TContext>();
        var transaction = await dbContext.Database.BeginTransactionAsync();
        _activeTransactions.Push(transaction);
        _logger.LogDebug("Began transaction on {DbContext}", typeof(TContext).Name);
    }

    /// <summary>
    /// Commits all active transactions.
    /// </summary>
    /// <returns>A task representing the commit operation.</returns>
    public async Task CommitAsync()
    {
        _logger.LogDebug("Committing {Count} transactions", _activeTransactions.Count);

        var exceptions = new List<Exception>();

        while (_activeTransactions.Count > 0)
        {
            var transaction = _activeTransactions.Pop();
            try
            {
                await transaction.CommitAsync();
                transaction.Dispose();
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
                try
                {
                    await transaction.RollbackAsync();
                }
                catch (Exception rollbackEx)
                {
                    _logger.LogError(rollbackEx, "Error rolling back transaction");
                }
                transaction.Dispose();
            }
        }

        if (exceptions.Count > 0)
        {
            throw new AggregateException("One or more transactions failed to commit", exceptions);
        }
    }

    /// <summary>
    /// Rolls back all active transactions.
    /// </summary>
    /// <returns>A task representing the rollback operation.</returns>
    public async Task RollbackAsync()
    {
        _logger.LogDebug("Rolling back {Count} transactions", _activeTransactions.Count);

        var exceptions = new List<Exception>();

        while (_activeTransactions.Count > 0)
        {
            var transaction = _activeTransactions.Pop();
            try
            {
                await transaction.RollbackAsync();
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
            finally
            {
                transaction.Dispose();
            }
        }

        if (exceptions.Count > 0)
        {
            _logger.LogError(exceptions[0], "Errors occurred during rollback");
        }
    }

    /// <summary>
    /// Executes an action within a transaction scope.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the execution.</returns>
    public async Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken cancellationToken = default)
    {
        await BeginTransactionAsync<ApplicationDbContext>();
        try
        {
            await action();
            await CommitAsync();
        }
        catch
        {
            await RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Executes an action within a transaction scope and returns a result.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="func">The function to execute.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the execution with the result.</returns>
    public async Task<TResult> ExecuteInTransactionAsync<TResult>(Func<Task<TResult>> func, CancellationToken cancellationToken = default)
    {
        await BeginTransactionAsync<ApplicationDbContext>();
        try
        {
            var result = await func();
            await CommitAsync();
            return result;
        }
        catch
        {
            await RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Executes a compensating action if the main action fails.
    /// Implements the Saga pattern for distributed transactions.
    /// </summary>
    /// <param name="action">The main action to execute.</param>
    /// <param name="compensatingAction">The compensating action to execute on failure.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the execution.</returns>
    public async Task ExecuteWithCompensationAsync(
        Func<Task> action,
        Func<Task> compensatingAction,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Main action failed, executing compensation");
            try
            {
                await compensatingAction();
            }
            catch (Exception compEx)
            {
                _logger.LogError(compEx, "Compensating action also failed");
            }
            throw;
        }
    }

    /// <summary>
    /// Executes multiple actions in a transactional manner.
    /// If any action fails, all previous actions are compensated.
    /// </summary>
    /// <param name="actions">The actions to execute.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the execution.</returns>
    public async Task ExecuteTransactionalWorkflowAsync(
        IEnumerable<(Func<Task> action, Func<Task> compensate)> actions,
        CancellationToken cancellationToken = default)
    {
        var executedActions = new List<(Func<Task> action, Func<Task> compensate)>();

        try
        {
            foreach (var (action, compensate) in actions)
            {
                await action();
                executedActions.Add((action, compensate));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Transactional workflow failed, compensating");
            
            // Execute compensation in reverse order
            for (int i = executedActions.Count - 1; i >= 0; i--)
            {
                try
                {
                    await executedActions[i].compensate();
                }
                catch (Exception compEx)
                {
                    _logger.LogError(compEx, "Compensation failed for action {Index}", i);
                }
            }
            throw;
        }
    }
}

/// <summary>
/// Interface for managing distributed transactions.
/// </summary>
public interface ITransactionManager : IUnitOfWork
{
    /// <summary>
    /// Begins a new distributed transaction.
    /// </summary>
    /// <returns>A transaction scope.</returns>
    ITransactionScope BeginTransaction();

    /// <summary>
    /// Begins a new transaction on the specified DbContext.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    /// <returns>A task representing the transaction.</returns>
    Task BeginTransactionAsync<TContext>() where TContext : DbContext;

    /// <summary>
    /// Executes a compensating action if the main action fails.
    /// </summary>
    /// <param name="action">The main action to execute.</param>
    /// <param name="compensatingAction">The compensating action to execute on failure.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the execution.</returns>
    Task ExecuteWithCompensationAsync(
        Func<Task> action,
        Func<Task> compensatingAction,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes multiple actions in a transactional manner.
    /// </summary>
    /// <param name="actions">The actions to execute with their compensating actions.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the execution.</returns>
    Task ExecuteTransactionalWorkflowAsync(
        IEnumerable<(Func<Task> action, Func<Task> compensate)> actions,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a transaction scope for managing the lifetime of a transaction.
/// </summary>
public interface ITransactionScope : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Gets the transaction manager.
    /// </summary>
    ITransactionManager TransactionManager { get; }

    /// <summary>
    /// Commits the transaction.
    /// </summary>
    /// <returns>A task representing the commit operation.</returns>
    Task CommitAsync();

    /// <summary>
    /// Rolls back the transaction.
    /// </summary>
    /// <returns>A task representing the rollback operation.</returns>
    Task RollbackAsync();
}

/// <summary>
/// Implementation of ITransactionScope.
/// </summary>
public class TransactionScope : ITransactionScope
{
    private readonly ITransactionManager _transactionManager;
    private bool _committed = false;
    private bool _disposed = false;

    public TransactionScope(ITransactionManager transactionManager)
    {
        _transactionManager = transactionManager;
    }

    public ITransactionManager TransactionManager => _transactionManager;

    public async Task CommitAsync()
    {
        if (_committed)
        {
            return;
        }

        await _transactionManager.CommitAsync();
        _committed = true;
    }

    public async Task RollbackAsync()
    {
        if (_committed)
        {
            return;
        }

        await _transactionManager.RollbackAsync();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (!_committed)
        {
            _transactionManager.RollbackAsync().GetAwaiter().GetResult();
        }

        _disposed = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        if (!_committed)
        {
            await _transactionManager.RollbackAsync();
        }

        _disposed = true;
    }
}
