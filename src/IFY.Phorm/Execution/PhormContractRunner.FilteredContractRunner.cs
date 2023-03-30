using IFY.Phorm.Data;
using System.Linq.Expressions;

namespace IFY.Phorm.Execution;

internal sealed partial class PhormContractRunner<TActionContract>
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
                if (typeof(TEntity).GetConstructor(Array.Empty<Type>()) == null)
                {
                    throw new ArgumentException($"Type argument TResult for FilteredContractRunner must have a public default constructor.");
                }
            }
            else if (typeof(TResult).BaseType == typeof(GenSpecBase))
            {
                if (typeof(TResult).GenericTypeArguments.Length == 0 || typeof(TResult).GenericTypeArguments[0] != typeof(TEntity))
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

        public TResult GetAll()
            => GetAllAsync(CancellationToken.None).GetAwaiter().GetResult();
        public async Task<TResult> GetAllAsync(CancellationToken cancellationToken)
        {
            // Prepare and execute
            using var cmd = _parent.startCommand(out var pars, out var eventArgs);
            using var console = _parent._session.StartConsoleCapture(eventArgs.CommandGuid, cmd);
            using var rdr = await cmd.ExecuteReaderAsync(cancellationToken);

            var isGenSpec = typeof(TResult).BaseType == typeof(GenSpecBase);

            var entityType = typeof(TEntity);
            var resultMembers = ContractMemberDefinition.GetFromContract(entityType)
                .ToDictionary(m => m.DbName.ToUpperInvariant());

            // Discover minimum properties required from expression
            var predicateProperties = _predicate.Body.GetExpressionParameterProperties(typeof(TEntity));
            var cond = _predicate.Compile();

            // Build list of self-resolving entities
            var resolverList = new EntityList<TEntity>();
            while (!cancellationToken.IsCancellationRequested && _parent.safeRead(rdr, console))
            {
                var predicateMembers = resultMembers.Where(m => predicateProperties.Contains(m.Value.SourceMember))
                    .ToDictionary(k => k.Key, v => v.Value);
                var otherMembers = resultMembers.Except(predicateMembers)
                    .ToDictionary(k => k.Key, v => v.Value);

                var row = PhormContractRunner<TActionContract>.getRowValues(rdr);

                TEntity inst;
                if (!isGenSpec)
                {
                    inst = Activator.CreateInstance<TEntity>();
                }
                else
                {
                    // TODO: resolve spec instance
                    inst = null!;
                }

                // Resolve predicate properties for entity and filter
                var entity = (TEntity)_parent.fillEntity(inst, row, predicateMembers, eventArgs.CommandGuid, false);
                if (!cond(inst))
                {
                    continue;
                }

                // Resolver for remaining properties
                resolverList.AddEntity(() => _parent.fillEntity(inst, row, otherMembers, eventArgs.CommandGuid, true));
            }

            // TODO: Process sub results

            _parent.parseCommandResult(cmd, _parent._runArgs, pars, console.GetConsoleMessages(), eventArgs, resolverList.Count);

            if (isGenSpec)
            {
                var gs = (GenSpecBase)Activator.CreateInstance(typeof(TResult));
                gs.SetData(resolverList); // TODO: Must not resolve
                return (TResult)(object)gs;
            }

            return (TResult)(object)resolverList;
        }
    }
}
