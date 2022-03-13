using IFY.Phorm.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IFY.Phorm
{
    internal class PhormContractRunner<TActionContract> : IPhormContractRunner<TActionContract>
        where TActionContract : IPhormContract
    {
        private readonly AbstractPhormSession _session;
        private readonly string? _schema;
        private readonly string _objectName;
        private readonly DbObjectType _objectType;
        private readonly object? _runArgs;

        public PhormContractRunner(AbstractPhormSession runner, string? objectName, DbObjectType objectType, object? args)
            : this(runner, typeof(TActionContract), objectName, objectType, args)
        {
        }
        public PhormContractRunner(AbstractPhormSession runner, Type contractType, string? objectName, DbObjectType objectType, object? args)
        {
            _session = runner;

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

        private IAsyncDbCommand startCommand(ContractMember[] members)
        {
            var cmd = _session.CreateCommand(_schema, _objectName, _objectType);

            // Build WHERE clause from members
#if NETSTANDARD || NETCOREAPP
            if (_objectType.IsOneOf(DbObjectType.Table, DbObjectType.View))
#else
            if (_objectType is DbObjectType.Table or DbObjectType.View)
#endif
            {
                var sb = new StringBuilder();
#if NETSTANDARD || NETCOREAPP
                foreach (var memb in members.Where(m => m.Direction.IsOneOf(ParameterDirection.Input, ParameterDirection.InputOutput))
#else
                foreach (var memb in members.Where(m => m.Direction is ParameterDirection.Input or ParameterDirection.InputOutput)
#endif
                    .Where(m => m.Value != null && m.Value != DBNull.Value))
                {
                    // TODO: Ignore members without value
                    if (sb.Length > 0)
                    {
                        sb.Append(" AND ");
                    }
                    sb.AppendFormat("[{0}] = @{0}", memb.Name);
                }
                if (sb.Length > 0)
                {
                    cmd.CommandText += $" WHERE {sb}";
                }
            }

            // Convert to database parameters
            foreach (var memb in members)
            {
                var param = memb.ToDataParameter(cmd);
                if (param.Direction != ParameterDirection.Input || (param.Value != null && param.Value != DBNull.Value))
                {
                    cmd.Parameters.Add(param);
                }
            }

            return cmd;
        }

        private static void matchResultset(Type entityType, int order, IDataReader rdr, IEnumerable<object> parents)
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
            var recordMembers = ContractMember.GetMembersFromContract(null, recordType)
                .ToDictionary(m => m.Name.ToLower());
            while (rdr.Read())
            {
                var res = getEntity(recordType, rdr, recordMembers);
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

        private static object getEntity(Type entityType, IDataReader rdr, Dictionary<string, ContractMember> members)
        {
            var entity = Activator.CreateInstance(entityType) ?? new object();

            // Resolve member values
            var secureMembers = new Dictionary<ContractMember, int>();
            for (var i = 0; i < rdr.FieldCount; ++i)
            {
                var fieldName = rdr.GetName(i).ToUpperInvariant();
                if (members.TryGetValue(fieldName, out var memb))
                {
                    memb.ResolveAttributes(entity, out var isSecure);
                    if (isSecure)
                    {
                        // Defer secure members until after non-secure, to allow for authenticator properties
                        secureMembers[memb] = i;
                    }
                    else
                    {
                        setEntityValue(entity, memb, rdr, i);
                    }
                }
                else
                {
                    // TODO: Warnings for unexpected columns
                }
            }

            // Apply secure values
            foreach (var kvp in secureMembers)
            {
                setEntityValue(entity, kvp.Key, rdr, kvp.Value);
            }

            // TODO: Warnings for missing expected columns

            return entity;

            static void setEntityValue(object entity, ContractMember memb, IDataReader rdr, int idx)
            {
                memb.FromDatasource(rdr.GetValue(idx));
                try
                {
                    memb.SourceProperty?.SetValue(entity, memb.Value);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed set to property {memb.SourceProperty?.Name ?? memb.Name}", ex);
                }
            }
        }

        private static int parseCommandResult(IAsyncDbCommand cmd, object? contract, ContractMember[] members)
        {
            // Update parameters for output values
            var returnValue = 0;
            foreach (IDataParameter param in cmd.Parameters)
            {
#if NETSTANDARD || NETCOREAPP
                if (contract != null && param.Direction.IsOneOf(ParameterDirection.Output, ParameterDirection.InputOutput))
#else
                if (contract != null && param.Direction is ParameterDirection.Output or ParameterDirection.InputOutput)
#endif
                {
                    var memb = members.Single(a => a.Name == param.ParameterName[1..]);
                    memb.FromDatasource(param.Value); // NOTE: Always given as VARCHAR
                    var prop = memb.SourceProperty;
                    if (prop != null && prop.ReflectedType?.IsAssignableFrom(contract.GetType()) == false)
                    {
                        prop = contract.GetType().GetProperty(prop.Name);
                    }
                    prop?.SetValue(contract, memb.Value);
                }
                else if (param.Direction == ParameterDirection.ReturnValue)
                {
                    returnValue = (int?)param.Value ?? 0;
                    foreach (var memb in members.Where(a => a.Direction == ParameterDirection.ReturnValue))
                    {
                        memb.SetValue(returnValue);
                    }
                }
            }
            return returnValue;
        }

        #endregion Execution

        public async Task<int> CallAsync(CancellationToken? cancellationToken = null)
        {
            // Prepare execution
            var pars = ContractMember.GetMembersFromContract(_runArgs, typeof(TActionContract));
            using var cmd = startCommand(pars);

            // Execution
            using var rdr = await cmd.ExecuteReaderAsync(cancellationToken ?? CancellationToken.None);
            if (_session.StrictResultSize && rdr.Read())
            {
                throw new InvalidOperationException("Non-result request returned a result.");
            }

            return parseCommandResult(cmd, _runArgs, pars);
        }

        public TResult? Get<TResult>()
            where TResult : class
            => GetAsync<TResult>(null).GetAwaiter().GetResult();
        public async Task<TResult?> GetAsync<TResult>(CancellationToken? cancellationToken = null)
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
            var pars = ContractMember.GetMembersFromContract(_runArgs, typeof(TActionContract));
            using var cmd = startCommand(pars);
            var results = new List<object>();
            using var rdr = await cmd.ExecuteReaderAsync(cancellationToken ?? CancellationToken.None);

            // Handle GenSpec differently
            if (typeof(GenSpecBase).IsAssignableFrom(typeof(TResult)))
            {
                results.Add(parseGenSpec<TResult>(rdr));

                // TODO: handle subresults
            }
            else
            {
                var resultMembers = ContractMember.GetMembersFromContract(null, entityType)
                    .ToDictionary(m => m.Name.ToUpperInvariant());

                // Parse recordset
                if (!isArray && rdr.Read())
                {
                    var result = getEntity(entityType, rdr, resultMembers);
                    results.Add(result);

                    if (_session.StrictResultSize && rdr.Read())
                    {
                        throw new InvalidOperationException("Expected a single-record result, but more than one found.");
                    }
                }
                else
                {
                    while (rdr.Read())
                    {
                        var result = getEntity(entityType, rdr, resultMembers);
                        results.Add(result);
                    }
                }

                // Process sub results
                var rsOrder = 0;
                while (await rdr.NextResultAsync())
                {
                    matchResultset(entityType, rsOrder++, rdr, results);
                }
            }

            parseCommandResult(cmd, _runArgs, pars);

            // Return expected type
            if (!isArray)
            {
                return (TResult?)results.SingleOrDefault();
            }

            var resultArr = (Array)Activator.CreateInstance(typeof(TResult), new object[] { results.Count })!;
            Array.Copy(results.ToArray(), resultArr, results.Count);
            return (TResult)(object)resultArr;
        }

        private TResult parseGenSpec<TResult>(IDataReader rdr)
        {
            var genspec = (GenSpecBase)Activator.CreateInstance(typeof(TResult))!;

            // Prepare models
            var specs = genspec.SpecTypes.ToDictionary(t => t, t => (Attribute: t.GetCustomAttribute<PhormSpecOfAttribute>(false), Members: ContractMember.GetMembersFromContract(null, t).ToDictionary(m => m.Name.ToUpperInvariant())));
            if (specs.Values.Any(s => s.Attribute == null))
            {
                throw new InvalidOperationException("Invalid GenSpec usage. Provided type was not decorated with a PhormSpecOfAttribute: " + specs.First(s => s.Value.Attribute == null).Key.FullName);
            }

            // Parse recordset
            var results = new List<object>();
            var tempProps = new Dictionary<string, object?>();
            while (rdr.Read())
            {
                tempProps.Clear();
                foreach (var spec in specs)
                {
                    // Check Gen property for the Spec type (cached)
                    var attr = spec.Value.Attribute;
                    if (!tempProps.TryGetValue(attr.GenProperty, out var propValue))
                    {
                        var propIdx = rdr.GetOrdinal(attr.GenProperty);
                        propValue = propIdx>=0 ? rdr.GetValue(propIdx) : null;
                        tempProps[attr.GenProperty] = propValue;
                    }

                    if (attr.PropertyValue.Equals(propValue))
                    {
                        // Shape
                        var result = getEntity(spec.Key, rdr, spec.Value.Members);
                        results.Add(result);
                        break;
                    }
                }
            }

            genspec.SetData(results);
            return (TResult)(object)genspec;
        }
    }
}
