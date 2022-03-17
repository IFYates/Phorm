using IFY.Phorm.Connectivity;
using IFY.Phorm.Execution;
using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Linq;

namespace IFY.Phorm.SqlClient
{
    public class SqlPhormSession : AbstractPhormSession
    {
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
            : this(new SqlConnectionProvider(databaseConnectionString), connectionName)
        { }
        public SqlPhormSession(IPhormDbConnectionProvider connectionProvider, string? connectionName = null)
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

        public class SqlConsoleMessageCapture : AbstractConsoleMessageCapture
        {
            private readonly SqlConnection _conn;

            public SqlConsoleMessageCapture(AbstractPhormSession session, Guid commandGuid, SqlConnection conn)
                : base(session, commandGuid)
            {
                _conn = conn;
                conn.InfoMessage += captureInfoMessage;
            }

            public override bool ProcessException(Exception ex)
            {
                if (ex is SqlException sqlException)
                {
                    HasError = true;
                    fromSqlErrors(sqlException.Errors, true);
                    return true;
                }
                return false;
            }

            private void captureInfoMessage(object sender, SqlInfoMessageEventArgs e)
            {
                // TODO: possible only for cmd?
                fromSqlErrors(e.Errors, false);
            }

            private void fromSqlErrors(SqlErrorCollection errors, bool isException)
            {
                foreach (SqlError err in errors)
                {
                    // TODO: other info
                    OnConsoleMessage(new ConsoleMessage
                    {
                        IsError = isException,
                        Level = err.Class,
                        Source = $"{err.Procedure} @ {err.LineNumber}",
                        Message = err.Message
                    });
                }
            }

            public override void Dispose()
            {
                _conn.InfoMessage -= captureInfoMessage;
                GC.SuppressFinalize(this);
            }
        }

        protected override AbstractConsoleMessageCapture StartConsoleCapture(Guid commandGuid, IAsyncDbCommand cmd)
        {
            if (cmd.Connection is SqlConnection sql)
            {
                return new SqlConsoleMessageCapture(this, commandGuid, sql);
            }

            return base.StartConsoleCapture(commandGuid, cmd);
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
