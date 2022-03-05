﻿using IFY.Phorm.Connectivity;
using IFY.Phorm.Data;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace IFY.Phorm
{
    public abstract partial class AbstractPhormSession : IPhormSession
    {
        public bool StrictResultSize { get; set; } = true;

        public AbstractPhormSession()
        {
        }

        #region Connection

        internal IAsyncDbCommand CreateCommand(string? schema, string objectName, DbObjectType objectType)
        {
            var conn = GetConnection();
            schema = schema?.Length > 0 ? schema : conn.DefaultSchema;
            return CreateCommand(conn, schema, objectName, objectType);
        }

        protected abstract IPhormDbConnection GetConnection();

        protected virtual IAsyncDbCommand CreateCommand(IPhormDbConnection connection, string schema, string objectName, DbObjectType objectType)
        {
            var cmd = connection.CreateCommand();

#if NETSTANDARD || NETCOREAPP
            if (objectType.IsOneOf(DbObjectType.Table, DbObjectType.View))
#else
            if (objectType is DbObjectType.Table or DbObjectType.View)
#endif
            {
                cmd.CommandType = CommandType.Text;
                // TODO: Could replace '*' with desired column names, validated by cached SchemaOnly call
                // TODO: Can do TOP 2 if want to check for first item
                cmd.CommandText = $"SELECT * FROM [{schema}].[{objectName}]";
                return cmd;
            }

            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = $"[{schema}].[{objectName}]";
            return cmd;
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
            var runner = new PhormContractRunner<IPhormContract>(this, typeof(TResult), null, DbObjectType.Table, args);
            return runner.Get<TResult>();
        }

        public Task<TResult?> GetAsync<TResult>(TResult args, CancellationToken? cancellationToken = null)
            where TResult : class
            => GetAsync<TResult>((object?)args, cancellationToken);
        public Task<TResult?> GetAsync<TResult>(object? args = null, CancellationToken? cancellationToken = null)
            where TResult : class
        {
            var runner = new PhormContractRunner<IPhormContract>(this, typeof(TResult), null, DbObjectType.Table, args);
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
