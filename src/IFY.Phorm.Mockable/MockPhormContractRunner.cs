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

    public MockPhormContractRunner(MockPhormSession session, Type contractType, IPhormSessionMock mockObject, string? contractName, DbObjectType suggestedObjectType, object? args)
    {
        _mockObject = mockObject;
        _args = args;
        _contractName = contractName;

        var (schemaName, objectName, objectType, readOnly) = PhormContractRunner<IPhormContract>.ResolveContractName(contractType, contractName, suggestedObjectType);
        _callContext = session.GetCallContext(schemaName, objectName, objectType, readOnly);
    }

    public async Task<TResult?> GetAsync<TResult>(CancellationToken cancellationToken) where TResult : class
    {
        return typeof(TActionContract) == typeof(IPhormContract)
            ? _mockObject.GetFrom<TResult>(_contractName, _args, cancellationToken, _callContext)
            : _mockObject.GetFrom<TActionContract, TResult>(_args, cancellationToken, _callContext);
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