using System;
using System.Threading;
using System.Threading.Tasks;

namespace ERP.SharedKernel.Contracts;

/// <summary>
/// Interface for unit of work pattern to manage transactions across multiple repositories.
/// </summary>
public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Begins a new transaction.
    /// </summary>
    /// <returns>A task representing the transaction.</returns>
    Task BeginTransactionAsync();

    /// <summary>
    /// Commits the current transaction.
    /// </summary>
    /// <returns>A task representing the commit operation.</returns>
    Task CommitAsync();

    /// <summary>
    /// Rolls back the current transaction.
    /// </summary>
    /// <returns>A task representing the rollback operation.</returns>
    Task RollbackAsync();

    /// <summary>
    /// Executes a function within a transaction scope.
    /// </summary>
    /// <param name="action">The function to execute.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the execution.</returns>
    Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a function within a transaction scope and returns a result.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="func">The function to execute.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the execution with the result.</returns>
    Task<TResult> ExecuteInTransactionAsync<TResult>(Func<Task<TResult>> func, CancellationToken cancellationToken = default);
}

/// <summary>
/// Base implementation of IUnitOfWork for Entity Framework Core.
/// </summary>
public abstract class UnitOfWorkBase : IUnitOfWork
{
    private bool _disposed = false;

    /// <summary>
    /// Gets whether a transaction is currently active.
    /// </summary>
    public bool HasActiveTransaction { get; protected set; }

    /// <summary>
    /// Disposes the unit of work.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the unit of work asynchronously.
    /// </summary>
    /// <returns>A task representing the disposal.</returns>
    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore();
        Dispose(false);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Begins a new transaction.
    /// </summary>
    /// <returns>A task representing the transaction.</returns>
    public abstract Task BeginTransactionAsync();

    /// <summary>
    /// Commits the current transaction.
    /// </summary>
    /// <returns>A task representing the commit operation.</returns>
    public abstract Task CommitAsync();

    /// <summary>
    /// Rolls back the current transaction.
    /// </summary>
    /// <returns>A task representing the rollback operation.</returns>
    public abstract Task RollbackAsync();

    /// <summary>
    /// Executes a function within a transaction scope.
    /// </summary>
    /// <param name="action">The function to execute.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the execution.</returns>
    public async Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken cancellationToken = default)
    {
        if (HasActiveTransaction)
        {
            await action();
            return;
        }

        await BeginTransactionAsync();
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
    /// Executes a function within a transaction scope and returns a result.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="func">The function to execute.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the execution with the result.</returns>
    public async Task<TResult> ExecuteInTransactionAsync<TResult>(Func<Task<TResult>> func, CancellationToken cancellationToken = default)
    {
        if (HasActiveTransaction)
        {
            return await func();
        }

        await BeginTransactionAsync();
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
    /// Disposes resources.
    /// </summary>
    /// <param name="disposing">Whether to dispose managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Dispose managed resources
            }
            _disposed = true;
        }
    }

    /// <summary>
    /// Disposes async resources.
    /// </summary>
    /// <returns>A task representing the disposal.</returns>
    protected virtual async Task DisposeAsyncCore()
    {
        await Task.CompletedTask;
    }
}
