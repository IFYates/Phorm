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
            else if (typeof(GenSpecBase).IsAssignableFrom(typeof(TResult)))
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

            // Prepare genspec
            GenSpecBase? genspec = null;
            Dictionary<string, object?>? tempProps = null;
            if (typeof(GenSpecBase).IsAssignableFrom(typeof(TResult)))
            {
                genspec = (GenSpecBase)Activator.CreateInstance(typeof(TResult))!;
                tempProps ??= new();
            }

            // Discover minimum properties required from expression
            var predicateProperties = _predicate.Body.GetExpressionParameterProperties(typeof(TEntity));
            var cond = _predicate.Compile();

            // Build list of self-resolving entities
            ContractMemberDefinition[]? entityMembers = null;
            var resolverList = new EntityList<TEntity>();
            while (!cancellationToken.IsCancellationRequested && _parent.safeRead(rdr, console))
            {
                TEntity inst;
                if (genspec == null)
                {
                    inst = Activator.CreateInstance<TEntity>();
                    entityMembers ??= ContractMemberDefinition.GetFromContract(typeof(TEntity));
                }
                else
                {
                    // Resolve spec type
                    tempProps!.Clear();
                    var spec = genspec.GetFirstSpecType(m =>
                    {
                        // Check Gen property for the Spec type (cached)
                        if (!tempProps.TryGetValue(m.SourceMemberId!, out var propValue)
                            && m.TryFromDatasource(rdr[m.DbName], null, out var gen))
                        {
                            propValue = gen.Value;
                            tempProps[gen.SourceMemberId!] = propValue;
                        }
                        return propValue;
                    });

                    // No spec type and base type abstract
                    if (spec == null && genspec.GenType.IsAbstract)
                    {
                        // TODO: Warning events for dropped records
                        continue;
                    }
                    
                    var entityType = spec?.Type ?? genspec.GenType;
                    entityMembers = ContractMemberDefinition.GetFromContract(entityType);
                    inst = (TEntity)Activator.CreateInstance(entityType)!;
                }

                var predicateMembers = entityMembers.Where(m => predicateProperties.Any(p => p.Name == m.SourceMember!.Name))
                    .ToDictionary(k => k.DbName.ToUpperInvariant(), v => v);
                var otherMembers = entityMembers.Except(predicateMembers.Values)
                    .ToDictionary(k => k.DbName.ToUpperInvariant(), v => v);

                var row = PhormContractRunner<TActionContract>.getRowValues(rdr);

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

            if (genspec != null)
            {
                genspec.SetData(resolverList); // TODO: Must not resolve
                return (TResult)(object)genspec;
            }

            return (TResult)(object)resolverList;
        }
    }
}
