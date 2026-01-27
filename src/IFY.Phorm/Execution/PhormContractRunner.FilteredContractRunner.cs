using IFY.Phorm.Data;
using System.Linq.Expressions;

namespace IFY.Phorm.Execution;

partial class PhormContractRunner<TActionContract>
    where TActionContract : IPhormContract
{
    /// <summary>
    /// Reads entities from the datasource and filters them using minimal resolution.
    /// </summary>
    /// <typeparam name="TEntity">The entity type that will be received and filtered OR the base type of the <see cref="GenSpecBase"/> <typeparamref name="TResult"/>. Must have a default constructor, unless this is the abstract base type of the GenSpec.</typeparam>
    /// <typeparam name="TResult">Either the same as <typeparamref name="TEntity"/>, or any GenSpec&lt;TEntity, ...&gt;.</typeparam>
    internal sealed class FilteredContractRunner<TEntity, TResult> : IPhormFilteredContractRunner<TResult>
        where TEntity : class
        where TResult : class
    {
        private readonly PhormContractRunner<TActionContract> _parent;
        private readonly Expression<Func<TEntity, bool>> _predicate;

        public FilteredContractRunner(PhormContractRunner<TActionContract> parent, Expression<Func<TEntity, bool>> predicate)
        {
            // Runtime checks for type-safety
            if (typeof(TResult) == typeof(IEnumerable<TEntity>))
            {
                if (typeof(TEntity).GetConstructor([]) == null)
                {
                    throw new ArgumentException($"Type argument TResult for FilteredContractRunner must have a public default constructor.");
                }
            }
            else if (typeof(GenSpecBase).IsAssignableFrom(typeof(TResult)))
            {
                if (!typeof(GenSpecBase<TEntity>).IsAssignableFrom(typeof(TResult)))
                {
                    throw new ArgumentException($"Type argument TResult for FilteredContractRunner must use TResult as the GenSpec TBase.");
                }
            }
            else
            {
                throw new ArgumentException($"Type argument TResult for FilteredContractRunner must match TResult or be GenSpec<TResult, ...>.");
            }

            _parent = parent;
            _predicate = predicate;
        }

        public async Task<TResult> GetAllAsync(CancellationToken cancellationToken)
        {
            // Discover minimum properties required from expression
            var predicateProperties = _predicate.Body.GetExpressionParameterProperties(typeof(TEntity));
            var cond = _predicate.Compile();

            var contract = getContract(typeof(TEntity));
            return (TResult)await _parent.executeGetAll(typeof(TResult), contract,
                (contract, rowData, _) =>
                {
                    var predicateMembers = contract.Members
                        .Where(m => predicateProperties.Any(p => p.Name == m.SourceMember!.Name)).ToArray();
                    var otherMembers = contract.Members.Except(predicateMembers).ToArray();

                    // Resolve predicate properties for entity then filter
                    var inst = contract.BuildInstance(rowData);
                    EntityContract.FillInstance(inst, rowData, predicateMembers);
                    if (!cond((TEntity)inst))
                    {
                        return null;
                    }

                    // Resolver for remaining properties
                    return () => EntityContract.FillInstance(inst, rowData, otherMembers);
                }, cancellationToken);
        }
    }
}
