using IFY.Phorm.Connectivity;
using System;
using Microsoft.Data.SqlClient;
using System.Collections.Concurrent;
using System.Data;

namespace IFY.Phorm.SqlClient
{
    public class SqlConnectionProvider : IPhormDbConnectionProvider
    {
        public string DatabaseConnectionString { get; }

        public event EventHandler<IPhormDbConnection>? Connected;

        private static readonly ConcurrentDictionary<string, IPhormDbConnection> _connectionPool = new ConcurrentDictionary<string, IPhormDbConnection>();

        internal Func<string?, IDbConnection, IPhormDbConnection> _connectionBuilder = (connectionName, conn) => new PhormDbConnection(connectionName, conn);

        public SqlConnectionProvider(string databaseConnectionString)
        {
            DatabaseConnectionString = databaseConnectionString;
        }

        protected virtual SqlConnectionStringBuilder GetConnectionString(string? connectionName)
        {
            // The connection will identify as the given name
            var connStr = new SqlConnectionStringBuilder(DatabaseConnectionString);
            connStr.ApplicationName = connectionName ?? connStr.ApplicationName;
            return connStr;
        }

        public IPhormDbConnection GetConnection(string? connectionName)
        {
            // Ensure application name is known user
            var connectionString = GetConnectionString(connectionName);
            var sqlConnStr = connectionString.ToString();

            // Reuse existing connections, where possible
            IPhormDbConnection getNewConnection()
            {
                var conn = new SqlConnection(sqlConnStr);
                return _connectionBuilder(connectionName, conn);
            }
            var phormConn = _connectionPool.AddOrUpdate(sqlConnStr,
                _ => getNewConnection(),
                (_, c) =>
                {
                    if (c.State != ConnectionState.Open)
                    {
                        c.Dispose();
                        c = getNewConnection();
                    }
                    return c;
                });

            // Resolve default schema
            if (phormConn.DefaultSchema.Length == 0)
            {
                phormConn.Open();
                using var cmd = ((IDbConnection)phormConn).CreateCommand();
                cmd.CommandText = "SELECT schema_name()";
                phormConn.DefaultSchema = cmd.ExecuteScalar()?.ToString() ?? connectionString.UserID;
            }

            try
            {
                Connected?.Invoke(this, phormConn);
            }
            catch { }
            return phormConn;
        }

        public IPhormSession GetSession(string? connectionName = null)
        {
            return new SqlPhormSession(this, connectionName);
        }
    }
}
