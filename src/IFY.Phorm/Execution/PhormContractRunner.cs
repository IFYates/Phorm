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
    public class PhormContractRunner<TActionContract> : IPhormContractRunner<TActionContract>
        where TActionContract : IPhormContract
    {
        private readonly AbstractPhormSession _session;
        private readonly string? _schema;
        private readonly string _objectName;
        private readonly DbObjectType _objectType;

        public PhormContractRunner(AbstractPhormSession session, string? objectName, DbObjectType objectType)
        {
            _session = session;

            var contractType = typeof(TActionContract);
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
        }

        #region Execution

        private IAsyncDbCommand startCommand(ContractMember[] members)
        {
            var cmd = _session.CreateCommand(_schema, _objectName, _objectType);

            // Build WHERE clause from members
            if (_objectType is DbObjectType.Table or DbObjectType.View)
            {
                var sb = new StringBuilder();
                foreach (var memb in members.Where(m => m.Direction is ParameterType.Input or ParameterType.InputOutput)
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
                cmd.Parameters.Add(param);
            }

            return cmd;
        }

        private static async Task doExec(IAsyncDbCommand cmd, CancellationToken? cancellationToken)
        {
            using var rdr = await cmd.ExecuteReaderAsync(cancellationToken ?? CancellationToken.None);
            if (rdr.Read())
            {
                throw new InvalidOperationException("Non-result request returned a result.");
            }
        }
        private static async Task<TResult[]> readAll<TResult>(IAsyncDbCommand cmd, CancellationToken? cancellationToken)
            where TResult : new()
        {
            var results = new List<TResult>();
            var resultMembers = ContractMember.GetMembersFromContract(null, typeof(TResult))
                .ToDictionary(m => m.Name.ToLower());

            // Parse first resultset
            using var rdr = await cmd.ExecuteReaderAsync(cancellationToken ?? CancellationToken.None);
            while (rdr.Read())
            {
                var res = getEntity<TResult>(rdr, resultMembers);
                results.Add(res);
            }

            // Process sub results
            int rsOrder = 0;
            while (await rdr.NextResultAsync())
            {
                matchResultset(rsOrder++, rdr, results);
            }

            return results.ToArray();
        }
        private static async Task<TResult?> readSingle<TResult>(IAsyncDbCommand cmd, CancellationToken? cancellationToken)
            where TResult : new()
        {
            var resultMembers = ContractMember.GetMembersFromContract(null, typeof(TResult))
                .ToDictionary(m => m.Name.ToLower());

            // Parse first record of result
            TResult? res = default;
            using var rdr = await cmd.ExecuteReaderAsync(cancellationToken ?? CancellationToken.None);
            if (rdr.Read())
            {
                res = getEntity<TResult>(rdr, resultMembers);
                if (rdr.Read())
                {
                    throw new InvalidOperationException("Expected a single-record result, but more than one found.");
                }
            }

            // Process sub results
            int rsOrder = 0;
            while (await rdr.NextResultAsync())
            {
                matchResultset(rsOrder++, rdr, new[] { res });
            }

            return res;
        }

        private static void matchResultset<TResult>(int order, IDataReader rdr, IEnumerable<TResult> parents)
        {
            // Find resultset target
            var rsProp = typeof(TResult).GetProperties()
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

        private static TResult getEntity<TResult>(IDataReader rdr, Dictionary<string, ContractMember> members)
            where TResult : new()
        {
            return (TResult)getEntity(typeof(TResult), rdr, members);
        }
        private static object getEntity(Type entityType, IDataReader rdr, Dictionary<string, ContractMember> members)
        {
            var entity = Activator.CreateInstance(entityType) ?? new object();

            // Resolve member values
            var secureMembers = new Dictionary<ContractMember, int>();
            for (var i = 0; i < rdr.FieldCount; ++i)
            {
                var fieldName = rdr.GetName(i).ToLower();
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
                        memb.FromDatasource(rdr.GetValue(i));
                        memb.SourceProperty?.SetValue(entity, memb.Value);
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
                var memb = kvp.Key;
                memb.FromDatasource(rdr.GetValue(kvp.Value));
                memb.SourceProperty?.SetValue(entity, memb.Value);
            }

            // TODO: Warnings for missing expected columns

            return entity;
        }

        private static int parseCommandResult(IAsyncDbCommand cmd, object? contract, ContractMember[] members, string consoleOutput)
        {
            // Update parameters for output values
            var returnValue = 0;
            foreach (IDataParameter param in cmd.Parameters)
            {
                if (contract != null && param.Direction is ParameterDirection.Output or ParameterDirection.InputOutput)
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
                    foreach (var memb in members.Where(a => a.Direction == ParameterType.ReturnValue))
                    {
                        memb.SetValue(returnValue);
                    }
                }
            }

            // TODO: only if needed
            // Support console capture
            var consoleProp = contract?.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.PropertyType == typeof(ContractMember<string>))
                .Select(p => p.GetValue(contract) as ContractMember<string>)
                .FirstOrDefault(v => v?.Direction == ParameterType.Console);
            consoleProp?.SetValue(consoleOutput);

            return returnValue;
        }

        #endregion Execution

        public int Call(object? args = null)
            => CallAsync(args).GetAwaiter().GetResult();
        public async Task<int> CallAsync(object? args = null, CancellationToken? cancellationToken = null)
        {
            var pars = ContractMember.GetMembersFromContract(args, typeof(TActionContract));
            using var cmd = startCommand(pars);
            var console = _session.StartConsoleCapture(cmd);
            await doExec(cmd, cancellationToken);
            return parseCommandResult(cmd, args, pars, console.Complete());
        }

        public int Call(TActionContract args)
            => CallAsync(args, null).GetAwaiter().GetResult();
        public async Task<int> CallAsync(TActionContract args, CancellationToken? cancellationToken = null)
        {
            var pars = ContractMember.GetMembersFromContract(args, typeof(TActionContract));
            using var cmd = startCommand(pars);
            var console = _session.StartConsoleCapture(cmd);
            await doExec(cmd, cancellationToken);
            return parseCommandResult(cmd, args, pars, console.Complete());
        }

        public TResult[] Many<TResult>(object? args = null)
            where TResult : new()
            => ManyAsync<TResult>(args, null).GetAwaiter().GetResult();
        public async Task<TResult[]> ManyAsync<TResult>(object? args = null, CancellationToken? cancellationToken = null)
            where TResult : new()
        {
            var pars = ContractMember.GetMembersFromContract(args, typeof(TActionContract));
            using var cmd = startCommand(pars);
            var console = _session.StartConsoleCapture(cmd);
            var result = await readAll<TResult>(cmd, cancellationToken);
            parseCommandResult(cmd, args, pars, console.Complete());
            return result;
        }

        public TResult[] Many<TResult>(TActionContract args)
            where TResult : new()
            => ManyAsync<TResult>(args, null).GetAwaiter().GetResult();
        public async Task<TResult[]> ManyAsync<TResult>(TActionContract args, CancellationToken? cancellationToken = null) where TResult : new()
        {
            var pars = ContractMember.GetMembersFromContract(args, typeof(TActionContract));
            using var cmd = startCommand(pars);
            var console = _session.StartConsoleCapture(cmd);
            var result = await readAll<TResult>(cmd, cancellationToken);
            parseCommandResult(cmd, args, pars, console.Complete());
            return result;
        }

        public TResult? One<TResult>(object? args = null)
            where TResult : new()
            => OneAsync<TResult>(args, null).GetAwaiter().GetResult();
        public async Task<TResult?> OneAsync<TResult>(object? args = null, CancellationToken? cancellationToken = null)
            where TResult : new()
        {
            var pars = ContractMember.GetMembersFromContract(args, typeof(TActionContract));
            using var cmd = startCommand(pars);
            var console = _session.StartConsoleCapture(cmd);
            var result = await readSingle<TResult>(cmd, cancellationToken);
            parseCommandResult(cmd, args, pars, console.Complete());
            return result;
        }

        public TResult? One<TResult>(TActionContract args)
            where TResult : new()
            => OneAsync<TResult>(args, null).GetAwaiter().GetResult();
        public async Task<TResult?> OneAsync<TResult>(TActionContract args, CancellationToken? cancellationToken = null)
            where TResult : new()
        {
            var pars = ContractMember.GetMembersFromContract(args, typeof(TActionContract));
            using var cmd = startCommand(pars);
            var console = _session.StartConsoleCapture(cmd);
            var result = await readSingle<TResult>(cmd, cancellationToken);
            parseCommandResult(cmd, args, pars, console.Complete());
            return result;
        }
    }
}
