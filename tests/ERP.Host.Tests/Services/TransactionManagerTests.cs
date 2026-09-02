using ERP.Host.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ERP.Host.Tests.Services;

/// <summary>
/// Unit tests for TransactionManager.
/// </summary>
public class TransactionManagerTests : IDisposable
{
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly Mock<DbContext> _dbContextMock;
    private readonly Mock<IDbContextTransaction> _transactionMock;
    private readonly ILogger<TransactionManager> _logger;
    private readonly TransactionManager _transactionManager;

    public TransactionManagerTests()
    {
        _serviceProviderMock = new Mock<IServiceProvider>();
        _dbContextMock = new Mock<DbContext>();
        _transactionMock = new Mock<IDbContextTransaction>();
        _logger = NullLogger<TransactionManager>.Instance;
        
        _transactionManager = new TransactionManager(_serviceProviderMock.Object, _logger);
    }

    public void Dispose()
    {
        _serviceProviderMock?.Invoke();
        _dbContextMock?.Invoke();
        _transactionMock?.Invoke();
    }

    [Fact]
    public void HasActiveTransaction_ShouldReturnFalse_WhenNoTransactions()
    {
        // Act
        var hasActive = _transactionManager.HasActiveTransaction;

        // Assert
        Assert.False(hasActive);
    }

    [Fact]
    public void BeginTransaction_ShouldReturnTransactionScope()
    {
        // Act
        var scope = _transactionManager.BeginTransaction();

        // Assert
        Assert.NotNull(scope);
        Assert.IsType<TransactionScope>(scope);
        Assert.Same(_transactionManager, scope.TransactionManager);
    }

    [Fact]
    public async Task BeginTransactionAsync_ShouldAddTransactionToStack()
    {
        // Arrange
        _serviceProviderMock.Setup(x => x.GetService(typeof(DbContext))).Returns(_dbContextMock.Object);
        _dbContextMock.Setup(x => x.Database.BeginTransactionAsync(default)).ReturnsAsync(_transactionMock.Object);

        // Act
        await _transactionManager.BeginTransactionAsync<DbContext>();

        // Assert
        Assert.True(_transactionManager.HasActiveTransaction);
    }

    [Fact]
    public async Task CommitAsync_ShouldCommitAllTransactions()
    {
        // Arrange
        var transaction1 = new Mock<IDbContextTransaction>();
        var transaction2 = new Mock<IDbContextTransaction>();
        
        // Manually add transactions to the stack
        var activeTransactionsField = typeof(TransactionManager).GetField("_activeTransactions", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var stack = (Stack<IDbContextTransaction>)activeTransactionsField!.GetValue(_transactionManager)!;
        stack.Push(transaction2.Object);
        stack.Push(transaction1.Object);

        // Act
        await _transactionManager.CommitAsync();

        // Assert
        transaction1.Verify(x => x.CommitAsync(), Times.Once);
        transaction2.Verify(x => x.CommitAsync(), Times.Once);
        transaction1.Verify(x => x.Dispose(), Times.Once);
        transaction2.Verify(x => x.Dispose(), Times.Once);
    }

    [Fact]
    public async Task CommitAsync_ShouldRollbackFailedTransactions()
    {
        // Arrange
        var transaction1 = new Mock<IDbContextTransaction>();
        var transaction2 = new Mock<IDbContextTransaction>();
        
        transaction1.Setup(x => x.CommitAsync()).ThrowsAsync(new Exception("Commit failed"));
        
        var activeTransactionsField = typeof(TransactionManager).GetField("_activeTransactions", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var stack = (Stack<IDbContextTransaction>)activeTransactionsField!.GetValue(_transactionManager)!;
        stack.Push(transaction2.Object);
        stack.Push(transaction1.Object);

        // Act & Assert
        await Assert.ThrowsAsync<AggregateException>(() => _transactionManager.CommitAsync());
        
        transaction1.Verify(x => x.RollbackAsync(), Times.Once);
        transaction2.Verify(x => x.CommitAsync(), Times.Never);
    }

    [Fact]
    public async Task RollbackAsync_ShouldRollbackAllTransactions()
    {
        // Arrange
        var transaction1 = new Mock<IDbContextTransaction>();
        var transaction2 = new Mock<IDbContextTransaction>();
        
        var activeTransactionsField = typeof(TransactionManager).GetField("_activeTransactions", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var stack = (Stack<IDbContextTransaction>)activeTransactionsField!.GetValue(_transactionManager)!;
        stack.Push(transaction2.Object);
        stack.Push(transaction1.Object);

        // Act
        await _transactionManager.RollbackAsync();

        // Assert
        transaction1.Verify(x => x.RollbackAsync(), Times.Once);
        transaction2.Verify(x => x.RollbackAsync(), Times.Once);
        transaction1.Verify(x => x.Dispose(), Times.Once);
        transaction2.Verify(x => x.Dispose(), Times.Once);
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_ShouldCommitOnSuccess()
    {
        // Arrange
        var executed = false;
        
        _serviceProviderMock.Setup(x => x.GetService(typeof(DbContext))).Returns(_dbContextMock.Object);
        _dbContextMock.Setup(x => x.Database.BeginTransactionAsync(default)).ReturnsAsync(_transactionMock.Object);

        // Act
        await _transactionManager.ExecuteInTransactionAsync(async () =>
        {
            executed = true;
        });

        // Assert
        Assert.True(executed);
        _transactionMock.Verify(x => x.CommitAsync(), Times.Once);
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_ShouldRollbackOnException()
    {
        // Arrange
        _serviceProviderMock.Setup(x => x.GetService(typeof(DbContext))).Returns(_dbContextMock.Object);
        _dbContextMock.Setup(x => x.Database.BeginTransactionAsync(default)).ReturnsAsync(_transactionMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _transactionManager.ExecuteInTransactionAsync(async () =>
        {
            throw new Exception("Test exception");
        }));
        
        _transactionMock.Verify(x => x.RollbackAsync(), Times.Once);
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_WithResult_ShouldReturnResultOnSuccess()
    {
        // Arrange
        _serviceProviderMock.Setup(x => x.GetService(typeof(DbContext))).Returns(_dbContextMock.Object);
        _dbContextMock.Setup(x => x.Database.BeginTransactionAsync(default)).ReturnsAsync(_transactionMock.Object);

        // Act
        var result = await _transactionManager.ExecuteInTransactionAsync(async () =>
        {
            return "TestResult";
        });

        // Assert
        Assert.Equal("TestResult", result);
        _transactionMock.Verify(x => x.CommitAsync(), Times.Once);
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_WithResult_ShouldRollbackOnException()
    {
        // Arrange
        _serviceProviderMock.Setup(x => x.GetService(typeof(DbContext))).Returns(_dbContextMock.Object);
        _dbContextMock.Setup(x => x.Database.BeginTransactionAsync(default)).ReturnsAsync(_transactionMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _transactionManager.ExecuteInTransactionAsync(async () =>
        {
            throw new Exception("Test exception");
            return "TestResult";
        }));
        
        _transactionMock.Verify(x => x.RollbackAsync(), Times.Once);
    }

    [Fact]
    public async Task ExecuteWithCompensationAsync_ShouldExecuteCompensationOnFailure()
    {
        // Arrange
        var compensationExecuted = false;

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _transactionManager.ExecuteWithCompensationAsync(
            async () =>
            {
                throw new Exception("Main action failed");
            },
            async () =>
            {
                compensationExecuted = true;
            }));
        
        Assert.True(compensationExecuted);
    }

    [Fact]
    public async Task ExecuteWithCompensationAsync_ShouldNotExecuteCompensationOnSuccess()
    {
        // Arrange
        var compensationExecuted = false;

        // Act
        await _transactionManager.ExecuteWithCompensationAsync(
            async () =>
            {
                // Success
            },
            async () =>
            {
                compensationExecuted = true;
            });
        
        // Assert
        Assert.False(compensationExecuted);
    }

    [Fact]
    public async Task ExecuteWithCompensationAsync_ShouldLogErrorOnCompensationFailure()
    {
        // Arrange
        var compensationException = new Exception("Compensation failed");
        var loggerMock = new Mock<ILogger<TransactionManager>>();
        var managerWithMockLogger = new TransactionManager(_serviceProviderMock.Object, loggerMock.Object);

        // Act
        await Assert.ThrowsAsync<Exception>(() => managerWithMockLogger.ExecuteWithCompensationAsync(
            async () =>
            {
                throw new Exception("Main action failed");
            },
            async () =>
            {
                throw compensationException;
            }));
        
        // Assert
        loggerMock.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Compensating action also failed")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteTransactionalWorkflowAsync_ShouldExecuteAllActionsOnSuccess()
    {
        // Arrange
        var action1Executed = false;
        var action2Executed = false;
        var action3Executed = false;

        var actions = new List<(Func<Task> action, Func<Task> compensate)>
        {
            (async () => { action1Executed = true; await Task.CompletedTask; }, 
             async () => { }),
            (async () => { action2Executed = true; await Task.CompletedTask; },
             async () => { }),
            (async () => { action3Executed = true; await Task.CompletedTask; },
             async () => { })
        };

        // Act
        await _transactionManager.ExecuteTransactionalWorkflowAsync(actions);

        // Assert
        Assert.True(action1Executed);
        Assert.True(action2Executed);
        Assert.True(action3Executed);
    }

    [Fact]
    public async Task ExecuteTransactionalWorkflowAsync_ShouldExecuteCompensationInReverseOrderOnFailure()
    {
        // Arrange
        var compensationOrder = new List<int>();

        var actions = new List<(Func<Task> action, Func<Task> compensate)>
        {
            (async () => { await Task.CompletedTask; },
             async () => { compensationOrder.Add(1); await Task.CompletedTask; }),
            (async () => { await Task.CompletedTask; },
             async () => { compensationOrder.Add(2); await Task.CompletedTask; }),
            (async () => { throw new Exception("Action 3 failed"); },
             async () => { compensationOrder.Add(3); await Task.CompletedTask; })
        };

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _transactionManager.ExecuteTransactionalWorkflowAsync(actions));
        
        // Compensation should be executed in reverse order: 3, 2, 1
        // But since action 3 failed, only actions 1 and 2 were executed, so compensation 2, 1
        Assert.Equal(new[] { 2, 1 }, compensationOrder);
    }

    [Fact]
    public async Task ExecuteTransactionalWorkflowAsync_ShouldContinueCompensationOnCompensationFailure()
    {
        // Arrange
        var compensationOrder = new List<int>();

        var actions = new List<(Func<Task> action, Func<Task> compensate)>
        {
            (async () => { await Task.CompletedTask; },
             async () => { compensationOrder.Add(1); await Task.CompletedTask; }),
            (async () => { throw new Exception("Action 2 failed"); },
             async () => { compensationOrder.Add(2); throw new Exception("Compensation 2 failed"); }),
            (async () => { await Task.CompletedTask; },
             async () => { compensationOrder.Add(3); await Task.CompletedTask; })
        };

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _transactionManager.ExecuteTransactionalWorkflowAsync(actions));
        
        // All compensations should be attempted
        Assert.Equal(new[] { 2, 1 }, compensationOrder);
    }
}

/// <summary>
/// Unit tests for TransactionScope.
/// </summary>
public class TransactionScopeTests : IDisposable
{
    private readonly Mock<ITransactionManager> _transactionManagerMock;
    private readonly TransactionScope _scope;

    public TransactionScopeTests()
    {
        _transactionManagerMock = new Mock<ITransactionManager>();
        _scope = new TransactionScope(_transactionManagerMock.Object);
    }

    public void Dispose()
    {
        _transactionManagerMock?.Invoke();
        _scope.Dispose();
    }

    [Fact]
    public void TransactionManager_ShouldReturnConfiguredManager()
    {
        // Act
        var manager = _scope.TransactionManager;

        // Assert
        Assert.Same(_transactionManagerMock.Object, manager);
    }

    [Fact]
    public async Task CommitAsync_ShouldCommitTransactionManager()
    {
        // Act
        await _scope.CommitAsync();

        // Assert
        _transactionManagerMock.Verify(x => x.CommitAsync(), Times.Once);
    }

    [Fact]
    public async Task CommitAsync_ShouldNotCommitTwice()
    {
        // Act
        await _scope.CommitAsync();
        await _scope.CommitAsync();

        // Assert
        _transactionManagerMock.Verify(x => x.CommitAsync(), Times.Once);
    }

    [Fact]
    public async Task RollbackAsync_ShouldRollbackTransactionManager()
    {
        // Act
        await _scope.RollbackAsync();

        // Assert
        _transactionManagerMock.Verify(x => x.RollbackAsync(), Times.Once);
    }

    [Fact]
    public async Task RollbackAsync_ShouldNotRollbackAfterCommit()
    {
        // Act
        await _scope.CommitAsync();
        await _scope.RollbackAsync();

        // Assert
        _transactionManagerMock.Verify(x => x.RollbackAsync(), Times.Never);
    }

    [Fact]
    public void Dispose_ShouldRollbackIfNotCommitted()
    {
        // Act
        _scope.Dispose();

        // Assert
        _transactionManagerMock.Verify(x => x.RollbackAsync(), Times.Once);
    }

    [Fact]
    public async Task DisposeAsync_ShouldRollbackIfNotCommitted()
    {
        // Act
        await _scope.DisposeAsync();

        // Assert
        _transactionManagerMock.Verify(x => x.RollbackAsync(), Times.Once);
    }

    [Fact]
    public void Dispose_ShouldNotRollbackIfCommitted()
    {
        // Arrange
        _scope.CommitAsync().GetAwaiter().GetResult();

        // Act
        _scope.Dispose();

        // Assert
        _transactionManagerMock.Verify(x => x.RollbackAsync(), Times.Never);
    }

    [Fact]
    public void Dispose_ShouldBeIdempotent()
    {
        // Act
        _scope.Dispose();
        _scope.Dispose();

        // Assert
        _transactionManagerMock.Verify(x => x.RollbackAsync(), Times.Once);
    }

    [Fact]
    public async Task DisposeAsync_ShouldBeIdempotent()
    {
        // Act
        await _scope.DisposeAsync();
        await _scope.DisposeAsync();

        // Assert
        _transactionManagerMock.Verify(x => x.RollbackAsync(), Times.Once);
    }
}
