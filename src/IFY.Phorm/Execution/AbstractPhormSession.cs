using IFY.Phorm.Connectivity;
using IFY.Phorm.Data;
using IFY.Phorm.Execution;
using System.Data;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IFY.Phorm
{
    public abstract partial class AbstractPhormSession : IPhormSession
    {
        protected readonly IPhormDbConnectionProvider _connectionProvider;

        public AbstractPhormSession(IPhormDbConnectionProvider connectionProvider)
        {
            _connectionProvider = connectionProvider;
        }

        #region Connection

        internal IAsyncDbCommand CreateCommand(string? schema, string objectName, DbObjectType objectType)
        {
            var conn = _connectionProvider.GetConnection(GetConnectionName());
            schema = schema?.Length > 0 ? schema : conn.DefaultSchema;
            return CreateCommand(conn, schema, objectName, objectType);
        }

        protected abstract string? GetConnectionName();

        protected virtual IAsyncDbCommand CreateCommand(IPhormDbConnection connection, string schema, string objectName, DbObjectType objectType)
        {
            var cmd = connection.CreateCommand();

            if (objectType is DbObjectType.Table or DbObjectType.View)
            {
                cmd.CommandType = CommandType.Text;
                // TODO: Could replace '*' with desired column names, validated by cached SchemaOnly call
                cmd.CommandText = $"SELECT * FROM [{schema}].[{objectName}]";
                return cmd;
            }

            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = $"[{schema}].[{objectName}]";
            return cmd;
        }

        /// <summary>
        /// If the connection implementation supports capture of console output (print statements),
        /// this method returns a new <see cref="IConsoleCapture"/> that will receive the output.
        /// </summary>
        /// <param name="cmd">The command to capture console output for.</param>
        /// <returns>The object that will be provide the final console output.</returns>
        protected internal virtual IConsoleCapture StartConsoleCapture(IAsyncDbCommand cmd) => NullConsoleCapture.Instance;

        #endregion Connection

        #region From

        public IPhormContractRunner From(string objectName, DbObjectType objectType = DbObjectType.StoredProcedure)
        {
            return new PhormContractRunner<IPhormContract>(this, objectName, objectType);
        }

        public IPhormContractRunner<T> From<T>(DbObjectType objectType = DbObjectType.StoredProcedure)
            where T : IPhormContract
        {
            return new PhormContractRunner<T>(this, null, objectType);
        }

        #endregion From

        #region Call

        public int Call(string objectName, object? args = null)
            => CallAsync(objectName, args).GetAwaiter().GetResult();
        public Task<int> CallAsync(string objectName, object? args = null, CancellationToken? cancellationToken = null)
        {
            var runner = new PhormContractRunner<IPhormContract>(this, objectName, DbObjectType.StoredProcedure);
            return runner.CallAsync(args, cancellationToken);
        }

        public int Call<TActionContract>(object? args = null)
            where TActionContract : IPhormContract
            => CallAsync<TActionContract>(args).GetAwaiter().GetResult();
        public Task<int> CallAsync<TActionContract>(object? args = null, CancellationToken? cancellationToken = null)
            where TActionContract : IPhormContract
        {
            var runner = new PhormContractRunner<TActionContract>(this, null, DbObjectType.StoredProcedure);
            return runner.CallAsync(args, cancellationToken);
        }

        public int Call<TActionContract>(TActionContract contract)
            where TActionContract : IPhormContract
            => CallAsync(contract, null).GetAwaiter().GetResult();
        public Task<int> CallAsync<TActionContract>(TActionContract contract, CancellationToken? cancellationToken = null)
            where TActionContract : IPhormContract
        {
            var runner = new PhormContractRunner<TActionContract>(this, null, DbObjectType.StoredProcedure);
            return runner.CallAsync(contract, cancellationToken);
        }

        #endregion Call

        #region Transactions

        public abstract bool SupportsTransactions { get; }

        public abstract bool IsInTransaction { get; }

        public abstract ITransactedPhormSession BeginTransaction();

        #endregion Transactions
    }
}
