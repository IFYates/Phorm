using IFY.Phorm.Connectivity;
using IFY.Phorm.Data;
using IFY.Phorm.EventArgs;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IFY.Phorm.Execution
{
    public abstract class AbstractPhormSession : IPhormSession
    {
        protected readonly string _databaseConnectionString;

        public string? ConnectionName { get; private set; }

        public string ProcedurePrefix
        {
            get;
#if !NET5_0_OR_GREATER
            set;
#else
            init;
#endif
        } = GlobalSettings.ProcedurePrefix;
        public string TablePrefix
        {
            get;
#if !NET5_0_OR_GREATER
            set;
#else
            init;
#endif
        } = GlobalSettings.TablePrefix;
        public string ViewPrefix
        {
            get;
#if !NET5_0_OR_GREATER
            set;
#else
            init;
#endif
        } = GlobalSettings.ViewPrefix;

        #region Events

        public event EventHandler<ConnectedEventArgs> Connected = null!;
        internal void OnConnected(ConnectedEventArgs args)
        {
            try { Connected?.Invoke(this, args); } catch { /* Consume handler errors */ }
            Events.OnConnected(this, args);
        }

        public event EventHandler<CommandExecutingEventArgs> CommandExecuting = null!;
        internal void OnCommandExecuting(CommandExecutingEventArgs args)
        {
            try { CommandExecuting?.Invoke(this, args); } catch { /* Consume handler errors */ }
            Events.OnCommandExecuting(this, args);
        }

        public event EventHandler<CommandExecutedEventArgs> CommandExecuted = null!;
        internal void OnCommandExecuted(CommandExecutedEventArgs args)
        {
            try { CommandExecuted?.Invoke(this, args); } catch { /* Consume handler errors */ }
            Events.OnCommandExecuted(this, args);
        }

        public event EventHandler<UnexpectedRecordColumnEventArgs> UnexpectedRecordColumn = null!;
        internal void OnUnexpectedRecordColumn(UnexpectedRecordColumnEventArgs args)
        {
            try { UnexpectedRecordColumn?.Invoke(this, args); } catch { /* Consume handler errors */ }
            Events.OnUnexpectedRecordColumn(this, args);
        }

        public event EventHandler<UnresolvedContractMemberEventArgs> UnresolvedContractMember = null!;
        internal void OnUnresolvedContractMember(UnresolvedContractMemberEventArgs args)
        {
            try { UnresolvedContractMember?.Invoke(this, args); } catch { /* Consume handler errors */ }
            Events.OnUnresolvedContractMember(this, args);
        }

        public event EventHandler<ConsoleMessageEventArgs> ConsoleMessage = null!;
        internal void OnConsoleMessage(ConsoleMessageEventArgs args)
        {
            try { ConsoleMessage?.Invoke(this, args); } catch { /* Consume handler errors */ }
            Events.OnConsoleMessage(this, args);
        }

        #endregion Events

        public bool ExceptionsAsConsoleMessage { get; set; } = GlobalSettings.ExceptionsAsConsoleMessage;

        public bool StrictResultSize { get; set; } = GlobalSettings.StrictResultSize;

        protected AbstractPhormSession(string databaseConnectionString, string? connectionName)
        {
            _databaseConnectionString = databaseConnectionString;
            ConnectionName = connectionName;
        }

        #region Connection

        private static readonly Dictionary<string, IPhormDbConnection> _connectionPool = new Dictionary<string, IPhormDbConnection>();
        internal static void ResetConnectionPool()
        {
            lock (_connectionPool)
            {
                foreach (var conn in _connectionPool.Values)
                {
                    conn.Dispose();
                }
                _connectionPool.Clear();
            }
        }

        protected internal virtual IPhormDbConnection GetConnection()
        {
            // Reuse existing connections, where possible
            if (!_connectionPool.TryGetValue(ConnectionName ?? string.Empty, out var phormConn)
                || phormConn.State != ConnectionState.Open)
            {
                lock (_connectionPool)
                {
                    if (!_connectionPool.TryGetValue(ConnectionName ?? string.Empty, out phormConn)
                        || phormConn.State != ConnectionState.Open)
                    {
                        // Create new connection
                        phormConn?.Dispose();

                        // Create connection
                        phormConn = CreateConnection();

                        // Resolve default schema
                        if (phormConn.DefaultSchema.Length == 0)
                        {
                            var dbSchema = GetDefaultSchema(phormConn);
                            if (dbSchema?.Length > 0)
                            {
                                phormConn.DefaultSchema = dbSchema;
                            }
                        }
                        _connectionPool[ConnectionName ?? string.Empty] = phormConn;

                        OnConnected(new ConnectedEventArgs { Connection = phormConn });
                    }
                }
            }
            return phormConn;
        }

        protected abstract IPhormDbConnection CreateConnection();

        /// <summary>
        /// Implementations to provide logic for resolving the default schema of the connection.
        /// </summary>
        /// <returns>The default schema name, if known.</returns>
        protected abstract string? GetDefaultSchema(IPhormDbConnection phormConn);

        /// <summary>
        /// Request a session with a different connection name.
        /// </summary>
        public abstract IPhormSession SetConnectionName(string connectionName);

        #endregion Connection

        internal IAsyncDbCommand CreateCommand(string? schema, string objectName, DbObjectType objectType)
        {
            var conn = GetConnection();
            schema = schema?.Length > 0 ? schema : conn.DefaultSchema;
            return CreateCommand(conn, schema, objectName, objectType);
        }

        protected virtual IAsyncDbCommand CreateCommand(IPhormDbConnection connection, string schema, string objectName, DbObjectType objectType)
        {
            // Complete object name
            objectName = objectType switch
            {
                DbObjectType.StoredProcedure => objectName.FirstOrDefault() == '#'
                    ? objectName : ProcedurePrefix + objectName, // Support temp sprocs
                DbObjectType.View => ViewPrefix + objectName,
                DbObjectType.Table => TablePrefix + objectName,
                _ => throw new NotSupportedException($"Unsupported object type: {objectType}")
            };

            var cmd = connection.CreateCommand();

#if !NET5_0_OR_GREATER
            if (objectType.IsOneOf(DbObjectType.Table, DbObjectType.View))
#else
            if (objectType is DbObjectType.Table or DbObjectType.View)
#endif
            {
                cmd.CommandType = CommandType.Text;
                // TODO: Could replace '*' with desired column names, validated by cached SchemaOnly call
                // TODO: Can do TOP 2 if we know single entity Get, to know only 1 item
                cmd.CommandText = $"SELECT * FROM [{schema}].[{objectName}]";
                return cmd;
            }

            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = $"[{schema}].[{objectName}]";
            return cmd;
        }

        #region Console capture

        /// <summary>
        /// If the connection implementation supports capture of console output (print statements),
        /// this method returns a new <see cref="AbstractConsoleMessageCapture"/> that will receive the output.
        /// </summary>
        /// <param name="cmd">The command to capture console output for.</param>
        /// <returns>The object that will be provide the final console output.</returns>
        protected internal virtual AbstractConsoleMessageCapture StartConsoleCapture(Guid commandGuid, IAsyncDbCommand cmd)
            => NullConsoleMessageCapture.Instance;

        protected internal class NullConsoleMessageCapture : AbstractConsoleMessageCapture
        {
            public static readonly NullConsoleMessageCapture Instance = new NullConsoleMessageCapture();
            private NullConsoleMessageCapture() : base(null!, Guid.Empty) { }
            public override bool ProcessException(Exception ex) => false;
            public override void Dispose() { /* Nothing to release */ }
        }

        #endregion Console capture

        #region Call

        public int Call(string contractName)
            => CallAsync(contractName, null).GetAwaiter().GetResult();
        public int Call(string contractName, object? args)
            => CallAsync(contractName, args).GetAwaiter().GetResult();
        public Task<int> CallAsync(string contractName)
            => CallAsync(contractName, null, CancellationToken.None);
        public Task<int> CallAsync(string contractName, object? args)
            => CallAsync(contractName, args, CancellationToken.None);
        public Task<int> CallAsync(string contractName, CancellationToken cancellationToken)
            => CallAsync(contractName, null, cancellationToken);
        public Task<int> CallAsync(string contractName, object? args, CancellationToken cancellationToken)
        {
            var runner = new PhormContractRunner<IPhormContract>(this, contractName, DbObjectType.StoredProcedure, args);
            return runner.CallAsync(cancellationToken);
        }

        public int Call<TActionContract>()
            where TActionContract : IPhormContract
            => CallAsync<TActionContract>(null).GetAwaiter().GetResult();
        public int Call<TActionContract>(object? args)
            where TActionContract : IPhormContract
            => CallAsync<TActionContract>(args).GetAwaiter().GetResult();
        public Task<int> CallAsync<TActionContract>()
            where TActionContract : IPhormContract
            => CallAsync<TActionContract>(null, CancellationToken.None);
        public Task<int> CallAsync<TActionContract>(object? args)
            where TActionContract : IPhormContract
            => CallAsync<TActionContract>(args, CancellationToken.None);
        public Task<int> CallAsync<TActionContract>(CancellationToken cancellationToken)
            where TActionContract : IPhormContract
            => CallAsync<TActionContract>(null, cancellationToken);
        public Task<int> CallAsync<TActionContract>(object? args, CancellationToken cancellationToken)
            where TActionContract : IPhormContract
        {
            var runner = new PhormContractRunner<TActionContract>(this, null, DbObjectType.StoredProcedure, args);
            return runner.CallAsync(cancellationToken);
        }

        public int Call<TActionContract>(TActionContract contract)
            where TActionContract : IPhormContract
            => CallAsync(contract, CancellationToken.None).GetAwaiter().GetResult();
        public Task<int> CallAsync<TActionContract>(TActionContract contract)
            where TActionContract : IPhormContract
            => CallAsync(contract, CancellationToken.None);
        public Task<int> CallAsync<TActionContract>(TActionContract contract, CancellationToken cancellationToken)
            where TActionContract : IPhormContract
        {
            var runner = new PhormContractRunner<TActionContract>(this, null, DbObjectType.StoredProcedure, contract);
            return runner.CallAsync(cancellationToken);
        }

        #endregion Call

        #region From

        public IPhormContractRunner From(string contractName)
            => From(contractName, null);
        public IPhormContractRunner From(string contractName, object? args)
        {
            return new PhormContractRunner<IPhormContract>(this, contractName, DbObjectType.StoredProcedure, args);
        }

        public IPhormContractRunner<TActionContract> From<TActionContract>()
            where TActionContract : IPhormContract
            => From<TActionContract>(null);
        public IPhormContractRunner<TActionContract> From<TActionContract>(object? args)
            where TActionContract : IPhormContract
        {
            return new PhormContractRunner<TActionContract>(this, null, DbObjectType.StoredProcedure, args);
        }

        public IPhormContractRunner<TActionContract> From<TActionContract>(TActionContract contract)
            where TActionContract : IPhormContract
        {
            return new PhormContractRunner<TActionContract>(this, null, DbObjectType.StoredProcedure, contract);
        }

        #endregion From

        #region Get

        public TResult? Get<TResult>()
            where TResult : class
            => Get<TResult>((object?)null);
        public TResult? Get<TResult>(TResult args)
            where TResult : class
            => Get<TResult>((object?)args);
        public TResult? Get<TResult>(object? args)
            where TResult : class
        {
            var runner = new PhormContractRunner<IPhormContract>(this, typeof(TResult), null, DbObjectType.View, args);
            return runner.Get<TResult>();
        }

        public Task<TResult?> GetAsync<TResult>()
            where TResult : class
            => GetAsync<TResult>((object?)null, CancellationToken.None);
        public Task<TResult?> GetAsync<TResult>(CancellationToken cancellationToken)
            where TResult : class
            => GetAsync<TResult>((object?)null, cancellationToken);
        public Task<TResult?> GetAsync<TResult>(TResult args)
            where TResult : class
            => GetAsync<TResult>((object?)args, CancellationToken.None);
        public Task<TResult?> GetAsync<TResult>(TResult args, CancellationToken cancellationToken)
            where TResult : class
            => GetAsync<TResult>((object?)args, cancellationToken);
        public Task<TResult?> GetAsync<TResult>(object? args)
            where TResult : class
            => GetAsync<TResult>(args, CancellationToken.None);
        public Task<TResult?> GetAsync<TResult>(object? args, CancellationToken cancellationToken)
            where TResult : class
        {
            var runner = new PhormContractRunner<IPhormContract>(this, typeof(TResult), null, DbObjectType.View, args);
            return runner.GetAsync<TResult>(cancellationToken);
        }

        #endregion Get

        #region Transactions

        public abstract bool SupportsTransactions { get; }

        public abstract bool IsInTransaction { get; }

        public abstract ITransactedPhormSession BeginTransaction();

        #endregion Transactions
    }
}
