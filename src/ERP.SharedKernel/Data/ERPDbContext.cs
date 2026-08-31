using Microsoft.EntityFrameworkCore;

namespace ERP.SharedKernel.Data;

/// <summary>
/// Base DbContext for ERP plugin modules.
/// Each plugin can inherit from this to add its own entities.
/// </summary>
public class ERPDbContext : DbContext
{
    public ERPDbContext(DbContextOptions<ERPDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply configurations from all assemblies
        modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);
    }
}

/// <summary>
/// Interface for plugin database contexts.
/// </summary>
public interface IPluginDbContext
{
    /// <summary>
    /// Gets the DbContext.
    /// </summary>
    DbContext DbContext { get; }

    /// <summary>
    /// Saves changes to the database.
    /// </summary>
    /// <returns>A task representing the save operation.</returns>
    Task<int> SaveChangesAsync();

    /// <summary>
    /// Begins a transaction.
    /// </summary>
    /// <returns>A database transaction.</returns>
    Task<IDbContextTransaction> BeginTransactionAsync();
}

/// <summary>
/// Base implementation of IPluginDbContext.
/// </summary>
/// <typeparam name="TContext">The DbContext type.</typeparam>
public class PluginDbContext<TContext> : IPluginDbContext where TContext : DbContext
{
    private readonly TContext _dbContext;

    public PluginDbContext(TContext dbContext)
    {
        _dbContext = dbContext;
    }

    public DbContext DbContext => _dbContext;

    public Task<int> SaveChangesAsync()
    {
        return _dbContext.SaveChangesAsync();
    }

    public Task<IDbContextTransaction> BeginTransactionAsync()
    {
        return _dbContext.Database.BeginTransactionAsync();
    }
}
