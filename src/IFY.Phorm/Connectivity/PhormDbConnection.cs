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

        public string ConnectionString { get => _db.ConnectionString; [param: AllowNull] set => _db.ConnectionString = value; }
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

        public IAsyncDbCommand CreateCommand() => _db.CreateCommand().Shim<IAsyncDbCommand>()!;
        IDbCommand IDbConnection.CreateCommand() => _db.CreateCommand();

        public void Dispose() => _db.Dispose();
    }
}
