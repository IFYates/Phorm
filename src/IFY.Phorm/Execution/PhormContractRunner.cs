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
        private readonly AbstractPhormRunner _runner;
        private readonly string? _schema;
        private readonly string _objectName;
        private readonly DbObjectType _objectType;

        public PhormContractRunner(AbstractPhormRunner runner, string? objectName, DbObjectType objectType)
        {
            _runner = runner;

            var contractType = typeof(TActionContract);
            var contractName = contractType.Name;
            if (contractType.IsInterface && contractName[0] == 'I')
            {
                contractName = contractName[1..];
            }
            if (contractType == typeof(IPhormContract))
            {
                _objectName = objectName ?? throw new ArgumentNullException(nameof(objectName));
            }
            else
            {
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
            var cmd = _runner.CreateCommand(_schema, _objectName, _objectType);

            // Build WHERE clause from members
            if (_objectType is DbObjectType.Table or DbObjectType.View)
            {
                var sb = new StringBuilder();
                foreach (var memb in members.Where(m => m.Direction is ParameterDirection.Input or ParameterDirection.InputOutput)
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
        private static async Task<TResult?> readSingle<TResult>(IAsyncDbCommand cmd, CancellationToken? cancellationToken)
            where TResult : new()
        {
            var results = new List<TResult>();
            var resultMembers = getMembersFromContract(null, typeof(TResult))
                .ToDictionary(m => m.Name.ToLower());

            // Parse first record of result
            using var rdr = await cmd.ExecuteReaderAsync(cancellationToken ?? CancellationToken.None);
            if (rdr.Read())
            {
                var res = getEntity<TResult>(rdr, resultMembers);
                if (rdr.Read())
                {
                    throw new InvalidOperationException("Expected a single-record result, but more than one found.");
                }
                return res;
            }

            if (await rdr.NextResultAsync())
            {
                throw new InvalidOperationException("Expected a single-record result, but more than one found.");
            }
            return default;
        }
        private static async Task<TResult[]> readAll<TResult>(IAsyncDbCommand cmd, CancellationToken? cancellationToken)
            where TResult : new()
        {
            var results = new List<TResult>();
            var resultMembers = getMembersFromContract(null, typeof(TResult))
                .ToDictionary(m => m.Name.ToLower());

            // Parse first resultset
            using var rdr = await cmd.ExecuteReaderAsync(cancellationToken ?? CancellationToken.None);
            while (rdr.Read())
            {
                var res = getEntity<TResult>(rdr, resultMembers);
                results.Add(res);
            }

            if (await rdr.NextResultAsync())
            {
                // TODO: handle child resultset
            }

            return results.ToArray();
        }

        private static TResult getEntity<TResult>(IDataReader rdr, Dictionary<string, ContractMember> members)
            where TResult : new()
        {
            var entity = new TResult();

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

        #endregion Execution

        #region Contract parsing

        /// <summary>
        /// Convert properties of any object to <see cref="ContractMember"/>s.
        /// </summary>
        private static ContractMember[] getMembersFromContract(object? obj, Type? contractType)
        {
            if (contractType == typeof(IPhormContract))
            {
                contractType = null;
            }
            if (obj == null && contractType == null)
            {
                return addReturnValue(obj, new()).ToArray();
            }

            var objType = obj?.GetType();
            var hasContract = contractType != null;
            var isContract = contractType != null && (obj == null || contractType.IsAssignableFrom(objType));
            contractType ??= objType ?? throw new NullReferenceException();

            var props = contractType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            var members = new List<ContractMember>(props.Length);
            foreach (var prop in props)
            {
                PropertyInfo? objProp = null;
                object? value;
                if (!isContract)
                {
                    // Allow use of non-contract
                    objProp = objType?.GetProperty(prop.Name, BindingFlags.Instance | BindingFlags.Public);
                    value = obj != null && objProp?.CanRead == true ? objProp.GetValue(obj) : null;
                }
                else
                {
                    value = obj != null && prop.CanRead ? prop.GetValue(obj) : null;
                }

                if (prop.GetCustomAttribute<IgnoreDataMemberAttribute>() != null)
                {
                    continue;
                }

                // Wrap as ContractMember, if not already
                if (value is not ContractMember memb)
                {
                    if (!hasContract)
                    {
                        memb = ContractMember.In(prop.Name, value);
                    }
                    else if (!prop.CanWrite)
                    {
                        memb = ContractMember.In(prop.Name, value, prop);
                    }
                    else if (prop.CanRead)
                    {
                        memb = ContractMember.InOut(prop.Name, value, prop);
                    }
                    else
                    {
                        memb = ContractMember.Out<object>(prop.Name, prop);
                    }
                }
                else
                {
                    memb.Name = prop.Name;
                }

                members.Add(memb);
                memb.ResolveAttributes(obj, out _);

                // Check for DataMemberAttribute
                var dmAttr = memb.Attributes.OfType<DataMemberAttribute>().SingleOrDefault();
                if (dmAttr != null)
                {
                    memb.Name = dmAttr.Name ?? memb.Name;

                    // Primitives are never "missing", so only check null
                    if (dmAttr.IsRequired && memb.Value == null)
                    {
                        throw new ArgumentNullException(memb.Name, $"Parameter {memb.Name} for contract {contractType.FullName} is required but was null");
                    }
                }
            }

            return addReturnValue(obj, members).ToArray();

            static IList<ContractMember> addReturnValue(object? obj, List<ContractMember> members)
            {
                // Always want the return value on action contracts
                var retPar = (ContractMember<int>?)members.FirstOrDefault(p => p.Direction == ParameterDirection.ReturnValue);
                if (retPar == null)
                {
                    // Allow for a return value on the object
                    retPar = obj?.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)
                        .Where(p => p.PropertyType == typeof(ContractMember<int>))
                        .Select(p => p.GetValue(obj) as ContractMember<int>)
                        .FirstOrDefault(v => v?.Direction == ParameterDirection.ReturnValue);
                    members.Add(retPar ?? ContractMember.RetVal());
                }
                return members;
            }
        }

        private static int parseCommandResult(IAsyncDbCommand cmd, object? contract, ContractMember[] members)
        {
            // Update parameters for output values
            var returnValue = 0;
            foreach (IDataParameter param in cmd.Parameters)
            {
                if (param.Direction is ParameterDirection.Output or ParameterDirection.InputOutput)
                {
                    var memb = members.SingleOrDefault(a => a.Name == param.ParameterName[1..]);
                    memb?.FromDatasource(param.Value); // NOTE: Always given as VARCHAR
                    memb?.SourceProperty?.SetValue(contract, memb.Value);
                }
                else if (param.Direction == ParameterDirection.ReturnValue)
                {
                    var memb = members.SingleOrDefault(a => a.Direction == ParameterDirection.ReturnValue);
                    memb?.SetValue((int?)param.Value ?? 0);
                    returnValue = (int?)memb?.Value ?? 0;
                }
            }
            return returnValue;
        }

        #endregion Contract parsing

        public int Call(object? args = null)
            => CallAsync(args).GetAwaiter().GetResult();
        public async Task<int> CallAsync(object? args = null, CancellationToken? cancellationToken = null)
        {
            var pars = getMembersFromContract(args, typeof(TActionContract));
            using var cmd = startCommand(pars);
            await doExec(cmd, cancellationToken);
            return parseCommandResult(cmd, args, pars);
        }

        public int Call(TActionContract args)
            => CallAsync(args, null).GetAwaiter().GetResult();
        public async Task<int> CallAsync(TActionContract args, CancellationToken? cancellationToken = null)
        {
            var pars = getMembersFromContract(args, typeof(TActionContract));
            using var cmd = startCommand(pars);
            await doExec(cmd, cancellationToken);
            return parseCommandResult(cmd, args, pars);
        }

        public TResult[] Many<TResult>(object? args = null)
            where TResult : new()
            => ManyAsync<TResult>(args, null).GetAwaiter().GetResult();
        public async Task<TResult[]> ManyAsync<TResult>(object? args = null, CancellationToken? cancellationToken = null)
            where TResult : new()
        {
            var pars = getMembersFromContract(args, typeof(TActionContract));
            using var cmd = startCommand(pars);
            var result = await readAll<TResult>(cmd, cancellationToken);
            parseCommandResult(cmd, args, pars);
            return result;
        }

        public TResult[] Many<TResult>(TActionContract args)
            where TResult : new()
            => ManyAsync<TResult>(args, null).GetAwaiter().GetResult();
        public async Task<TResult[]> ManyAsync<TResult>(TActionContract args, CancellationToken? cancellationToken = null) where TResult : new()
        {
            var pars = getMembersFromContract(args, typeof(TActionContract));
            using var cmd = startCommand(pars);
            var result = await readAll<TResult>(cmd, cancellationToken);
            parseCommandResult(cmd, args, pars);
            return result;
        }

        public TResult? One<TResult>(object? args = null)
            where TResult : new()
            => OneAsync<TResult>(args, null).GetAwaiter().GetResult();
        public async Task<TResult?> OneAsync<TResult>(object? args = null, CancellationToken? cancellationToken = null)
            where TResult : new()
        {
            var pars = getMembersFromContract(args, typeof(TActionContract));
            using var cmd = startCommand(pars);
            var result = await readSingle<TResult>(cmd, cancellationToken);
            parseCommandResult(cmd, args, pars);
            return result;
        }

        public TResult? One<TResult>(TActionContract args)
            where TResult : new()
            => OneAsync<TResult>(args, null).GetAwaiter().GetResult();
        public async Task<TResult?> OneAsync<TResult>(TActionContract args, CancellationToken? cancellationToken = null)
            where TResult : new()
        {
            var pars = getMembersFromContract(args, typeof(TActionContract));
            using var cmd = startCommand(pars);
            var result = await readSingle<TResult>(cmd, cancellationToken);
            parseCommandResult(cmd, args, pars);
            return result;
        }
    }
}
