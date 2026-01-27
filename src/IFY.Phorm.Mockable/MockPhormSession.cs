using IFY.Phorm.Data;
using IFY.Phorm.EventArgs;
using IFY.Phorm.Execution;

namespace IFY.Phorm.Mockable;

/// <summary>
/// A functional <see cref="IPhormSession"/> implementation that makes it easier to mock out Pho/rm logic for testing.
/// </summary>
public class MockPhormSession : IPhormSession
{
    private readonly IPhormSessionMock _mockObject;

    // TODO: Some way to make the invocation do all contract way in and out, to test transformations, etc.

    public string? ConnectionName { get; set; }
    public bool ExceptionsAsConsoleMessage { get; set; }
    public bool StrictResultSize { get; set; }

    public bool SupportsTransactions => true;
    public bool IsInTransaction => false;

    public string ProcedurePrefix { get; set; } = GlobalSettings.ProcedurePrefix;
    public string TablePrefix { get; set; } = GlobalSettings.TablePrefix;
    public string ViewPrefix { get; set; } = GlobalSettings.ViewPrefix;

    public IDictionary<string, object?> ContextData => throw new NotImplementedException();

    public event EventHandler<ConnectedEventArgs> Connected = null!;
    public event EventHandler<CommandExecutingEventArgs> CommandExecuting = null!;
    public event EventHandler<CommandExecutedEventArgs> CommandExecuted = null!;
    public event EventHandler<UnexpectedRecordColumnEventArgs> UnexpectedRecordColumn = null!;
    public event EventHandler<UnresolvedContractMemberEventArgs> UnresolvedContractMember = null!;
    public event EventHandler<ConsoleMessageEventArgs> ConsoleMessage = null!;

    public MockPhormSession(IPhormSessionMock mockObject)
    {
        _mockObject = mockObject;
    }

    internal CallContext GetCallContext(string? schemaName, string objectName, DbObjectType objectType, bool readOnly)
    {
        objectName = (objectType switch
        {
            DbObjectType.StoredProcedure => ProcedurePrefix,
            DbObjectType.Table => TablePrefix,
            DbObjectType.View => ViewPrefix,
            _ => throw new NotImplementedException(),
        }) + objectName;

        return new CallContext(ConnectionName, schemaName, objectName, objectType, readOnly);
    }

    public ITransactedPhormSession BeginTransaction()
    {
        throw new NotImplementedException();
    }

    public IPhormSession SetConnectionName(string connectionName)
    {
        throw new NotImplementedException();
    }

    public int Call(string contractName)
    {
        var ctxt = GetCallContext(null, contractName, DbObjectType.StoredProcedure, false);
        return _mockObject.Call(contractName, null, ctxt);
    }
    public int Call(string contractName, object? args)
    {
        var ctxt = GetCallContext(null, contractName, DbObjectType.StoredProcedure, false);
        return _mockObject.Call(contractName, args, ctxt);
    }
    public int Call<TActionContract>()
        where TActionContract : IPhormContract
    {
        var (schemaName, contractName, objectType, readOnly) = PhormContractRunner<IPhormContract>.ResolveContractName(typeof(TActionContract));
        var ctxt = GetCallContext(schemaName, contractName, objectType, readOnly);
        return _mockObject.Call<TActionContract>(null, ctxt);
    }
    public int Call<TActionContract>(object? args)
        where TActionContract : IPhormContract
    {
        var (schemaName, contractName, objectType, readOnly) = PhormContractRunner<IPhormContract>.ResolveContractName(typeof(TActionContract));
        var ctxt = GetCallContext(schemaName, contractName, objectType, readOnly);
        return _mockObject.Call<TActionContract>(args, ctxt);
    }
    public int Call<TActionContract>(TActionContract contract)
        where TActionContract : IPhormContract
    {
        var (schemaName, contractName, objectType, readOnly) = PhormContractRunner<IPhormContract>.ResolveContractName(typeof(TActionContract));
        var ctxt = GetCallContext(schemaName, contractName, objectType, readOnly);
        return _mockObject.Call<TActionContract>(contract, ctxt);
    }
    public Task<int> CallAsync(string contractName)
    {
        var ctxt = GetCallContext(null, contractName, DbObjectType.StoredProcedure, false);
        return Task.FromResult(_mockObject.Call(contractName, null, ctxt));
    }
    public Task<int> CallAsync(string contractName, object? args)
    {
        var ctxt = GetCallContext(null, contractName, DbObjectType.StoredProcedure, false);
        return Task.FromResult(_mockObject.Call(contractName, args, ctxt));
    }
    public Task<int> CallAsync(string contractName, CancellationToken cancellationToken)
    {
        var ctxt = GetCallContext(null, contractName, DbObjectType.StoredProcedure, false);
        return Task.FromResult(_mockObject.Call(contractName, null, ctxt));
    }
    public Task<int> CallAsync(string contractName, object? args, CancellationToken cancellationToken)
    {
        var ctxt = GetCallContext(null, contractName, DbObjectType.StoredProcedure, false);
        return Task.FromResult(_mockObject.Call(contractName, args, ctxt));
    }
    public Task<int> CallAsync<TActionContract>()
        where TActionContract : IPhormContract
    {
        var (schemaName, contractName, objectType, readOnly) = PhormContractRunner<IPhormContract>.ResolveContractName(typeof(TActionContract));
        var ctxt = GetCallContext(schemaName, contractName, objectType, readOnly);
        return Task.FromResult(_mockObject.Call<TActionContract>(null, ctxt));
    }
    public Task<int> CallAsync<TActionContract>(object? args)
        where TActionContract : IPhormContract
    {
        var (schemaName, contractName, objectType, readOnly) = PhormContractRunner<IPhormContract>.ResolveContractName(typeof(TActionContract));
        var ctxt = GetCallContext(schemaName, contractName, objectType, readOnly);
        return Task.FromResult(_mockObject.Call<TActionContract>(args, ctxt));
    }
    public Task<int> CallAsync<TActionContract>(CancellationToken cancellationToken)
        where TActionContract : IPhormContract
    {
        var (schemaName, contractName, objectType, readOnly) = PhormContractRunner<IPhormContract>.ResolveContractName(typeof(TActionContract));
        var ctxt = GetCallContext(schemaName, contractName, objectType, readOnly);
        return Task.FromResult(_mockObject.Call<TActionContract>(null, ctxt));
    }
    public Task<int> CallAsync<TActionContract>(object? args, CancellationToken cancellationToken)
        where TActionContract : IPhormContract
    {
        var (schemaName, contractName, objectType, readOnly) = PhormContractRunner<IPhormContract>.ResolveContractName(typeof(TActionContract));
        var ctxt = GetCallContext(schemaName, contractName, objectType, readOnly);
        return Task.FromResult(_mockObject.Call<TActionContract>(args, ctxt));
    }
    public Task<int> CallAsync<TActionContract>(TActionContract contract)
        where TActionContract : IPhormContract
    {
        var (schemaName, contractName, objectType, readOnly) = PhormContractRunner<IPhormContract>.ResolveContractName(typeof(TActionContract));
        var ctxt = GetCallContext(schemaName, contractName, objectType, readOnly);
        return Task.FromResult(_mockObject.Call<TActionContract>(contract, ctxt));
    }
    public Task<int> CallAsync<TActionContract>(TActionContract contract, CancellationToken cancellationToken)
        where TActionContract : IPhormContract
    {
        var (schemaName, contractName, objectType, readOnly) = PhormContractRunner<IPhormContract>.ResolveContractName(typeof(TActionContract));
        var ctxt = GetCallContext(schemaName, contractName, objectType, readOnly);
        return Task.FromResult(_mockObject.Call<TActionContract>(contract, ctxt));
    }

    public IPhormContractRunner From(string contractName)
    {
        return new MockPhormContractRunner<IPhormContract>(this, _mockObject, contractName, null);
    }
    public IPhormContractRunner From(string contractName, object? args)
    {
        return new MockPhormContractRunner<IPhormContract>(this, _mockObject, contractName, args);
    }
    public IPhormContractRunner<TActionContract> From<TActionContract>()
        where TActionContract : IPhormContract
    {
        return new MockPhormContractRunner<TActionContract>(this, _mockObject, null, null);
    }
    public IPhormContractRunner<TActionContract> From<TActionContract>(object? args)
        where TActionContract : IPhormContract
    {
        return new MockPhormContractRunner<TActionContract>(this, _mockObject, null, args);
    }
    public IPhormContractRunner<TActionContract> From<TActionContract>(TActionContract args)
        where TActionContract : IPhormContract
    {
        return new MockPhormContractRunner<TActionContract>(this, _mockObject, null, args);
    }

    public Task<TResult?> GetAsync<TResult>()
        where TResult : class
    {
        return new MockPhormContractRunner<IPhormContract>(this, _mockObject, null, null)
            .GetAsync<TResult>();
    }
    public Task<TResult?> GetAsync<TResult>(object? args)
        where TResult : class
    {
        return new MockPhormContractRunner<IPhormContract>(this, _mockObject, null, args)
            .GetAsync<TResult>();
    }
    public Task<TResult?> GetAsync<TResult>(CancellationToken cancellationToken)
        where TResult : class
    {
        return new MockPhormContractRunner<IPhormContract>(this, _mockObject, null, null)
            .GetAsync<TResult>(cancellationToken);
    }
    public Task<TResult?> GetAsync<TResult>(object? args, CancellationToken cancellationToken)
        where TResult : class
    {
        return new MockPhormContractRunner<IPhormContract>(this, _mockObject, null, args)
            .GetAsync<TResult>(cancellationToken);
    }
    public Task<TResult?> GetAsync<TResult>(TResult args)
        where TResult : class
    {
        return new MockPhormContractRunner<IPhormContract>(this, _mockObject, null, args)
            .GetAsync<TResult>();
    }
    public Task<TResult?> GetAsync<TResult>(TResult args, CancellationToken cancellationToken)
        where TResult : class
    {
        return new MockPhormContractRunner<IPhormContract>(this, _mockObject, null, args)
            .GetAsync<TResult>(cancellationToken);
    }

    public IPhormSession WithContext(string? connectionName, IDictionary<string, object?> contextData)
    {
        throw new NotImplementedException();
    }

    public Task<ITransactedPhormSession> BeginTransactionAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
