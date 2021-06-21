using IFY.Phorm.Connectivity;
using System.Data;

namespace IFY.Phorm.SqlClient
{
    public class SqlPhormRunner : AbstractPhormRunner
    {
        private readonly string? _connectionName;

        public string ProcedurePrefix { get; init; } = "usp_";

        public SqlPhormRunner(IPhormDbConnectionProvider connectionProvider, string? connectionName)
            : base(connectionProvider)
        {
            _connectionName = connectionName;
        }

        protected override string? GetConnectionName() => _connectionName;

        protected override IAsyncDbCommand CreateCommand(IPhormDbConnection connection, string schema, string actionName)
        {
            // Support temp sprocs
            actionName = actionName.Length > 0 && actionName[0] == '#' ? actionName : ProcedurePrefix + actionName;

            return base.CreateCommand(connection, schema, actionName);
        }

        #region Transactions

        public override bool SupportsTransactions => true;
        public override bool IsInTransaction => false;

        public override ITransactedPhormRunner BeginTransaction()
        {
            var conn = _connectionProvider.GetConnection(_connectionName);
            conn.Open();
            var transaction = conn.BeginTransaction();
            return new TransactedSqlPhormRunner(_connectionProvider, _connectionName, transaction);
        }

        #endregion Transactions
    }
}
