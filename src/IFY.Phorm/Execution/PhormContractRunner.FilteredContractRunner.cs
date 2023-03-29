using IFY.Phorm.Data;
using System.Linq.Expressions;

namespace IFY.Phorm.Execution;

internal sealed partial class PhormContractRunner<TActionContract> where TActionContract : IPhormContract
{
    /// <summary>
    /// Reads entities from the datasource and filters them using minimal resolution.
    /// </summary>
    /// <typeparam name="TEntity">The entity type that will be received and filtered.</typeparam>
    private sealed class FilteredContractRunner<TEntity> : IPhormFilteredContractRunner<TEntity>
        where TEntity : class, new()
    {
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

                // Resolve predicate properties for entity and filter
                var entity = (TEntity)_parent.fillEntity(new TEntity(), row, predicateMembers, eventArgs.CommandGuid, false);
                if (!cond(entity))
                {
                    continue;
                }

                // Resolver for remaining properties
                resolverList.AddEntity(() => _parent.fillEntity(entity, row, otherMembers, eventArgs.CommandGuid, true));
            }

            // TODO: Process sub results

            _parent.parseCommandResult(cmd, _parent._runArgs, pars, console.GetConsoleMessages(), eventArgs, resolverList.Count);

            return resolverList;
        }
    }
}
