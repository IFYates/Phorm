using IFY.Phorm.Data;
using IFY.Phorm.Execution;
using System.Linq.Expressions;

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

        var (schemaName, objectName, objectType, readOnly) = PhormContractRunner<IPhormContract>.ResolveContractName(typeof(TActionContract), contractName);
        _callContext = session.GetCallContext(schemaName, objectName, objectType, readOnly);
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

    public IPhormFilteredContractRunner<IEnumerable<TEntity>> Where<TEntity>(Expression<Func<TEntity, bool>> predicate) where TEntity : class, new()
    {
        throw new NotImplementedException();
    }

    public IPhormFilteredContractRunner<TGenSpec> Where<TBase, TGenSpec>(Expression<Func<TBase, bool>> predicate)
        where TBase : class
        where TGenSpec : GenSpecBase<TBase>
    {
        throw new NotImplementedException();
    }
}
