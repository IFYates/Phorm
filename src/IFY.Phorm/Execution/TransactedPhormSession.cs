using IFY.Phorm.Connectivity;
using IFY.Phorm.Data;
using IFY.Phorm.EventArgs;
using System.Data;
using System.Diagnostics.CodeAnalysis;

namespace IFY.Phorm.Execution;

/// <summary>
/// A wrapper for an <see cref="AbstractPhormSession"/> instance, adding standard transaction-handling logic.
/// </summary>
public partial class TransactedPhormSession : ITransactedPhormSession
{
    private bool _isDisposed;

    private readonly AbstractPhormSession _baseSession;
    private readonly IDbTransaction _transaction;

    /// <inheritdoc/>
    public bool IsInTransaction => true;

    internal TransactedPhormSession(AbstractPhormSession baseSession, IDbTransaction transaction)
    {
        _baseSession = baseSession;
        _transaction = transaction;
    }

    /// <inheritdoc/>
    public void Commit()
    {
        _transaction.Commit();
    }

    /// <inheritdoc/>
    public void Rollback()
    {
        _transaction.Rollback();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (!_isDisposed)
        {
            _transaction.Dispose();
            _isDisposed = true;
        }
        GC.SuppressFinalize(this);
    }

    #region Call

    /// <inheritdoc/>
    public Task<int> CallAsync(string contractName, object? args, CancellationToken cancellationToken)
    {
        var runner = new PhormContractRunner<IPhormContract>(_baseSession, contractName, DbObjectType.StoredProcedure, args, _transaction);
        return runner.CallAsync(cancellationToken);
    }
    /// <inheritdoc/>
    public Task<int> CallAsync<TActionContract>(object? args, CancellationToken cancellationToken)
        where TActionContract : IPhormContract
    {
        var runner = new PhormContractRunner<TActionContract>(_baseSession, null, DbObjectType.StoredProcedure, args, _transaction);
        return runner.CallAsync(cancellationToken);
    }

    #endregion Call

    #region From

    /// <inheritdoc/>
    public IPhormContractRunner From(string contractName, object? args)
    {
        return new PhormContractRunner<IPhormContract>(_baseSession, contractName, DbObjectType.StoredProcedure, args, _transaction);
    }

    /// <inheritdoc/>
    public IPhormContractRunner<TActionContract> From<TActionContract>(object? args)
        where TActionContract : IPhormContract
    {
        return new PhormContractRunner<TActionContract>(_baseSession, null, DbObjectType.StoredProcedure, args, _transaction);
    }

    #endregion From

    #region Get

    /// <inheritdoc/>
    public TResult? Get<TResult>(object? args)
        where TResult : class
    {
        var runner = new PhormContractRunner<IPhormContract>(_baseSession, typeof(TResult), null, DbObjectType.View, args, _transaction);
        return runner.Get<TResult>();
    }

    /// <inheritdoc/>
    public Task<TResult?> GetAsync<TResult>(object? args, CancellationToken cancellationToken)
        where TResult : class
    {
        var runner = new PhormContractRunner<IPhormContract>(_baseSession, typeof(TResult), null, DbObjectType.View, args, _transaction);
        return runner.GetAsync<TResult>(cancellationToken);
    }

    #endregion Get
}

///--- Purely wrapped members below here
[ExcludeFromCodeCoverage]
partial class TransactedPhormSession
{
    /// <inheritdoc/>
    public event EventHandler<ConnectedEventArgs> Connected { add => _baseSession.Connected += value; remove => _baseSession.Connected -= value; }
    /// <inheritdoc/>
    public event EventHandler<CommandExecutingEventArgs> CommandExecuting { add => _baseSession.CommandExecuting += value; remove => _baseSession.CommandExecuting -= value; }
    /// <inheritdoc/>
    public event EventHandler<CommandExecutedEventArgs> CommandExecuted { add => _baseSession.CommandExecuted += value; remove => _baseSession.CommandExecuted -= value; }
    /// <inheritdoc/>
    public event EventHandler<UnexpectedRecordColumnEventArgs> UnexpectedRecordColumn { add => _baseSession.UnexpectedRecordColumn += value; remove => _baseSession.UnexpectedRecordColumn -= value; }
    /// <inheritdoc/>
    public event EventHandler<UnresolvedContractMemberEventArgs> UnresolvedContractMember { add => _baseSession.UnresolvedContractMember += value; remove => _baseSession.UnresolvedContractMember -= value; }
    /// <inheritdoc/>
    public event EventHandler<ConsoleMessageEventArgs> ConsoleMessage { add => _baseSession.ConsoleMessage += value; remove => _baseSession.ConsoleMessage -= value; }

    /// <inheritdoc/>
    public string? ConnectionName => _baseSession.ConnectionName;

    /// <inheritdoc/>
    public bool ExceptionsAsConsoleMessage { get => _baseSession.ExceptionsAsConsoleMessage; set => _baseSession.ExceptionsAsConsoleMessage = value; }
    /// <inheritdoc/>
    public bool StrictResultSize { get => _baseSession.StrictResultSize; set => _baseSession.StrictResultSize = value; }

    /// <inheritdoc/>
    public bool SupportsTransactions => _baseSession.SupportsTransactions;

    /// <inheritdoc/>
    protected internal IPhormDbConnection GetConnection()
        => _baseSession.GetConnection();

    /// <inheritdoc/>
    public IPhormSession SetConnectionName(string connectionName)
        => _baseSession.SetConnectionName(connectionName);

    /// <inheritdoc/>
    public ITransactedPhormSession BeginTransaction()
        => _baseSession.BeginTransaction();

    /// <inheritdoc/>
    public int Call(string contractName, object? args)
        => CallAsync(contractName, args, CancellationToken.None).GetAwaiter().GetResult();

    /// <inheritdoc/>
    public int Call<TActionContract>(object? args)
        where TActionContract : IPhormContract
        => CallAsync<TActionContract>(args, CancellationToken.None).GetAwaiter().GetResult();
}
