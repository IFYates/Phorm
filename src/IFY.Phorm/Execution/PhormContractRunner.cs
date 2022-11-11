﻿using IFY.Phorm.Data;
using IFY.Phorm.EventArgs;
using System.Data;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

namespace IFY.Phorm.Execution;

internal sealed class PhormContractRunner<TActionContract> : IPhormContractRunner<TActionContract>
    where TActionContract : IPhormContract
{
    private readonly AbstractPhormSession _session;
    private readonly string? _schema;
    private readonly string _objectName;
    private readonly DbObjectType _objectType;
    private readonly object? _runArgs;

    public PhormContractRunner(AbstractPhormSession session, string? objectName, DbObjectType objectType, object? args)
        : this(session, typeof(TActionContract), objectName, objectType, args)
    {
    }
    public PhormContractRunner(AbstractPhormSession session, Type contractType, string? objectName, DbObjectType objectType, object? args)
    {
        _session = session;

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

    #region Execution

    private IAsyncDbCommand startCommand(out ContractMember[] members, out CommandExecutingEventArgs eventArgs)
    {
        members = ContractMember.GetMembersFromContract(_runArgs, typeof(TActionContract), true);
        var cmd = _session.CreateCommand(_schema, _objectName, _objectType);

        // Build WHERE clause from members
        if (_objectType is DbObjectType.Table or DbObjectType.View)
        {
            var sb = new StringBuilder();
            foreach (var memb in members.Where(m => (m.Direction & ParameterType.Input) > 0)
                .Where(m => m.Value != null && m.Value != DBNull.Value))
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
            if (param.Direction != ParameterDirection.Input
                || param.Value != null && param.Value != DBNull.Value)
            {
                cmd.Parameters.Add(param);
            }
        }

        eventArgs = new CommandExecutingEventArgs
        {
            CommandGuid = Guid.NewGuid(),
            CommandText = cmd.CommandText,
            CommandParameters = cmd.Parameters.Cast<IDbDataParameter>()
                .ToDictionary(p => p.ParameterName, p => p.Value)
        };
        _session.OnCommandExecuting(eventArgs);

        return cmd;
    }

    private void matchResultset(Type entityType, int order, IDataReader rdr, IEnumerable<object> parents, Guid commandGuid, AbstractConsoleMessageCapture console)
    {
        // Find resultset target
        var rsProp = entityType.GetProperties()
            .SingleOrDefault(p => p.GetCustomAttribute<ResultsetAttribute>()?.Order == order);
        if (rsProp?.CanWrite != true)
        {
            throw new InvalidDataContractException($"Resultset property '{rsProp?.Name}' is not writable.");
        }

        // Get data
        var recordType = rsProp.PropertyType.IsArray ? rsProp.PropertyType.GetElementType()! : rsProp.PropertyType;
        var records = new List<object>();
        var recordMembers = ContractMemberDefinition.GetFromContract(recordType, null)
            .ToDictionary(m => m.DbName.ToUpperInvariant());
        while (safeRead(rdr, console))
        {
            var res = getEntity(recordType, rdr, recordMembers, commandGuid);
            records.Add(res);
        }

        // Use selector
        var attr = rsProp.GetCustomAttribute<ResultsetAttribute>() ?? new ResultsetAttribute(0, string.Empty);
        foreach (var parent in parents)
        {
            var matches = attr.FilterMatched(parent!, records);
            if (rsProp.PropertyType.IsArray)
            {
                var arr = (Array?)Activator.CreateInstance(rsProp.PropertyType, new object[] { matches.Length }) ?? Array.Empty<object>();
                Array.Copy(matches, arr, matches.Length);
                rsProp.SetValue(parent, arr);
            }
            else if (matches.Length > 1)
            {
                throw new InvalidCastException($"Resultset property {rsProp.Name} is not an array but matched {matches.Length} records.");
            }
            else
            {
                rsProp.SetValue(parent, matches.FirstOrDefault());
            }
        }
    }

    private object getEntity(Type entityType, IDataReader rdr, IDictionary<string, ContractMemberDefinition> members, Guid commandGuid)
    {
        members = members.ToDictionary(k => k.Key, v => v.Value); // Copy
        var entity = Activator.CreateInstance(entityType)!;

        // Resolve member values
        var secureMembers = new Dictionary<ContractMemberDefinition, object>();
        for (var i = 0; i < rdr.FieldCount; ++i)
        {
            var fieldName = rdr.GetName(i);
            if (members.Remove(fieldName.ToUpperInvariant(), out var memb))
            {
                if (memb.HasSecureAttribute)
                {
                    // Defer secure members until after non-secure, to allow for authenticator properties
                    secureMembers[memb] = rdr.GetValue(i);
                }
                else
                {
                    memb.FromDatasource(rdr.GetValue(i), entity)
                        .ApplyToEntity(entity);
                }
            }
            else
            {
                // Report unexpected column
                _session.OnUnexpectedRecordColumn(new UnexpectedRecordColumnEventArgs
                {
                    CommandGuid = commandGuid,
                    EntityType = entityType,
                    ColumnName = fieldName
                });
            }
        }

        // Apply secure values
        foreach (var kvp in secureMembers)
        {
            kvp.Key.FromDatasource(kvp.Value, entity)
                .ApplyToEntity(entity);
        }

        // Warnings for missing expected columns
        if (members.Count > 0)
        {
            _session.OnUnresolvedContractMember(new UnresolvedContractMemberEventArgs
            {
                CommandGuid = commandGuid,
                EntityType = entityType,
                MemberNames = members.Values.Where(m => (m.Direction & ParameterType.Output) > 0 && m.Direction != ParameterType.ReturnValue)
                    .Select(m => m.SourceMember?.Name ?? m.DbName).ToArray()
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
                .Where(p => typeof(ConsoleLogMember).IsAssignableFrom(p.PropertyType))
                .Select(p => p.GetValue(contract) as ConsoleLogMember)
                .FirstOrDefault(v => v?.Direction == ParameterType.Console);
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
            foreach (var memb in members.Where(a => a.Direction == ParameterType.ReturnValue))
            {
                memb.SetValue(returnValue);
            }
        }
        else if (contract != null && (param.Direction & ParameterDirection.Output) > 0)
        {
            var memb = members.Single(a => a.DbName == param.ParameterName[1..])
                .FromDatasource(param.Value, contract); // NOTE: Always given as VARCHAR
            var prop = memb.SourceMember;
            if (prop != null && prop.ReflectedType?.IsAssignableFrom(contract.GetType()) == false)
            {
                prop = contract.GetType().GetProperty(prop.Name);
            }
            ((PropertyInfo?)prop)?.SetValue(contract, memb.Value);
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

    public Task<int> CallAsync()
        => CallAsync(CancellationToken.None);
    public async Task<int> CallAsync(CancellationToken cancellationToken)
    {
        // Prepare execution
        using var cmd = startCommand(out var pars, out var eventArgs);
        using var console = _session.StartConsoleCapture(eventArgs.CommandGuid, cmd);

        // Execution
        using var rdr = await cmd.ExecuteReaderAsync(cancellationToken);

        if (safeRead(rdr, console) && _session.StrictResultSize)
        {
            throw new InvalidOperationException("Non-result request returned a result.");
        }

        return parseCommandResult(cmd, _runArgs, pars, console.GetConsoleMessages(), eventArgs, null);
    }

    public TResult? Get<TResult>()
        where TResult : class
        => GetAsync<TResult>(CancellationToken.None).GetAwaiter().GetResult();
    public Task<TResult?> GetAsync<TResult>()
        where TResult : class
        => GetAsync<TResult>(CancellationToken.None);
    public async Task<TResult?> GetAsync<TResult>(CancellationToken cancellationToken)
        where TResult : class
    {
        // Check whether this is One or Many
        var entityType = typeof(TResult);
        var isArray = entityType.IsArray;
        if (isArray)
        {
            entityType = entityType.GetElementType()!;
        }
        if (entityType.GetConstructor(Array.Empty<Type>()) == null)
        {
            throw new MissingMethodException($"Attempt to get type {entityType.FullName} without empty constructor.");
        }

        // Prepare execution
        using var cmd = startCommand(out var pars, out var eventArgs);
        using var console = _session.StartConsoleCapture(eventArgs.CommandGuid, cmd);

        // Execution
        using var rdr = await cmd.ExecuteReaderAsync(cancellationToken);
        var results = new List<object>();
        GenSpecBase? genspec = null;
        if (typeof(GenSpecBase).IsAssignableFrom(typeof(TResult)))
        {
            // Handle GenSpec differently
            genspec = parseGenSpec<TResult>(results, rdr, eventArgs.CommandGuid, console);
        }
        else
        {
            var resultMembers = ContractMemberDefinition.GetFromContract(entityType, null)
                .ToDictionary(m => m.DbName.ToUpperInvariant());

            // Parse recordset
            if (!isArray && safeRead(rdr, console))
            {
                var result = getEntity(entityType, rdr, resultMembers, eventArgs.CommandGuid);
                results.Add(result);

                if (_session.StrictResultSize && safeRead(rdr, console))
                {
                    throw new InvalidOperationException("Expected a single-record result, but more than one found.");
                }
            }
            else if (isArray)
            {
                while (safeRead(rdr, console))
                {
                    var result = getEntity(entityType, rdr, resultMembers, eventArgs.CommandGuid);
                    results.Add(result);
                }
            }

            // Process sub results
            if (!console.HasError)
            {
                var rsOrder = 0;
                while (await rdr.NextResultAsync())
                {
                    matchResultset(entityType, rsOrder++, rdr, results, eventArgs.CommandGuid, console);
                }
            }
        }

        parseCommandResult(cmd, _runArgs, pars, console.GetConsoleMessages(), eventArgs, results.Count);

        // Return expected type
        if (genspec != null)
        {
            return (TResult)(object)genspec;
        }
        if (!isArray)
        {
            return (TResult?)results.SingleOrDefault();
        }

        var resultArr = (Array)Activator.CreateInstance(typeof(TResult), new object[] { results.Count })!;
        Array.Copy(results.ToArray(), resultArr, results.Count);
        return (TResult)(object)resultArr;
    }

    private class SpecDef
    {
        public Type Type { get; }
        public ContractMemberDefinition? GenProperty { get; }
        public object SpecValue { get; }
        public IDictionary<string, ContractMemberDefinition> Members { get; }

        public SpecDef(Type type)
        {
            Type = type;
            var attr = type.GetCustomAttribute<PhormSpecOfAttribute>(false);
            Members = ContractMemberDefinition.GetFromContract(type, null)
                .ToDictionary(m => m.DbName.ToUpperInvariant());
            if (attr != null)
            {
                GenProperty = Members.Values.FirstOrDefault(m => m.SourceMember?.Name.ToUpperInvariant() == attr.GenProperty.ToUpperInvariant());
                SpecValue = attr.PropertyValue;
            }
            else
            {
                SpecValue = this; // Any non-null
            }
        }
    }

    private GenSpecBase parseGenSpec<TResult>(IList<object> results, IDataReader rdr, Guid commandGuid, AbstractConsoleMessageCapture console)
    {
        var genspec = (GenSpecBase)Activator.CreateInstance(typeof(TResult))!;
        IDictionary<string, ContractMemberDefinition>? baseMembers = null;

        // Prepare models
        var specs = genspec.SpecTypes.Select(t => new SpecDef(t)).ToArray();
        if (specs.Any(s => s.GenProperty == null))
        {
            throw new InvalidOperationException("Invalid GenSpec usage. Provided type was not decorated with a PhormSpecOfAttribute referencing a valid property: " + specs.First(s => s.GenProperty == null).Type.FullName);
        }

        // Parse recordset
        var tempProps = new Dictionary<string, object?>();
        while (safeRead(rdr, console))
        {
            var included = false;
            tempProps.Clear();
            foreach (var spec in specs)
            {
                // Check Gen property for the Spec type (cached)
                if (!tempProps.TryGetValue(spec.GenProperty!.SourceMemberId!, out var propValue))
                {
                    var gen = spec.GenProperty.FromDatasource(rdr[spec.GenProperty.DbName], null);
                    propValue = gen.Value;
                    tempProps[gen.SourceMemberId!] = propValue;
                }

                if (spec.SpecValue.Equals(propValue))
                {
                    // Shape
                    var result = getEntity(spec.Type, rdr, spec.Members, commandGuid);
                    results.Add(result);
                    included = true;
                    break;
                }
            }

            if (!included)
            {
                if (!genspec.GenType.IsAbstract)
                {
                    baseMembers ??= ContractMemberDefinition.GetFromContract(genspec.GenType, null)
                        .ToDictionary(m => m.DbName.ToUpperInvariant());
                    var result = getEntity(genspec.GenType, rdr, baseMembers, commandGuid);
                    results.Add(result);
                }
                else
                {
                    // TODO: Warning events for dropped records
                }
            }
        }

        genspec.SetData(results);
        return genspec;
    }
}
