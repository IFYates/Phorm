using IFY.Phorm.Connectivity;
using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Linq;

namespace IFY.Phorm.SqlClient
{
    // TODO: handle errors and log messages

    public class SqlPhormSession : AbstractPhormSession
    {
        private readonly string _databaseConnectionString;
        private readonly string? _connectionName;

        public string ViewPrefix
        {
            get;
#if NETSTANDARD || NETCOREAPP
            set;
#else
            init;
#endif
        } = "vw_";
        public string ProcedurePrefix
        {
            get;
#if NETSTANDARD || NETCOREAPP
            set;
#else
            init;
#endif
        } = "usp_";
        public string TablePrefix
        {
            get;
#if NETSTANDARD || NETCOREAPP
            set;
#else
            init;
#endif
        } = "";

        public SqlPhormSession(string databaseConnectionString, string? connectionName = null)
        {
            _databaseConnectionString = databaseConnectionString;
            _connectionName = connectionName;
        }

        protected override IPhormDbConnection GetConnection()
        {
            // Ensure connection identifies as the given name
            var connStr = new SqlConnectionStringBuilder(_databaseConnectionString);
            connStr.ApplicationName = _connectionName ?? connStr.ApplicationName;

            // TODO: if conn is a reuse, use same PhormDbConnection instance
            var conn = new SqlConnection(connStr.ToString());
            var phormConn = new PhormDbConnection(_connectionName, conn);

            // Resolve default schema
            if (phormConn.DefaultSchema.Length == 0)
            {
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT schema_name()";
                phormConn.DefaultSchema = cmd.ExecuteScalar()?.ToString() ?? connStr.UserID;
            }

            return phormConn;
        }

        protected override IAsyncDbCommand CreateCommand(IPhormDbConnection connection, string schema, string objectName, DbObjectType objectType)
        {
            // Complete object name
            objectName = objectType switch
            {
                DbObjectType.StoredProcedure => objectName.FirstOrDefault() == '#' ? objectName : ProcedurePrefix + objectName, // Support temp sprocs
                DbObjectType.View => ViewPrefix + objectName,
                DbObjectType.Table => TablePrefix + objectName,
                _ => throw new NotSupportedException($"Unsupported object type: {objectType}")
            };

            return base.CreateCommand(connection, schema, objectName, objectType);
        }

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
