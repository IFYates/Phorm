using Shimterface;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace IFY.Phorm.Connectivity
{
    /// <summary>
    /// Wraps <see cref="IDbConnection"/> with additional Pho/rm values.
    /// </summary>
    public sealed class PhormDbConnection : IPhormDbConnection
    {
        private readonly IDbConnection _db;
        public IDbConnection DbConnection => _db;

        public string? ConnectionName { get; }
        public string DefaultSchema { get; set; } = string.Empty;

        public string ConnectionString { get => _db.ConnectionString; set => _db.ConnectionString = value; }
        public int ConnectionTimeout => _db.ConnectionTimeout;
        public string Database => _db.Database;
        public ConnectionState State => _db.State;

        public PhormDbConnection(string? connectionName, IDbConnection dbConnection)
        {
            ConnectionName = connectionName;
            _db = dbConnection;
        }

        public IDbTransaction BeginTransaction() => _db.BeginTransaction();
        public IDbTransaction BeginTransaction(IsolationLevel il) => _db.BeginTransaction(il);

        public void ChangeDatabase(string databaseName) => _db.ChangeDatabase(databaseName);

        public void Open() => _db.Open();
        public void Close() => _db.Close();

        // TEMP
        [ExcludeFromCodeCoverage]
        internal class DbCommandShim : IAsyncDbCommand
        {
            private readonly DbCommand _cmd;

            public DbCommandShim(DbCommand cmd)
            {
                _cmd = cmd;
            }

            public string CommandText { get => _cmd.CommandText; set => _cmd.CommandText = value; }
            public CommandType CommandType { get => _cmd.CommandType; set => _cmd.CommandType = value; }

            public IDataParameterCollection Parameters => _cmd.Parameters;

            public IDbDataParameter CreateParameter() => _cmd.CreateParameter();

            public void Dispose()
            {
                _cmd.Dispose();
            }

            public Task<DbDataReader> ExecuteReaderAsync(CancellationToken cancellationToken) => _cmd.ExecuteReaderAsync(cancellationToken);
        }

#pragma warning disable CS8603 // Possible null reference return.
        public IAsyncDbCommand CreateCommand() => new DbCommandShim(_db.CreateCommand() as DbCommand);//?.Shim<IAsyncDbCommand>();
#pragma warning restore CS8603 // Possible null reference return.
        IDbCommand IDbConnection.CreateCommand() => _db.CreateCommand();

        public void Dispose() => _db.Dispose();
    }
}
