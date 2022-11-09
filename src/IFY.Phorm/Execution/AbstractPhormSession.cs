﻿using IFY.Phorm.Connectivity;
using IFY.Phorm.Data;
using IFY.Phorm.EventArgs;
using IFY.Phorm.Execution;
using System;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IFY.Phorm
{
    public abstract partial class AbstractPhormSession : IPhormSession
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

        public event EventHandler<CommandExecutingEventArgs>? CommandExecuting;
        internal void OnCommandExecuting(CommandExecutingEventArgs args)
        {
            try { CommandExecuting?.Invoke(this, args); } catch { }
            Events.OnCommandExecuting(this, args);
        }

        public event EventHandler<CommandExecutedEventArgs>? CommandExecuted;
        internal void OnCommandExecuted(CommandExecutedEventArgs args)
        {
            try { CommandExecuted?.Invoke(this, args); } catch { }
            Events.OnCommandExecuted(this, args);
        }

        public event EventHandler<UnexpectedRecordColumnEventArgs>? UnexpectedRecordColumn;
        internal void OnUnexpectedRecordColumn(UnexpectedRecordColumnEventArgs args)
        {
            try { UnexpectedRecordColumn?.Invoke(this, args); } catch { }
            Events.OnUnexpectedRecordColumn(this, args);
        }

        public event EventHandler<UnresolvedContractMemberEventArgs>? UnresolvedContractMember;
        internal void OnUnresolvedContractMember(UnresolvedContractMemberEventArgs args)
        {
            try { UnresolvedContractMember?.Invoke(this, args); } catch { }
            Events.OnUnresolvedContractMember(this, args);
        }

        public event EventHandler<ConsoleMessageEventArgs>? ConsoleMessage;
        internal void OnConsoleMessage(ConsoleMessageEventArgs args)
        {
            try { ConsoleMessage?.Invoke(this, args); } catch { }
            Events.OnConsoleMessage(this, args);
        }

        #endregion Events

        public bool ExceptionsAsConsoleMessage { get; set; } = GlobalSettings.ExceptionsAsConsoleMessage;

        public bool StrictResultSize { get; set; } = GlobalSettings.StrictResultSize;

        public AbstractPhormSession(string databaseConnectionString, string? connectionName)
        {
            _databaseConnectionString = databaseConnectionString;
            ConnectionName = connectionName;
        }

        #region Connection

        protected internal abstract IPhormDbConnection GetConnection();

        public abstract IPhormSession SetConnectionName(string connectionName);

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
            public override void Dispose() { }
        }

        #endregion Connection

        #region Call

        public int Call(string contractName, object? args = null)
            => CallAsync(contractName, args).GetAwaiter().GetResult();
        public Task<int> CallAsync(string contractName, object? args = null, CancellationToken? cancellationToken = null)
        {
            var runner = new PhormContractRunner<IPhormContract>(this, contractName, DbObjectType.StoredProcedure, args);
            return runner.CallAsync(cancellationToken);
        }

        public int Call<TActionContract>(object? args = null)
            where TActionContract : IPhormContract
            => CallAsync<TActionContract>(args).GetAwaiter().GetResult();
        public Task<int> CallAsync<TActionContract>(object? args = null, CancellationToken? cancellationToken = null)
            where TActionContract : IPhormContract
        {
            var runner = new PhormContractRunner<TActionContract>(this, null, DbObjectType.StoredProcedure, args);
            return runner.CallAsync(cancellationToken);
        }

        public int Call<TActionContract>(TActionContract contract)
            where TActionContract : IPhormContract
            => CallAsync(contract, null).GetAwaiter().GetResult();
        public Task<int> CallAsync<TActionContract>(TActionContract contract, CancellationToken? cancellationToken = null)
            where TActionContract : IPhormContract
        {
            var runner = new PhormContractRunner<TActionContract>(this, null, DbObjectType.StoredProcedure, contract);
            return runner.CallAsync(cancellationToken);
        }

        #endregion Call

        #region From

        public IPhormContractRunner From(string contractName, object? args = null)
        {
            return new PhormContractRunner<IPhormContract>(this, contractName, DbObjectType.StoredProcedure, args);
        }

        public IPhormContractRunner<TActionContract> From<TActionContract>(object? args = null)
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

        public TResult? Get<TResult>(TResult args)
            where TResult : class
            => Get<TResult>((object?)args);
        public TResult? Get<TResult>(object? args = null)
            where TResult : class
        {
            var runner = new PhormContractRunner<IPhormContract>(this, typeof(TResult), null, DbObjectType.View, args);
            return runner.Get<TResult>();
        }

        public Task<TResult?> GetAsync<TResult>(TResult args, CancellationToken? cancellationToken = null)
            where TResult : class
            => GetAsync<TResult>((object?)args, cancellationToken);
        public Task<TResult?> GetAsync<TResult>(object? args = null, CancellationToken? cancellationToken = null)
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
