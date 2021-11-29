using IFY.Phorm.Connectivity;
using IFY.Phorm.Data;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace IFY.Phorm
{
    public abstract partial class AbstractPhormRunner : IPhormRunner
    {
        protected readonly IPhormDbConnectionProvider _connectionProvider;

        public AbstractPhormRunner(IPhormDbConnectionProvider connectionProvider)
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
                cmd.CommandText = $"SELECT * FROM [{schema}].[{objectName}]";
                return cmd;
            }

            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = $"[{schema}].[{objectName}]";
            return cmd;
        }

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
            return From(objectName).CallAsync(args, cancellationToken);
        }

        public int Call<TActionContract>(object? args = null)
            where TActionContract : IPhormContract
            => CallAsync<TActionContract>(args).GetAwaiter().GetResult();
        public Task<int> CallAsync<TActionContract>(object? args = null, CancellationToken? cancellationToken = null)
            where TActionContract : IPhormContract
        {
            return From<TActionContract>().CallAsync(args, cancellationToken);
        }

        public int Call<TActionContract>(TActionContract contract)
            where TActionContract : IPhormContract
            => CallAsync(contract, null).GetAwaiter().GetResult();
        public Task<int> CallAsync<TActionContract>(TActionContract contract, CancellationToken? cancellationToken = null)
            where TActionContract : IPhormContract
        {
            return From<TActionContract>().CallAsync(contract, cancellationToken);
        }

        #endregion Call

        #region Transactions

        public abstract bool SupportsTransactions { get; }

        public abstract bool IsInTransaction { get; }

        public abstract ITransactedPhormRunner BeginTransaction();

        #endregion Transactions
    }
}
