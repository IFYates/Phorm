using IFY.Phorm.Data;
using System.Linq.Expressions;

namespace IFY.Phorm.Execution;

public interface IPhormContractRunner
{
    /// <summary>
    /// Get one or more entity instances.
    /// </summary>
    /// <typeparam name="TResult">The type of entity to map result data to.</typeparam>
    /// <returns>When <typeparamref name="TResult"/> is the entity type, will return the single result instance or null. When <typeparamref name="TResult"/> is an array of the entity type, will return an array of all types from the result (never null).</returns>
    TResult? Get<TResult>()
        where TResult : class;
    /// <summary>
    /// Get one or more entity instances.
    /// </summary>
    /// <typeparam name="TResult">The type of entity to map result data to.</typeparam>
    /// <returns>When <typeparamref name="TResult"/> is the entity type, will return the single result instance or null. When <typeparamref name="TResult"/> is an array of the entity type, will return an array of all types from the result (never null).</returns>
    public Task<TResult?> GetAsync<TResult>()
        where TResult : class
        => GetAsync<TResult>(CancellationToken.None);
    /// <summary>
    /// Get one or more entity instances.
    /// </summary>
    /// <typeparam name="TResult">The type of entity to map result data to.</typeparam>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>When <typeparamref name="TResult"/> is the entity type, will return the single result instance or null. When <typeparamref name="TResult"/> is an array of the entity type, will return an array of all types from the result (never null).</returns>
    Task<TResult?> GetAsync<TResult>(CancellationToken cancellationToken)
        where TResult : class;

    /// <summary>
    /// Adds a predicate to filter the resultset before the entire entity is parsed.
    /// </summary>
    /// <typeparam name="TEntity">The entity type that will be fetched in the subsequent Get call.</typeparam>
    IPhormFilteredContractRunner<IEnumerable<TEntity>> Where<TEntity>(Expression<Func<TEntity, bool>> predicate)
        where TEntity : class, new();
    /// <summary>
    /// Adds a predicate to filter the resultset before the entire entity is parsed as a gen-spec.
    /// </summary>
    /// <typeparam name="TBase">The base entity type that will be used to filter results.</typeparam>
    /// <typeparam name="TGenSpec">The full gen-spec definition of the resultset</typeparam>
    IPhormFilteredContractRunner<TGenSpec> Where<TBase, TGenSpec>(Expression<Func<TBase, bool>> predicate)
        where TBase : class
        where TGenSpec : GenSpecBase<TBase>;

    // TODO: OrderBy, Skip, Take?
}

public interface IPhormContractRunner<TActionContract> : IPhormContractRunner
    where TActionContract : IPhormContract
{
    /// <summary>
    /// The resolve contract type.
    /// </summary>
    public Type ContractType => typeof(TActionContract);
}
