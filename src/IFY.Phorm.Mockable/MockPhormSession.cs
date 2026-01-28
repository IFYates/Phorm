using IFY.Phorm.Data;
using IFY.Phorm.EventArgs;
using IFY.Phorm.Execution;

namespace IFY.Phorm.Mockable;

/// <summary>
/// A functional <see cref="IPhormSession"/> implementation that makes it easier to mock out
/// Pho/rm logic for testing using the <see cref="IPhormSessionMock"/> interface.
/// </summary>
public class MockPhormSession(IPhormSessionMock mockObject) : IPhormSession, ITransactedPhormSession
{
    private readonly IPhormSessionMock _mockObject = mockObject;

    // TODO: Some way to make the invocation do all contract way in and out, to test transformations, etc.

    /// <inheritdoc/>
    public string? ConnectionName { get; set; }
    /// <inheritdoc/>
    public bool ExceptionsAsConsoleMessage { get; set; }
    /// <inheritdoc/>
    public bool StrictResultSize { get; set; }

    /// <inheritdoc/>
    public bool SupportsTransactions => true;
    /// <inheritdoc/>
    public bool IsInTransaction => TransactionId != null;
    /// <summary>
    /// A random ID for a transaction, to make it easy to identify if we're in the same transaction on multiple requests.
    /// </summary>
    public int? TransactionId { get; private set; }

    /// <inheritdoc/>
    public string ProcedurePrefix { get; set; } = GlobalSettings.ProcedurePrefix;
    /// <inheritdoc/>
    public string TablePrefix { get; set; } = GlobalSettings.TablePrefix;
    /// <inheritdoc/>
    public string ViewPrefix { get; set; } = GlobalSettings.ViewPrefix;

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, object?> ContextData { get; set; } = new Dictionary<string, object?>();

    /// <inheritdoc/>
    public event EventHandler<ConnectedEventArgs> Connected = null!;
    /// <inheritdoc/>
    public event EventHandler<CommandExecutingEventArgs> CommandExecuting = null!;
    /// <inheritdoc/>
    public event EventHandler<CommandExecutedEventArgs> CommandExecuted = null!;
    /// <inheritdoc/>
    public event EventHandler<UnexpectedRecordColumnEventArgs> UnexpectedRecordColumn = null!;
    /// <inheritdoc/>
    public event EventHandler<UnresolvedContractMemberEventArgs> UnresolvedContractMember = null!;
    /// <inheritdoc/>
    public event EventHandler<ConsoleMessageEventArgs> ConsoleMessage = null!;

    internal CallContext GetCallContext(string? schemaName, string objectName, DbObjectType objectType, bool readOnly)
    {
        // TODO: contract could be renamed
        objectName = (objectType switch
        {
            DbObjectType.StoredProcedure => ProcedurePrefix,
            DbObjectType.Table => TablePrefix,
            DbObjectType.View => ViewPrefix,
            _ => throw new NotImplementedException(),
        }) + objectName;

        return new CallContext(ConnectionName, ContextData, schemaName, objectName, objectType, TransactionId, readOnly);
    }

    /// <inheritdoc/>
    public IPhormSession SetConnectionName(string connectionName)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public Task<int> CallAsync(string contractName, object? args, CancellationToken cancellationToken)
    {
        var ctxt = GetCallContext(null, contractName, DbObjectType.StoredProcedure, false);
        return Task.FromResult(_mockObject.Call(contractName, args, cancellationToken, ctxt));
    }
    /// <inheritdoc/>
    public Task<int> CallAsync<TActionContract>(object? args, CancellationToken cancellationToken)
        where TActionContract : IPhormContract
    {
        var (schemaName, contractName, objectType, readOnly) = PhormContractRunner<IPhormContract>.ResolveContractName(typeof(TActionContract));
        var ctxt = GetCallContext(schemaName, contractName, objectType, readOnly);
        return Task.FromResult(_mockObject.Call<TActionContract>(args, cancellationToken, ctxt));
    }

    /// <inheritdoc/>
    public IPhormContractRunner From(string contractName, object? args)
    {
        return new MockPhormContractRunner<IPhormContract>(this, typeof(IPhormContract), _mockObject, contractName, DbObjectType.Default, args);
    }
    /// <inheritdoc/>
    public IPhormContractRunner<TActionContract> From<TActionContract>(object? args)
        where TActionContract : IPhormContract
    {
        return new MockPhormContractRunner<TActionContract>(this, typeof(TActionContract), _mockObject, null, DbObjectType.Default, args);
    }

    /// <inheritdoc/>
    public Task<TResult?> GetAsync<TResult>(object? args, CancellationToken cancellationToken)
        where TResult : class
    {
        return new MockPhormContractRunner<IPhormContract>(this, typeof(TResult), _mockObject, null, DbObjectType.View, args)
            .GetAsync<TResult>(cancellationToken);
    }

    /// <inheritdoc/>
    public IPhormSession WithContext(string? connectionName, IDictionary<string, object?> contextData)
    {
        return new MockPhormSession(_mockObject)
        {
            ConnectionName = connectionName,
            ContextData = contextData as Dictionary<string, object?> ?? new Dictionary<string, object?>(contextData),
            TransactionId = TransactionId
        };
    }

    /// <inheritdoc/>
    public async Task<ITransactedPhormSession> BeginTransactionAsync(CancellationToken cancellationToken)
    {
        return new MockPhormSession(_mockObject)
        {
            ConnectionName = ConnectionName,
            ContextData = ContextData,
            TransactionId = new Random().Next()
        };
    }

    private bool _disposed = false;
    /// <inheritdoc/>
    public void Commit()
    {
        if (!IsInTransaction || _disposed)
        {
            throw new InvalidOperationException("No active transaction to commit.");
        }
        _disposed = true;
        _mockObject.Commit();
    }

    /// <inheritdoc/>
    public void Rollback()
    {
        if (!IsInTransaction || _disposed)
        {
            throw new InvalidOperationException("No active transaction to roll back.");
        }
        _disposed = true;
        _mockObject.Rollback();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (IsInTransaction && !_disposed)
        {
            Rollback();
        }
    }
}
