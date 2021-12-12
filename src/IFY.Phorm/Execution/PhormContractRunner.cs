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
        private readonly AbstractPhormSession _runner;
        private readonly string? _schema;
        private readonly string _objectName;
        private readonly DbObjectType _objectType;

        public PhormContractRunner(AbstractPhormSession runner, string? objectName, DbObjectType objectType)
        {
            _runner = runner;

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

            if (await rdr.NextResultAsync())
            {
                // TODO: handle child resultset
            }

            return results.ToArray();
        }
        private static async Task<TResult?> readSingle<TResult>(IAsyncDbCommand cmd, CancellationToken? cancellationToken)
            where TResult : new()
        {
            var results = new List<TResult>();
            var resultMembers = ContractMember.GetMembersFromContract(null, typeof(TResult))
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

        private static int parseCommandResult(IAsyncDbCommand cmd, object? contract, ContractMember[] members)
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
                    if (members.TrySingle(a => a.Direction == ParameterDirection.ReturnValue, out var memb))
                    {
                        memb.SetValue((int?)param.Value ?? 0);
                        returnValue = (int?)memb.Value ?? 0;
                    }
                }
            }
            return returnValue;
        }

        #endregion Execution

        public int Call(object? args = null)
            => CallAsync(args).GetAwaiter().GetResult();
        public async Task<int> CallAsync(object? args = null, CancellationToken? cancellationToken = null)
        {
            var pars = ContractMember.GetMembersFromContract(args, typeof(TActionContract));
            using var cmd = startCommand(pars);
            await doExec(cmd, cancellationToken);
            return parseCommandResult(cmd, args, pars);
        }

        public int Call(TActionContract args)
            => CallAsync(args, null).GetAwaiter().GetResult();
        public async Task<int> CallAsync(TActionContract args, CancellationToken? cancellationToken = null)
        {
            var pars = ContractMember.GetMembersFromContract(args, typeof(TActionContract));
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
            var pars = ContractMember.GetMembersFromContract(args, typeof(TActionContract));
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
            var pars = ContractMember.GetMembersFromContract(args, typeof(TActionContract));
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
            var pars = ContractMember.GetMembersFromContract(args, typeof(TActionContract));
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
            var pars = ContractMember.GetMembersFromContract(args, typeof(TActionContract));
            using var cmd = startCommand(pars);
            var result = await readSingle<TResult>(cmd, cancellationToken);
            parseCommandResult(cmd, args, pars);
            return result;
        }
    }
}
