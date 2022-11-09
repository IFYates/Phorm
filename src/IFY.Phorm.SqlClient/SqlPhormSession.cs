using IFY.Phorm.Connectivity;
using IFY.Phorm.Execution;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace IFY.Phorm.SqlClient
{
    public class SqlPhormSession : AbstractPhormSession
    {
        public event EventHandler<IPhormDbConnection>? Connected;

        private static readonly Dictionary<string, IPhormDbConnection> _connectionPool = new Dictionary<string, IPhormDbConnection>();

        internal Func<string, string?, IPhormDbConnection> _connectionBuilder = (sqlConnStr, connectionName) => new PhormDbConnection(connectionName, new SqlConnection(sqlConnStr));

        public SqlPhormSession(string databaseConnectionString, string? connectionName = null)
            : base(databaseConnectionString, connectionName)
        {
        }

        public override IPhormSession SetConnectionName(string connectionName)
        {
            return new SqlPhormSession(_databaseConnectionString, connectionName);
        }

        // TODO: base?
        protected override IPhormDbConnection GetConnection()
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

                        // Ensure application name is known user
                        var connectionString = new SqlConnectionStringBuilder(_databaseConnectionString);
                        connectionString.ApplicationName = ConnectionName ?? connectionString.ApplicationName;
                        var sqlConnStr = connectionString.ToString();

                        // Create connection
                        phormConn = _connectionBuilder(sqlConnStr, ConnectionName);

                        // Resolve default schema
                        if (phormConn.DefaultSchema.Length == 0)
                        {
                            using var cmd = ((IDbConnection)phormConn).CreateCommand();
                            cmd.CommandText = "SELECT schema_name()";
                            phormConn.DefaultSchema = cmd.ExecuteScalar()?.ToString() ?? connectionString.UserID;
                        }
                        _connectionPool[ConnectionName ?? string.Empty] = phormConn;

                        try
                        {
                            Connected?.Invoke(this, phormConn);
                        }
                        catch { }
                    }
                }
            }
            return phormConn;
        }

        #region Console capture

        protected override AbstractConsoleMessageCapture StartConsoleCapture(Guid commandGuid, IAsyncDbCommand cmd)
        {
            if (cmd.Connection is SqlConnection sql)
            {
                return new SqlConsoleMessageCapture(this, commandGuid, sql);
            }

            return NullConsoleMessageCapture.Instance;
        }

        #endregion Console capture

        #region Transactions

        public override bool SupportsTransactions => true;
        public override bool IsInTransaction => false;

        public override ITransactedPhormSession BeginTransaction()
        {
            var conn = GetConnection();
            conn.Open();
            var transaction = conn.BeginTransaction();
            return new TransactedSqlPhormSession(conn, transaction);
        }

        #endregion Transactions
    }
}
