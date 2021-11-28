using IFY.Phorm.Connectivity;
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
                if (param.Direction is ParameterDirection.Output or ParameterDirection.InputOutput)
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

        private static (string? schema, string objectName, DbObjectType objectType) getContractAttribute(Type type)
        {
            var pcAttr = type.GetCustomAttribute<PhormContractAttribute>(false);
            if (pcAttr?.Name != null)
            {
                return (pcAttr.Namespace, pcAttr.Name, pcAttr.Target);
            }

            var schemaName = pcAttr?.Namespace;
            var contractName = type.Name;
            var objectType = pcAttr?.Target ?? DbObjectType.StoredProcedure;

            if (type.IsInterface && contractName.Length > 0 && contractName[0] == 'I')
            {
                contractName = contractName[1..];
            }

            if (pcAttr == null)
            {
                var dcAttr = type.GetCustomAttribute<DataContractAttribute>(false);
                if (dcAttr != null)
                {
                    schemaName = dcAttr.Namespace;
                    contractName = dcAttr.Name ?? contractName;
                }
            }

            return (schemaName, contractName, objectType);
        }

        #endregion Contract parsing

        #region Execution

        private IAsyncDbCommand startCommand(string? schema, string objectName, ContractMember[] members, DbObjectType objectType)
        {
            var conn = _connectionProvider.GetConnection(GetConnectionName());
            schema = schema?.Length > 0 ? schema : conn.DefaultSchema;
            var cmd = CreateCommand(conn, schema, objectName, objectType);

            // Build WHERE clause from members
            if (objectType is DbObjectType.Table or DbObjectType.View)
            {
                var sb = new StringBuilder();
                foreach (var memb in members.Where(m => m.Direction is ParameterDirection.Input or ParameterDirection.InputOutput))
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
                    cmd.CommandText += " WHERE " + sb.ToString();
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

        #region Connection

        protected abstract string? GetConnectionName();

        protected virtual IAsyncDbCommand CreateCommand(IPhormDbConnection connection, string schema, string objectName, DbObjectType objectType)
        {
            var cmd = connection.CreateCommand();

            if (objectType is DbObjectType.Table or DbObjectType.View)
            {
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = $"SELECT * FROM [{schema}].[{objectName}]";
                return cmd;
            }

            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = $"[{schema}].[{objectName}]";
            return cmd;
        }

        #endregion Connection

        #region Call

        public int Call(string objectName, object? args = null)
            => CallAsync(objectName, args).GetAwaiter().GetResult();

        public int Call<TActionContract>(object? args = null)
            where TActionContract : IPhormContract
            => CallAsync<TActionContract>(args).GetAwaiter().GetResult();

        public int Call<TActionContract>(TActionContract? contract)
            where TActionContract : IPhormContract
            => CallAsync<TActionContract>((object?)contract, null).GetAwaiter().GetResult();

        public async Task<int> CallAsync(string objectName, object? args = null, CancellationToken? cancellationToken = null)
        {
            var pars = getMembersFromContract(args);
            using var cmd = startCommand(null, objectName ?? string.Empty, pars, DbObjectType.StoredProcedure);
            await doExec(cmd, cancellationToken);
            return parseCommandResult(cmd, args, pars);
        }

        public async Task<int> CallAsync<TActionContract>(object? args = null, CancellationToken? cancellationToken = null)
            where TActionContract : IPhormContract
        {
            var (schema, objectName, objectType) = getContractAttribute(typeof(TActionContract));
            var pars = getMembersFromContract(args, typeof(TActionContract));
            using var cmd = startCommand(schema, objectName, pars, objectType);
            await doExec(cmd, cancellationToken);
            return parseCommandResult(cmd, args, pars);
        }

        public Task<int> CallAsync<TActionContract>(TActionContract? contract, CancellationToken? cancellationToken = null)
            where TActionContract : IPhormContract
            => CallAsync<TActionContract>((object?)contract, cancellationToken);

        #endregion Call

        #region Single

        public TResultContract? One<TResultContract>(string objectName, object? args = null, DbObjectType objectType = DbObjectType.StoredProcedure)
            where TResultContract : new()
            => OneAsync<TResultContract>(objectName, args, objectType).GetAwaiter().GetResult();

        public TResultContract? One<TResultContract, TActionContract>(object? args = null)
            where TResultContract : new()
            where TActionContract : IPhormContract
            => OneAsync<TResultContract, TActionContract>(args).GetAwaiter().GetResult();

        public async Task<TResultContract?> OneAsync<TResultContract>(string objectName, object? args = null, DbObjectType objectType = DbObjectType.StoredProcedure, CancellationToken? cancellationToken = null)
            where TResultContract : new()
        {
            var pars = getMembersFromContract(args);
            using var cmd = startCommand(null, objectName, pars, objectType);
            var result = await readSingle<TResultContract>(cmd, cancellationToken);
            parseCommandResult(cmd, args, pars);
            return result;
        }

        public async Task<TResultContract?> OneAsync<TResultContract, TActionContract>(object? args = null, CancellationToken? cancellationToken = null)
            where TResultContract : new()
            where TActionContract : IPhormContract
        {
            var (schema, objectName, objectType) = getContractAttribute(typeof(TActionContract));
            var pars = getMembersFromContract(args, typeof(TActionContract));
            using var cmd = startCommand(schema, objectName, pars, objectType);
            var result = await readSingle<TResultContract>(cmd, cancellationToken);
            parseCommandResult(cmd, args, pars);
            return result;
        }

        #endregion Single

        #region Many

        public TResultContract[] Many<TResultContract>(string objectName, object? args = null, DbObjectType objectType = DbObjectType.StoredProcedure)
            where TResultContract : new()
            => ManyAsync<TResultContract>(objectName, args, objectType).GetAwaiter().GetResult();

        public TResultContract[] Many<TResultContract, TActionContract>(object? args = null)
            where TResultContract : new()
            where TActionContract : IPhormContract
            => ManyAsync<TResultContract, TActionContract>(args).GetAwaiter().GetResult();

        public async Task<TResultContract[]> ManyAsync<TResultContract>(string objectName, object? args = null, DbObjectType objectType = DbObjectType.StoredProcedure, CancellationToken? cancellationToken = null)
            where TResultContract : new()
        {
            var pars = getMembersFromContract(args);
            using var cmd = startCommand(null, objectName, pars, objectType);
            var results = await readAll<TResultContract>(cmd, cancellationToken);
            parseCommandResult(cmd, args, pars);
            return results;
        }

        public async Task<TResultContract[]> ManyAsync<TResultContract, TActionContract>(object? args = null, CancellationToken? cancellationToken = null)
            where TResultContract : new()
            where TActionContract : IPhormContract
        {
            var (schema, objectName, objectType) = getContractAttribute(typeof(TActionContract));
            var pars = getMembersFromContract(args, typeof(TActionContract));
            using var cmd = startCommand(schema, objectName, pars, objectType);
            var results = await readAll<TResultContract>(cmd, cancellationToken);
            parseCommandResult(cmd, args, pars);
            return results;
        }

        #endregion Many

        #region Transactions

        public abstract bool SupportsTransactions { get; }

        public abstract bool IsInTransaction { get; }

        public abstract ITransactedPhormRunner BeginTransaction();

        #endregion Transactions
    }
}
