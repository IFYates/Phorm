using IFY.Phorm.Connectivity;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace IFY.Phorm.SqlClient
{
    public class SqlConnectionProvider : IPhormDbConnectionProvider
    {
        public string DatabaseConnectionString { get; }

        public event EventHandler<IPhormDbConnection>? Connected;

        private static readonly Dictionary<string, IPhormDbConnection> _connectionPool = new Dictionary<string, IPhormDbConnection>();

        internal Func<string?, IDbConnection, IPhormDbConnection> _connectionBuilder = (connectionName, conn) => new PhormDbConnection(connectionName, conn);

        public SqlConnectionProvider(string databaseConnectionString)
        {
            DatabaseConnectionString = databaseConnectionString;
        }

        public IPhormDbConnection GetConnection(string? connectionName)
        {
            // Reuse existing connections, where possible
            if (!_connectionPool.TryGetValue(connectionName ?? string.Empty, out var phormConn)
                || phormConn.State != ConnectionState.Open)
            {
                lock (_connectionPool)
                {
                    if (!_connectionPool.TryGetValue(connectionName ?? string.Empty, out phormConn)
                        || phormConn.State != ConnectionState.Open)
                    {
                        // Create new connection
                        phormConn?.Dispose();

                        // Ensure application name is known user
                        var connectionString = new SqlConnectionStringBuilder(DatabaseConnectionString);
                        connectionString.ApplicationName = connectionName ?? connectionString.ApplicationName;
                        var sqlConnStr = connectionString.ToString();

                        // Open connection
                        var db = new SqlConnection(sqlConnStr);
                        phormConn = _connectionBuilder(connectionName, db);

                        // Resolve default schema
                        if (phormConn.DefaultSchema.Length == 0)
                        {
                            phormConn.Open();
                            using var cmd = ((IDbConnection)phormConn).CreateCommand();
                            cmd.CommandText = "SELECT schema_name()";
                            phormConn.DefaultSchema = cmd.ExecuteScalar()?.ToString() ?? connectionString.UserID;
                        }
                        _connectionPool[connectionName ?? string.Empty] = phormConn;

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

        public IPhormSession GetSession(string? connectionName = null)
        {
            return new SqlPhormSession(this, connectionName);
        }
    }
}
