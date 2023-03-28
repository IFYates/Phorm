using IFY.Phorm.Data;
using IFY.Phorm.Execution;

namespace IFY.Phorm.Mockable;

internal class MockPhormContractRunner<TActionContract> : IPhormContractRunner<TActionContract>
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
        _callContext = session.GetCallContext(schemaName, objectName, objectType);
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
