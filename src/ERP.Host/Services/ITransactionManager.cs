using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ERP.SharedKernel.Contracts;
using Microsoft.EntityFrameworkCore;

namespace ERP.Host.Services;

/// <summary>
/// Interface for managing distributed transactions across multiple plugin databases.
/// </summary>
public interface ITransactionManager : IUnitOfWork
{
    /// <summary>
    /// Gets whether there is an active transaction.
    /// </summary>
    bool HasActiveTransaction { get; }

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
    /// Implements the Saga pattern for distributed transactions.
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
    /// If any action fails, all previous actions are compensated.
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
