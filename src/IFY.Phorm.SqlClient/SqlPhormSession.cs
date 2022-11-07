using IFY.Phorm.Connectivity;
using IFY.Phorm.Execution;
using Microsoft.Data.SqlClient;
using System;
using System.Data;

namespace IFY.Phorm.SqlClient
{
    public class SqlPhormSession : AbstractPhormSession
    {
        public SqlPhormSession(string databaseConnectionString, string? connectionName = null)
            : base(new SqlConnectionProvider(databaseConnectionString), connectionName)
        { }
        public SqlPhormSession(IPhormDbConnectionProvider connectionProvider, string? connectionName = null)
            : base(connectionProvider, connectionName)
        {
        }

        public override IPhormSession SetConnectionName(string connectionName)
        {
            return new SqlPhormSession(_connectionProvider, connectionName);
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
            var conn = _connectionProvider.GetConnection(_connectionName);
            conn.Open();
            var transaction = conn.BeginTransaction();
            return new TransactedSqlPhormSession(_connectionProvider, _connectionName, transaction);
        }

        #endregion Transactions
    }
}
