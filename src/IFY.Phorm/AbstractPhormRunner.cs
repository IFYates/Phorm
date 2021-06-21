using IFY.Phorm.Connectivity;
using IFY.Phorm.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace IFY.Phorm
{
    public abstract class AbstractPhormRunner : IPhormRunner
    {
        protected readonly IPhormDbConnectionProvider _connectionProvider;

        public AbstractPhormRunner(IPhormDbConnectionProvider connectionProvider)
        {
            _connectionProvider = connectionProvider;
        }

        #region Contract parsing

        /// <summary>
        /// Convert properties of any object to <see cref="ContractMember"/>s.
        /// </summary>
        private static ContractMember[] getMembersFromContract(object? obj, Type? contractType = null)
        {
            if (obj == null && contractType == null)
            {
                return Array.Empty<ContractMember>();
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

                // Wrap as ContractMember, if not already
                if (value is not ContractMember mem)
                {
                    if (!hasContract)
                    {
                        mem = ContractMember.In(prop.Name, value);
                    }
                    else if (!prop.CanWrite)
                    {
                        mem = ContractMember.In(prop.Name, value, prop);
                    }
                    else if (prop.CanRead)
                    {
                        mem = ContractMember.InOut(prop.Name, value, prop);
                    }
                    else
                    {
                        mem = ContractMember.Out<object>(prop.Name, prop);
                    }
                }
                members.Add(mem);

                mem.ResolveAttributes(obj);

                // Check for DataMemberAttribute
                var dmAttr = mem.Attributes.OfType<DataMemberAttribute>().SingleOrDefault();
                if (dmAttr != null)
                {
                    mem.Name = dmAttr.Name ?? mem.Name;

                    // Primitives are never "missing", so only check null
                    if (dmAttr.IsRequired && mem.Value == null)
                    {
                        throw new ArgumentNullException(mem.Name, $"Parameter {mem.Name} for contract {contractType.FullName} is required but was null");
                    }
                }
            }

            if (obj != null)
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
                }
                members.Add(retPar ?? ContractMember.RetVal());
            }

            return members.ToArray();
        }

        private static int parseCommandResult(IAsyncDbCommand cmd, object? contract, ContractMember[] members)
        {
            // Update parameters for output values
            var returnValue = 0;
            foreach (IDataParameter param in cmd.Parameters)
            {
                if (param.Direction == ParameterDirection.Output || param.Direction == ParameterDirection.InputOutput)
                {
                    var mem = members.SingleOrDefault(a => a.Name == param.ParameterName[1..]);
                    mem?.FromDatasource(param.Value); // NOTE: Always given as VARCHAR
                    mem?.SourceProperty?.SetValue(contract, mem.Value);
                }
                else if (param.Direction == ParameterDirection.ReturnValue)
                {
                    var mem = members.SingleOrDefault(a => a.Direction == ParameterDirection.ReturnValue);
                    mem?.SetValue((int?)param.Value ?? 0);
                    returnValue = (int?)mem?.Value ?? 0;
                }
            }
            return returnValue;
        }

        private static (string? schema, string actionName) getContractAttribute(Type type)
        {
            var pcAttr = type.GetCustomAttribute<PhormContractAttribute>(false);
            if (pcAttr?.Name != null)
            {
                return (pcAttr.Namespace, pcAttr.Name ?? string.Empty);
            }

            var contractName = type.Name;
            if (type.IsInterface && contractName.Length > 0 && contractName[0] == 'I')
            {
                contractName = contractName[1..];
            }
            var schemaName = pcAttr?.Namespace;

            if (pcAttr == null)
            {
                var dcAttr = type.GetCustomAttribute<DataContractAttribute>(false);
                if (dcAttr != null)
                {
                    contractName = dcAttr.Name ?? contractName;
                    schemaName = dcAttr.Namespace;
                }
            }

            return (schemaName, contractName);
        }

        #endregion Contract parsing

        #region Execution

        private IAsyncDbCommand startCommand(string? schema, string actionName, ContractMember[] members)
        {
            var conn = _connectionProvider.GetConnection(GetConnectionName());
            schema = schema?.Length > 0 ? schema : conn.DefaultSchema;
            var cmd = CreateCommand(conn, schema, actionName);

            // Convert to database parameters
            foreach (var mem in members)
            {
                var param = mem.ToDataParameter(cmd);
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

            for (var i = 0; i < rdr.FieldCount; ++i)
            {
                var col = rdr.GetName(i).ToLower();
                if (members.TryGetValue(col, out var mem))
                {
                    mem.ResolveAttributes(entity);
                    mem.FromDatasource(rdr.GetValue(i));
                    mem.SourceProperty?.SetValue(entity, mem.Value);
                }
                else
                {
                    // TODO: Warnings for unexpected columns
                }
            }

            // TODO: Must apply non secure columns first
            // TODO: Warnings for missing expected columns

            return entity;
        }

        #endregion Execution

        protected abstract string? GetConnectionName();

        protected virtual IAsyncDbCommand CreateCommand(IPhormDbConnection connection, string schema, string actionName)
        {
            var cmd = connection.CreateCommand();
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = $"[{schema}].[{actionName}]";
            return cmd;
        }

        #region All

        public TResultContract[] All<TResultContract>(string actionName, object? args = null)
            where TResultContract : new()
            => AllAsync<TResultContract>(actionName, args).GetAwaiter().GetResult();

        public TResultContract[] All<TResultContract, TActionContract>(object? args = null)
            where TResultContract : new()
            => AllAsync<TResultContract, TActionContract>(args).GetAwaiter().GetResult();

        public async Task<TResultContract[]> AllAsync<TResultContract>(string actionName, object? args = null, CancellationToken? cancellationToken = null)
            where TResultContract : new()
        {
            var pars = getMembersFromContract(args);
            using var cmd = startCommand(null, actionName, pars);
            var results = await readAll<TResultContract>(cmd, cancellationToken);
            parseCommandResult(cmd, args, pars);
            return results;
        }

        public async Task<TResultContract[]> AllAsync<TResultContract, TActionContract>(object? args = null, CancellationToken? cancellationToken = null)
            where TResultContract : new()
        {
            var (schema, actionName) = getContractAttribute(typeof(TActionContract));
            var pars = getMembersFromContract(args, typeof(TActionContract));
            using var cmd = startCommand(schema, actionName, pars);
            var results = await readAll<TResultContract>(cmd, cancellationToken);
            parseCommandResult(cmd, args, pars);
            return results;
        }

        #endregion All

        #region Call

        public int Call(string actionName, object? args = null)
            => CallAsync(actionName, args).GetAwaiter().GetResult();

        public int Call<TActionContract>(object? args = null)
            => CallAsync<TActionContract>(args).GetAwaiter().GetResult();

        public int Call<TActionContract>(TActionContract? contract)
            => CallAsync(contract).GetAwaiter().GetResult();

        public async Task<int> CallAsync(string actionName, object? args = null, CancellationToken? cancellationToken = null)
        {
            var pars = getMembersFromContract(args);
            using var cmd = startCommand(null, actionName ?? string.Empty, pars);
            await doExec(cmd, cancellationToken);
            return parseCommandResult(cmd, args, pars);
        }

        public async Task<int> CallAsync<TActionContract>(object? args = null, CancellationToken? cancellationToken = null)
        {
            var (schema, actionName) = getContractAttribute(typeof(TActionContract));
            var pars = getMembersFromContract(args, typeof(TActionContract));
            using var cmd = startCommand(schema, actionName, pars);
            await doExec(cmd, cancellationToken);
            return parseCommandResult(cmd, args, pars);
        }

        public Task<int> CallAsync<TActionContract>(TActionContract? contract, CancellationToken? cancellationToken = null)
            => CallAsync<TActionContract>((object?)contract, cancellationToken);

        #endregion Call

        #region Single

        public TResultContract? Single<TResultContract>(string actionName, object? args = null)
            where TResultContract : new()
            => SingleAsync<TResultContract>(actionName, args).GetAwaiter().GetResult();

        public TResultContract? Single<TResultContract, TActionContract>(object? args = null)
            where TResultContract : new()
            => SingleAsync<TResultContract, TActionContract>(args).GetAwaiter().GetResult();

        public async Task<TResultContract?> SingleAsync<TResultContract>(string actionName, object? args = null, CancellationToken? cancellationToken = null) where TResultContract : new()
        {
            var pars = getMembersFromContract(args);
            using var cmd = startCommand(null, actionName, pars);
            var result = await readSingle<TResultContract>(cmd, cancellationToken);
            parseCommandResult(cmd, args, pars);
            return result;
        }

        public async Task<TResultContract?> SingleAsync<TResultContract, TActionContract>(object? args = null, CancellationToken? cancellationToken = null) where TResultContract : new()
        {
            var (schema, actionName) = getContractAttribute(typeof(TActionContract));
            var pars = getMembersFromContract(args, typeof(TActionContract));
            using var cmd = startCommand(schema, actionName, pars);
            var result = await readSingle<TResultContract>(cmd, cancellationToken);
            parseCommandResult(cmd, args, pars);
            return result;
        }

        #endregion Single

        #region Transactions

        public abstract bool SupportsTransactions { get; }

        public abstract bool IsInTransaction { get; }

        public abstract ITransactedPhormRunner BeginTransaction();

        #endregion Transactions
    }
}
