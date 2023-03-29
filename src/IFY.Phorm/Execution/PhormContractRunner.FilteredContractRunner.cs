using IFY.Phorm.Data;
using System.Linq.Expressions;
using System.Reflection;

namespace IFY.Phorm.Execution;

internal sealed partial class PhormContractRunner<TActionContract> where TActionContract : IPhormContract
{
    /// <summary>
    /// Reads entities from the datasource and filters them using minimal resolution.
    /// </summary>
    /// <typeparam name="TEntity">The entity type that will be received and filtered.</typeparam>
    protected sealed class FilteredContractRunner<TEntity> : BaseContractRunner, IPhormContractRunner<TActionContract, TEntity>
        where TEntity : class, new()
    {
        /// <summary>
        /// Resolve the entity properties used in the predicate.
        /// </summary>
        /// <returns></returns>
        private static string[] getPredicateProperties(Expression<Func<TEntity, bool>> predicate)
        {
            var props = predicate.Body.GetExpressionParameterProperties(typeof(TEntity));
            return props.Select(p => p.Name).ToArray(); // TODO: property aliases
        }

        private readonly PhormContractRunner<TActionContract> _parent;
        private readonly Expression<Func<TEntity, bool>> _predicate;

        public FilteredContractRunner(PhormContractRunner<TActionContract> parent, Expression<Func<TEntity, bool>> predicate)
        {
            _parent = parent;
            _predicate = predicate;
        }

        public IEnumerable<TEntity> GetAll()
            => GetAllAsync(CancellationToken.None).GetAwaiter().GetResult();
        public Task<IEnumerable<TEntity>> GetAllAsync()
            => GetAllAsync(CancellationToken.None);
        public async Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken)
        {
            // Prepare and execute
            using var cmd = _parent.startCommand(out var pars, out var eventArgs);
            using var console = _parent._session.StartConsoleCapture(eventArgs.CommandGuid, cmd);
            using var rdr = await cmd.ExecuteReaderAsync(cancellationToken);

            // TODO: GenSpec

            var entityType = typeof(TEntity);
            var resultMembers = ContractMemberDefinition.GetFromContract(entityType)
                .ToDictionary(m => m.DbName.ToUpperInvariant());

            // Discover minimum properties required from expression
            var predicateProperties = getPredicateProperties(_predicate);
            var cond = _predicate.Compile();

            // Build list of self-resolving entities
            var resolverList = new EntityList<TEntity>();
            while (!cancellationToken.IsCancellationRequested && _parent.safeRead(rdr, console))
            {
                var row = PhormContractRunner<TActionContract>.getRowValues(rdr);
                var members = resultMembers.ToDictionary(k => k.Key, v => v.Value);

                // Resolve predicate properties for entity and filter
                var predicateValues = row.Where(r => predicateProperties.Contains(r.Key))
                    .ToDictionary(r => r.Key, r => r.Value);
                var entity = (TEntity)_parent.fillEntity(new TEntity(), predicateValues, members, eventArgs.CommandGuid, false);
                if (!cond(entity))
                {
                    continue;
                }

                // Resolver for remaining properties
                row = row.Where(r => !predicateProperties.Contains(r.Key))
                    .ToDictionary(r => r.Key, r => r.Value);
                resolverList.AddEntity(() => _parent.fillEntity(entity, row, members, eventArgs.CommandGuid, true));
            }

            // TODO: Process sub results

            _parent.parseCommandResult(cmd, _parent._runArgs, pars, console.GetConsoleMessages(), eventArgs, resolverList.Count);

            return resolverList;
        }
    }
}
