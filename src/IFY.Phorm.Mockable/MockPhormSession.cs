using IFY.Phorm.Data;
using IFY.Phorm.EventArgs;
using IFY.Phorm.Execution;

namespace IFY.Phorm.Mockable;

/// <summary>
/// 
/// </summary>
public interface IPhormSessionMock
{
    int Call(string contractName, object? args, CallContext context);
    int Call<TActionContract>(object? args, CallContext context);
    TResult? GetFrom<TResult>(string? contractName, object? args, CallContext context);
    TResult? GetFrom<TActionContract, TResult>(object? args, CallContext context)
        where TActionContract : IPhormContract;
}

public class CallContext
{
    public string? ConnectionName { get; set; }
    public string? TargetSchema { get; set; }
    public string? TargetObject { get; set; }
    public DbObjectType? ObjectType { get; }

    public CallContext() { }
    public CallContext(string? connectionName, string? targetSchema, string? targetObject, DbObjectType objectType)
    {
        ConnectionName = connectionName;
        TargetSchema = targetSchema;
        TargetObject = targetObject;
        ObjectType = objectType;
    }
}

public class MockPhormSession : IPhormSession
{
    private readonly IPhormSessionMock _mockObject;

    // Call
    // - name, contract, anon name
    // - null, contract, anon args
    //
    // From<T>
    // - null, contract, anon args
    // - object comparer for both
    // 
    // don't treat Async/non different (opt. strict?)
    //
    // Easy check for target Table / View / SProc
    //
    // Default is to provide verbatim result, but way to provide "raw" data that will be processed like from datasource (i.e., through attributes)
    //

    public string? ConnectionName { get; set; }
    public bool ExceptionsAsConsoleMessage { get; set; }
    public bool StrictResultSize { get; set; }

    public bool SupportsTransactions => true;
    public bool IsInTransaction => false;

    public string ProcedurePrefix { get; set; } = GlobalSettings.ProcedurePrefix;
    public string TablePrefix { get; set; } = GlobalSettings.TablePrefix;
    public string ViewPrefix { get; set; } = GlobalSettings.ViewPrefix;

    public event EventHandler<ConnectedEventArgs> Connected;
    public event EventHandler<CommandExecutingEventArgs> CommandExecuting;
    public event EventHandler<CommandExecutedEventArgs> CommandExecuted;
    public event EventHandler<UnexpectedRecordColumnEventArgs> UnexpectedRecordColumn;
    public event EventHandler<UnresolvedContractMemberEventArgs> UnresolvedContractMember;
    public event EventHandler<ConsoleMessageEventArgs> ConsoleMessage;

    public MockPhormSession(IPhormSessionMock mockObject)
    {
        _mockObject = mockObject;
    }

    private CallContext getCallContext(string? schemaName, string objectName, DbObjectType objectType)
    {
        objectName = (objectType switch
        {
            DbObjectType.StoredProcedure => ProcedurePrefix,
            DbObjectType.Table => TablePrefix,
            DbObjectType.View => ViewPrefix,
            _ => throw new NotImplementedException(),
        }) + objectName;

        return new CallContext(ConnectionName, schemaName, objectName, objectType);
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
        var ctxt = getCallContext(null, contractName, DbObjectType.StoredProcedure);
        return _mockObject.Call(contractName, null, ctxt);
    }
    public int Call(string contractName, object? args)
    {
        var ctxt = getCallContext(null, contractName, DbObjectType.StoredProcedure);
        return _mockObject.Call(contractName, args, ctxt);
    }
    public int Call<TActionContract>()
        where TActionContract : IPhormContract
    {
        var (schemaName, contractName, objectType) = PhormContractRunner<IPhormContract>.ResolveContractName(typeof(TActionContract));
        var ctxt = getCallContext(schemaName, contractName, objectType);
        return _mockObject.Call<TActionContract>(null, ctxt);
    }
    public int Call<TActionContract>(object? args)
        where TActionContract : IPhormContract
    {
        var (schemaName, contractName, objectType) = PhormContractRunner<IPhormContract>.ResolveContractName(typeof(TActionContract));
        var ctxt = getCallContext(schemaName, contractName, objectType);
        return _mockObject.Call<TActionContract>(args, ctxt);
    }
    public int Call<TActionContract>(TActionContract contract)
        where TActionContract : IPhormContract
    {
        var (schemaName, contractName, objectType) = PhormContractRunner<IPhormContract>.ResolveContractName(typeof(TActionContract));
        var ctxt = getCallContext(schemaName, contractName, objectType);
        return _mockObject.Call<TActionContract>(contract, ctxt);
    }
    public Task<int> CallAsync(string contractName)
    {
        var ctxt = getCallContext(null, contractName, DbObjectType.StoredProcedure);
        return Task.FromResult(_mockObject.Call(contractName, null, ctxt));
    }
    public Task<int> CallAsync(string contractName, object? args)
    {
        var ctxt = getCallContext(null, contractName, DbObjectType.StoredProcedure);
        return Task.FromResult(_mockObject.Call(contractName, args, ctxt));
    }
    public Task<int> CallAsync(string contractName, CancellationToken cancellationToken)
    {
        var ctxt = getCallContext(null, contractName, DbObjectType.StoredProcedure);
        return Task.FromResult(_mockObject.Call(contractName, null, ctxt));
    }
    public Task<int> CallAsync(string contractName, object? args, CancellationToken cancellationToken)
    {
        var ctxt = getCallContext(null, contractName, DbObjectType.StoredProcedure);
        return Task.FromResult(_mockObject.Call(contractName, args, ctxt));
    }
    public Task<int> CallAsync<TActionContract>()
        where TActionContract : IPhormContract
    {
        var (schemaName, contractName, objectType) = PhormContractRunner<IPhormContract>.ResolveContractName(typeof(TActionContract));
        var ctxt = getCallContext(schemaName, contractName, objectType);
        return Task.FromResult(_mockObject.Call<TActionContract>(null, ctxt));
    }
    public Task<int> CallAsync<TActionContract>(object? args)
        where TActionContract : IPhormContract
    {
        var (schemaName, contractName, objectType) = PhormContractRunner<IPhormContract>.ResolveContractName(typeof(TActionContract));
        var ctxt = getCallContext(schemaName, contractName, objectType);
        return Task.FromResult(_mockObject.Call<TActionContract>(args, ctxt));
    }
    public Task<int> CallAsync<TActionContract>(CancellationToken cancellationToken)
        where TActionContract : IPhormContract
    {
        var (schemaName, contractName, objectType) = PhormContractRunner<IPhormContract>.ResolveContractName(typeof(TActionContract));
        var ctxt = getCallContext(schemaName, contractName, objectType);
        return Task.FromResult(_mockObject.Call<TActionContract>(null, ctxt));
    }
    public Task<int> CallAsync<TActionContract>(object? args, CancellationToken cancellationToken)
        where TActionContract : IPhormContract
    {
        var (schemaName, contractName, objectType) = PhormContractRunner<IPhormContract>.ResolveContractName(typeof(TActionContract));
        var ctxt = getCallContext(schemaName, contractName, objectType);
        return Task.FromResult(_mockObject.Call<TActionContract>(args, ctxt));
    }
    public Task<int> CallAsync<TActionContract>(TActionContract contract)
        where TActionContract : IPhormContract
    {
        var (schemaName, contractName, objectType) = PhormContractRunner<IPhormContract>.ResolveContractName(typeof(TActionContract));
        var ctxt = getCallContext(schemaName, contractName, objectType);
        return Task.FromResult(_mockObject.Call<TActionContract>(contract, ctxt));
    }
    public Task<int> CallAsync<TActionContract>(TActionContract contract, CancellationToken cancellationToken)
        where TActionContract : IPhormContract
    {
        var (schemaName, contractName, objectType) = PhormContractRunner<IPhormContract>.ResolveContractName(typeof(TActionContract));
        var ctxt = getCallContext(schemaName, contractName, objectType);
        return Task.FromResult(_mockObject.Call<TActionContract>(contract, ctxt));
    }

    public class MockPhormContractRunner<TActionContract> : IPhormContractRunner<TActionContract>
        where TActionContract : IPhormContract
    {
        private readonly IPhormSessionMock _mockObject;
        private readonly string? _contractName;
        private readonly object? _args;
        private readonly CallContext _callContext;

        public MockPhormContractRunner(MockPhormSession session, IPhormSessionMock mockObject, string? contractName, object? args)
        {
            _mockObject = mockObject;
            _contractName = contractName;
            _args = args;

            var (schemaName, objectName, objectType) = PhormContractRunner<IPhormContract>.ResolveContractName(typeof(TActionContract), contractName);
            _callContext = session.getCallContext(schemaName, objectName, objectType);
        }

        public TResult? Get<TResult>()
            where TResult : class
        {
            if (typeof(TActionContract) == typeof(IPhormContract))
            {
                return _mockObject.GetFrom<TResult>(_contractName, _args, _callContext);
            }
            return _mockObject.GetFrom<TActionContract, TResult>(_args, _callContext);
        }

        public Task<TResult?> GetAsync<TResult>() where TResult : class
        {
            if (_contractName != null)
            {
                return Task.FromResult(_mockObject.GetFrom<TResult>(_contractName, _args, _callContext));
            }
            return Task.FromResult(_mockObject.GetFrom<TActionContract, TResult>(_args, _callContext));
        }

        public Task<TResult?> GetAsync<TResult>(CancellationToken cancellationToken) where TResult : class
        {
            if (_contractName != null)
            {
                return Task.FromResult(_mockObject.GetFrom<TResult>(_contractName, _args, _callContext));
            }
            return Task.FromResult(_mockObject.GetFrom<TActionContract, TResult>(_args, _callContext));
        }
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

    public TResult? Get<TResult>()
        where TResult : class
    {
        return new MockPhormContractRunner<IPhormContract>(this, _mockObject, null, null)
            .Get<TResult>();
    }
    public TResult? Get<TResult>(object? args)
        where TResult : class
    {
        return new MockPhormContractRunner<IPhormContract>(this, _mockObject, null, args)
            .Get<TResult>();
    }
    public TResult? Get<TResult>(TResult args)
        where TResult : class
    {
        return new MockPhormContractRunner<IPhormContract>(this, _mockObject, null, args)
            .Get<TResult>();
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
}
