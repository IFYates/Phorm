using IFY.Phorm.Connectivity;
using IFY.Phorm.Execution;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace IFY.Phorm.SqlClient
{
    // TODO: handle errors and log messages

    public class SqlPhormSession : AbstractPhormSession
    {
        private readonly string? _connectionName;

        public string ViewPrefix { get; init; } = "vw_";
        public string ProcedurePrefix { get; init; } = "usp_";
        public string TablePrefix { get; init; } = "";

        public SqlPhormSession(IPhormDbConnectionProvider connectionProvider, string? connectionName)
            : base(connectionProvider)
        {
            _connectionName = connectionName;
        }

        protected override string? GetConnectionName() => _connectionName;

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

            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            return base.CreateCommand(connection, schema, objectName, objectType);
        }

        #region Console capture

        public class SqlConsoleCapture : IConsoleCapture
        {
            private readonly SqlConnection _conn;
            private readonly StringBuilder _consoleOutput = new();

            public SqlConsoleCapture(SqlConnection conn)
            {
                _conn = conn;
                conn.InfoMessage += captureInfoMessage;
            }

            private void captureInfoMessage(object sender, SqlInfoMessageEventArgs e)
            {
                foreach (SqlError err in e.Errors)
                {
                    // TODO: possible only for cmd?
                    // TODO: other info
                    _consoleOutput.AppendLine(err.Message);
                }
            }

            public string Complete()
            {
                _conn.InfoMessage -= captureInfoMessage;
                var res = _consoleOutput.ToString();
                _consoleOutput.Clear();
                return res;
            }
        }
        protected override IConsoleCapture StartConsoleCapture(IAsyncDbCommand cmd)
        {
            if (cmd.Connection is SqlConnection sql)
            {
                return new SqlConsoleCapture(sql);
            }

            return NullConsoleCapture.Instance;
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
