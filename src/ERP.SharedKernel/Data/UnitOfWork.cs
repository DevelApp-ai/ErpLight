using System;
using System.Threading;
using System.Threading.Tasks;
using ERP.SharedKernel.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace ERP.SharedKernel.Data;

/// <summary>
/// Unit of work implementation for Entity Framework Core.
/// </summary>
public class UnitOfWork : UnitOfWorkBase
{
    private readonly DbContext _dbContext;
    private IDbContextTransaction? _currentTransaction;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnitOfWork"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    public UnitOfWork(DbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    /// <summary>
    /// Gets the database context.
    /// </summary>
    public DbContext DbContext => _dbContext;

    /// <summary>
    /// Begins a new transaction.
    /// </summary>
    /// <returns>A task representing the transaction.</returns>
    public override async Task BeginTransactionAsync()
    {
        if (_currentTransaction != null)
        {
            return;
        }

        _currentTransaction = await _dbContext.Database.BeginTransactionAsync();
        HasActiveTransaction = true;
    }

    /// <summary>
    /// Commits the current transaction.
    /// </summary>
    /// <returns>A task representing the commit operation.</returns>
    public override async Task CommitAsync()
    {
        if (_currentTransaction == null)
        {
            return;
        }

        try
        {
            await _dbContext.SaveChangesAsync();
            await _currentTransaction.CommitAsync();
        }
        finally
        {
            _currentTransaction.Dispose();
            _currentTransaction = null;
            HasActiveTransaction = false;
        }
    }

    /// <summary>
    /// Rolls back the current transaction.
    /// </summary>
    /// <returns>A task representing the rollback operation.</returns>
    public override async Task RollbackAsync()
    {
        if (_currentTransaction == null)
        {
            return;
        }

        try
        {
            await _currentTransaction.RollbackAsync();
        }
        finally
        {
            _currentTransaction.Dispose();
            _currentTransaction = null;
            HasActiveTransaction = false;
        }
    }

    /// <summary>
    /// Disposes resources.
    /// </summary>
    /// <param name="disposing">Whether to dispose managed resources.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && _currentTransaction != null)
        {
            _currentTransaction.Dispose();
            _currentTransaction = null;
            HasActiveTransaction = false;
        }
        base.Dispose(disposing);
    }

    /// <summary>
    /// Disposes async resources.
    /// </summary>
    /// <returns>A task representing the disposal.</returns>
    protected override async Task DisposeAsyncCore()
    {
        if (_currentTransaction != null)
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
            HasActiveTransaction = false;
        }
        await base.DisposeAsyncCore();
    }
}

/// <summary>
/// Factory for creating unit of work instances.
/// </summary>
public interface IUnitOfWorkFactory
{
    /// <summary>
    /// Creates a new unit of work for the specified DbContext type.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    /// <returns>A unit of work instance.</returns>
    IUnitOfWork Create<TContext>() where TContext : DbContext;
}

/// <summary>
/// Implementation of IUnitOfWorkFactory.
/// </summary>
public class UnitOfWorkFactory : IUnitOfWorkFactory
{
    private readonly IServiceProvider _serviceProvider;

    public UnitOfWorkFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IUnitOfWork Create<TContext>() where TContext : DbContext
    {
        var dbContext = _serviceProvider.GetRequiredService<TContext>();
        return new UnitOfWork(dbContext);
    }
}
