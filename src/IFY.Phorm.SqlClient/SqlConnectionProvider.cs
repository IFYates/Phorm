using IFY.Phorm.Connectivity;
using System;
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

        protected virtual SqlConnectionStringBuilder GetConnectionString(string? connectionName)
        {
            // The connection will identify as the given name
            return new SqlConnectionStringBuilder(DatabaseConnectionString)
            {
                ApplicationName = connectionName ?? string.Empty
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
