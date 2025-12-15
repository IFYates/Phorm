using IFY.Phorm.Data;
using IFY.Phorm.EventArgs;
using IFY.Phorm.Telemetry;
using System.Data;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

namespace IFY.Phorm.Execution;

internal sealed partial class PhormContractRunner<TActionContract> : IPhormContractRunner<TActionContract>
    where TActionContract : IPhormContract
{
    private readonly AbstractPhormSession _session;
    private readonly IDbTransaction? _transaction;
    private readonly string? _schema;
    private readonly string _objectName;
    private readonly object? _runArgs;
    private readonly DbObjectType _objectType;
    private readonly bool _readOnly;

    public PhormContractRunner(AbstractPhormSession session, string? objectName, DbObjectType objectType, object? args, IDbTransaction? transaction)
        : this(session, typeof(TActionContract), objectName, objectType, args, transaction)
    {
    }
    public PhormContractRunner(AbstractPhormSession session, Type contractType, string? objectName, DbObjectType objectType, object? args, IDbTransaction? transaction)
    {
        _session = session;
        _transaction = transaction;

        contractType = contractType.IsArray ? contractType.GetElementType()! : contractType;
        var contractName = contractType.Name;
        if (contractType == typeof(IPhormContract))
        {
            _objectName = objectName ?? throw new ArgumentNullException(nameof(objectName));
        }
        else
        {
            if (contractType.IsInterface && contractName[0] == 'I')
            {
                contractName = contractName[1..];
            }
            objectName = null;
            _objectName = contractName;
        }

        // Check for override attribute
        var pcAttr = contractType.GetCustomAttribute<PhormContractAttribute>(false);
        if (pcAttr != null)
        {
            _schema = pcAttr.Namespace;
            _objectName = objectName ?? pcAttr.Name ?? contractName;
            _objectType = pcAttr.Target;
            _readOnly = pcAttr.ReadOnly;
        }
        else
        {
            var dcAttr = contractType.GetCustomAttribute<DataContractAttribute>(false);
            if (dcAttr != null)
            {
                _schema = dcAttr.Namespace ?? _schema;
                _objectName = objectName ?? dcAttr.Name ?? contractName;
            }
        }

        if (_objectType == DbObjectType.Default)
        {
            _objectType = objectType == DbObjectType.Default ? DbObjectType.StoredProcedure : objectType;
        }

        _runArgs = args;
    }

    public IPhormFilteredContractRunner<IEnumerable<TEntity>> Where<TEntity>(Expression<Func<TEntity, bool>> predicate)
        where TEntity : class, new()
    {
        return new FilteredContractRunner<TEntity, IEnumerable<TEntity>>(this, predicate);
    }
    public IPhormFilteredContractRunner<TGenSpec> Where<TBase, TGenSpec>(Expression<Func<TBase, bool>> predicate)
        where TBase : class
        where TGenSpec : GenSpecBase<TBase>
    {
        return new FilteredContractRunner<TBase, TGenSpec>(this, predicate);
    }

    #region Execution

    private IAsyncDbCommand startCommand(out ContractMember[] members, out CommandExecutingEventArgs eventArgs)
    {
        members = ContractMember.GetMembersFromContract(_runArgs, typeof(TActionContract), true);
        var cmd = _session.CreateCommand(_schema, _objectName, _objectType, _readOnly);
        cmd.Transaction = _transaction;

        // Build WHERE clause from members
        if (_objectType is DbObjectType.Table or DbObjectType.View)
        {
            var sb = new StringBuilder();
            foreach (var memb in members.Where(static m => (m.Direction & ParameterType.Input) > 0)
                .Where(static m => m.Value != null && m.Value != DBNull.Value))
            {
                // TODO: Ignore members without value
                if (sb.Length > 0)
                {
                    sb.Append(" AND ");
                }
                sb.AppendFormat("[{0}] = @{0}", memb.DbName);
            }
            if (sb.Length > 0)
            {
                cmd.CommandText += $" WHERE {sb}";
            }
        }

        // Convert to database parameters
        foreach (var memb in members)
        {
            var param = memb.ToDataParameter(cmd, _runArgs);
            if (param?.Value != null
                && (param.Direction != ParameterDirection.Input || param.Value != DBNull.Value))
            {
                cmd.Parameters.Add(param);
            }
        }

        eventArgs = new CommandExecutingEventArgs
        {
            CommandGuid = Guid.NewGuid(),
            CommandText = cmd.CommandText,
            CommandParameters = cmd.Parameters.Cast<IDbDataParameter>()
                .ToDictionary(static p => p.ParameterName, static p => (object?)p.Value)
        };
        _session.OnCommandExecuting(eventArgs);

        return cmd;
    }

    private void matchResultset(Type entityType, int order, IDataReader rdr, IEnumerable<object> parents, Guid commandGuid, AbstractConsoleMessageCapture console)
    {
        // Find resultset target
        var rsProp = entityType.GetProperties()
            .SingleOrDefault(p => p.GetCustomAttribute<ResultsetAttribute>()?.Order == order);
        if (rsProp == null)
        {
            // TODO: Warn if child resultset is being discarded
            return;
        }
        if (rsProp?.CanWrite != true)
        {
            throw new InvalidDataContractException($"Phorm Resultset property '{rsProp?.Name}' is not writable.");
        }

        // Get data
        var recordType = rsProp.PropertyType.IsArray ? rsProp.PropertyType.GetElementType()! : rsProp.PropertyType;
        var records = new List<object>();
        var recordMembers = ContractMemberDefinition.GetFromContract(recordType);
        while (safeRead(rdr, console))
        {
            var values = getRowValues(rdr);
            var members = recordMembers.ToDictionary(static m => m.DbName.ToUpperInvariant()); // Copy
            var entity = Activator.CreateInstance(recordType)!;
            var res = fillEntity(entity, values, members, commandGuid, true);
            records.Add(res);
        }

        // Use selector
        var attr = rsProp.GetCustomAttribute<ResultsetAttribute>() ?? new ResultsetAttribute(0, string.Empty);
        foreach (var parent in parents)
        {
            var matches = attr.FilterMatched(parent!, records);
            if (rsProp.PropertyType.IsArray)
            {
                var arr = (Array?)Activator.CreateInstance(rsProp.PropertyType, [matches.Length]) ?? Array.Empty<object>();
                Array.Copy(matches, arr, matches.Length);
                rsProp.SetValue(parent, arr);
            }
            else if (matches.Length > 1)
            {
                throw new InvalidCastException($"Phorm Resultset property {rsProp.Name} is not an array but matched {matches.Length} records.");
            }
            else
            {
                rsProp.SetValue(parent, matches.FirstOrDefault());
            }
        }
    }

    private static Dictionary<string, object?> getRowValues(IDataReader rdr)
    {
        var result = new Dictionary<string, object?>();
        for (var i = 0; i < rdr.FieldCount; ++i)
        {
            var fieldName = rdr.GetName(i);
            var value = rdr.GetValue(i);
            if (value == DBNull.Value)
            {
                value = null;
            }

            result[fieldName] = value;
        }
        return result;
    }

    private object fillEntity(object entity, IDictionary<string, object?> values, IDictionary<string, ContractMemberDefinition> members, Guid commandGuid, bool warnOnUnresolved)
    {
        // Apply member values
        var deferredMembers = new Dictionary<ContractMemberDefinition, object?>();
        foreach (var (fieldName, value) in values)
        {
            if (members.Remove(fieldName.ToUpperInvariant(), out var memb))
            {
                if (memb.HasSecureAttribute // Defer secure members until after non-secure, to allow for authenticator properties
                    || memb.HasTransphormation) // Defer transformation members until after non-transformed
                {
                    deferredMembers[memb] = value;
                }
                else if (memb.TryFromDatasource(value, entity, out var member))
                {
                    member.ApplyToEntity(entity);
                }
            }
            else if (warnOnUnresolved)
            {
                // Report unexpected column
                _session.OnUnexpectedRecordColumn(new UnexpectedRecordColumnEventArgs
                {
                    CommandGuid = commandGuid,
                    EntityType = entity.GetType(),
                    ColumnName = fieldName
                });
            }
        }

        // Apply deferred values
        foreach (var kvp in deferredMembers.OrderBy(static m => m.Key.HasSecureAttribute))
        {
            if (kvp.Key.TryFromDatasource(kvp.Value, entity, out var member))
            {
                member.ApplyToEntity(entity);
            }
        }

        // Warnings for missing expected columns
        if (warnOnUnresolved && members.Count > 0)
        {
            _session.OnUnresolvedContractMember(new UnresolvedContractMemberEventArgs
            {
                CommandGuid = commandGuid,
                EntityType = entity.GetType(),
                MemberNames = members.Values.Where(static m => (m.Direction & ParameterType.Output) > 0 && m.Direction != ParameterType.ReturnValue)
                    .Select(static m => m.SourceMember?.Name ?? m.DbName).ToArray()
            });
        }

        return entity;
    }

    private int parseCommandResult(IAsyncDbCommand cmd, object? contract, ContractMember[] members, IEnumerable<ConsoleMessage> consoleEvents, CommandExecutingEventArgs eventArgs, int? resultCount)
    {
        // Update parameters for output values
        var returnValue = 0;
        foreach (IDataParameter param in cmd.Parameters)
        {
            updateOutputParameter(param, contract, members, ref returnValue);
        }

        // Support console capture
        if (consoleEvents?.Any() == true)
        {
            var consoleProp = contract?.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(static p => typeof(ConsoleLogMember).IsAssignableFrom(p.PropertyType))
                .Select(p => p.GetValue(contract) as ConsoleLogMember)
                .FirstOrDefault(static v => v?.Direction == ParameterType.Console);
            consoleProp?.SetValue(consoleEvents.ToArray());
        }

        _session.OnCommandExecuted(new CommandExecutedEventArgs
        {
            CommandGuid = eventArgs.CommandGuid,
            CommandText = eventArgs.CommandText,
            CommandParameters = eventArgs.CommandParameters,
            ResultCount = resultCount,
            ReturnValue = returnValue
        });
        return returnValue;
    }
    private static void updateOutputParameter(IDataParameter param, object? contract, ContractMember[] members, ref int returnValue)
    {
        if (param.Direction == ParameterDirection.ReturnValue)
        {
            returnValue = (int?)param.Value ?? 0;
            foreach (var memb in members.Where(static a => a.Direction == ParameterType.ReturnValue))
            {
                memb.SetValue(returnValue);
            }
        }
        else if (contract != null && (param.Direction & ParameterDirection.Output) > 0
            && members.Single(a => a.DbName == param.ParameterName[1..]).TryFromDatasource(param.Value, contract, out var memb))
        {
            memb.ApplyToEntity(contract);
        }
    }

    private bool safeRead(IDataReader rdr, AbstractConsoleMessageCapture console)
    {
        try
        {
            return rdr.Read();
        }
        catch (Exception ex)
        {
            if (!_session.ExceptionsAsConsoleMessage
                || !console.ProcessException(ex))
            {
                throw;
            }
            return false;
        }
    }

    #endregion Execution

    public async Task<int> CallAsync(CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        using var activity = PhormActivitySource.Source.StartActivity(
            "phorm.call", 
            ActivityKind.Client);
        
        try
        {
            // Prepare execution
            using var cmd = startCommand(out var pars, out var eventArgs);
            
            activity?.SetTag("db.statement", cmd.CommandText);
            activity?.SetTag("phorm.contract", typeof(TActionContract).Name);
            
            using var console = _session.StartConsoleCapture(eventArgs.CommandGuid, cmd);

            // Execution
            using var rdr = await cmd.ExecuteReaderAsync(cancellationToken);

            if (safeRead(rdr, console) && _session.StrictResultSize)
            {
                throw new InvalidOperationException("Phorm non-result request returned a result.");
            }

            var returnValue = parseCommandResult(cmd, _runArgs, pars, console.GetConsoleMessages(), eventArgs, null);
            activity?.SetTag("phorm.return_value", returnValue);

            PhormMetrics.CallCounter.Add(1, 
                new KeyValuePair<string, object?>("contract", typeof(TActionContract).Name),
                new KeyValuePair<string, object?>("object", _objectName));
            PhormMetrics.CallDuration.Record(sw.ElapsedMilliseconds,
                new KeyValuePair<string, object?>("contract", typeof(TActionContract).Name));
            
            activity?.SetStatus(ActivityStatusCode.Ok);
            return returnValue;
        }
        catch (Exception ex)
        {
            PhormMetrics.ErrorCounter.Add(1,
                new KeyValuePair<string, object?>("operation", "call"),
                new KeyValuePair<string, object?>("error_type", ex.GetType().Name));
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddException(ex);
            throw;
        }
    }

    public async Task<TResult?> GetAsync<TResult>(CancellationToken cancellationToken)
        where TResult : class
    {
        var sw = Stopwatch.StartNew();
        using var activity = PhormActivitySource.Source.StartActivity(
            "phorm.get", 
            ActivityKind.Client);
        activity?.SetTag("phorm.result_type", typeof(TResult).Name);
        
        try
        {
            // Check whether this is One or Many
            var entityType = typeof(TResult);
            var resultType = typeof(TResult);
            bool isArray = entityType.IsArray, isEnumerable = isArray;
            var isGenSpec = typeof(GenSpecBase).IsAssignableFrom(resultType);
            if (isArray)
            {
                entityType = entityType.GetElementType()!;
                resultType = typeof(IEnumerable<>).MakeGenericType(entityType);
            }
            else if (isGenSpec)
            {
                entityType = entityType.GenericTypeArguments[0];
            }
            else if (entityType.IsGenericType)
            {
                var genTypeDef = entityType.GetGenericTypeDefinition();
                isEnumerable = genTypeDef == typeof(IEnumerable<>) || genTypeDef == typeof(ICollection<>);
                if (isEnumerable)
                {
                    entityType = entityType.GenericTypeArguments[0];
                }
            }
            if (!isGenSpec && entityType.GetConstructor([]) == null)
            {
                throw new MissingMethodException($"Attempt to get type {entityType.FullName} without empty constructor.");
            }

            // Execute method as either IEnumerable<TEntity> or GenSpec<...>
            var result = await executeGetAll(resultType, entityType,
                (inst, entityMembers, rowData, commandGuid, record) =>
                {
                    // Single result
                    if (!isEnumerable && !isGenSpec && record > 1)
                    {
                        if (_session.StrictResultSize)
                        {
                            throw new InvalidOperationException("Expected a single-record result, but more than one found.");
                        }
                        return null;
                    }

                    var members = entityMembers.ToDictionary(static k => k.DbName.ToUpperInvariant());
                    return () => fillEntity(inst, rowData, members, commandGuid, true);
                }, cancellationToken);
            if (result is IEnumerable<object> coll)
            {
                activity?.SetTag("phorm.result_count", coll.Count());
            }

            PhormMetrics.GetCounter.Add(1,
                new KeyValuePair<string, object?>("contract", typeof(TActionContract).Name),
                new KeyValuePair<string, object?>("object", _objectName));
            PhormMetrics.GetDuration.Record(sw.ElapsedMilliseconds,
                new KeyValuePair<string, object?>("contract", typeof(TActionContract).Name));

            activity?.SetStatus(ActivityStatusCode.Ok);
            return (TResult)result;
        }
        catch (Exception ex)
        {
            PhormMetrics.ErrorCounter.Add(1,
                new KeyValuePair<string, object?>("operation", "get"),
                new KeyValuePair<string, object?>("error_type", ex.GetType().Name));
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddException(ex);
            throw;
        }
    }

    private async Task<object> executeGetAll(Type resultType, Type entityType, Func<object, ContractMemberDefinition[], IDictionary<string, object?>, Guid, int, Func<object>?> entityProcessor, CancellationToken cancellationToken)
    {
        // Prepare and execute
        using var cmd = startCommand(out var pars, out var eventArgs);
        using var console = _session.StartConsoleCapture(eventArgs.CommandGuid, cmd);
        using var rdr = await cmd.ExecuteReaderAsync(cancellationToken);

        // Prepare genspec
        GenSpecBase? genspec = null;
        Dictionary<string, object?>? tempProps = null;
        ContractMemberDefinition[]? entityMembers = null;
        if (typeof(GenSpecBase).IsAssignableFrom(resultType))
        {
            genspec = (GenSpecBase)Activator.CreateInstance(resultType)!;
            tempProps ??= [];
        }
        else
        {
            entityMembers = ContractMemberDefinition.GetFromContract(entityType);
        }

        // Build list of self-resolving entities
        var resolverListType = typeof(EntityList<>).MakeGenericType(entityType);
        var resolverList = (IEntityList)Activator.CreateInstance(resolverListType)!;
        var record = 0;
        while (!cancellationToken.IsCancellationRequested && safeRead(rdr, console))
        {
            ++record;
            var rowData = PhormContractRunner<TActionContract>.getRowValues(rdr);

            var instType = entityType;
            if (genspec != null)
            {
                // Resolve spec type
                tempProps!.Clear();
                var spec = genspec.GetFirstSpecType(m =>
                {
                    // Check Gen property for the Spec type (cached)
                    if (!tempProps.TryGetValue(m.SourceMemberId!, out var propValue)
                        && rowData.TryGetValue(m.DbName, out var dbValue)
                        && m.TryFromDatasource(dbValue, null, out var gen))
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

                instType = spec?.Type ?? genspec.GenType;
                entityMembers = ContractMemberDefinition.GetFromContract(instType);
            }

            var inst = Activator.CreateInstance(instType)!;
            var resolver = entityProcessor(inst, entityMembers!, rowData, eventArgs.CommandGuid, record);
            if (resolver != null)
            {
                resolverList.AddResolver(resolver);
            }
        }

        // Process sub results
        if (!console.HasError)
        {
            var rsOrder = 0;
            while (!cancellationToken.IsCancellationRequested && await rdr.NextResultAsync(CancellationToken.None))
            {
                matchResultset(entityType, rsOrder++, rdr, (IEnumerable<object>)resolverList, eventArgs.CommandGuid, console);
            }
        }

        parseCommandResult(cmd, _runArgs, pars, console.GetConsoleMessages(), eventArgs, resolverList.Count);

        if (genspec != null)
        {
            genspec.SetData(resolverList);
            return genspec;
        }
        return resolverList;
    }
}
