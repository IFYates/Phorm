using IFY.Phorm.Connectivity;
using Shimterface;
using System;
using System.Data;
using System.Data.SqlClient;

namespace IFY.Phorm.SqlClient
{
    public class SqlConnectionProvider : IPhormDbConnectionProvider
    {
        public string DatabaseConnectionString { get; }

        public event EventHandler<IPhormDbConnection>? Connected;

        public SqlConnectionProvider(string databaseConnectionString)
        {
            DatabaseConnectionString = databaseConnectionString;
        }

        protected virtual SqlConnectionStringBuilder GetConnectionString(string? contextUser)
        {
            // The connection will identify as the context user
            return new SqlConnectionStringBuilder(DatabaseConnectionString)
            {
                ApplicationName = contextUser ?? string.Empty
            };
        }

        public IPhormDbConnection GetConnection(string? connectionName)
        {
            // Ensure application name is known user
            var connectionString = GetConnectionString(connectionName);

            // TODO: if conn is a reuse, use same PhormDbConnection instance
            var conn = new SqlConnection(connectionString.ToString());
            var phormConn = new PhormDbConnection(connectionName, conn);

            // Resolve default schema
            if (phormConn.DefaultSchema.Length == 0)
            {
                conn.Open();
                using var cmd = conn.CreateCommand();
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
    }
}
