using IFY.Phorm.Connectivity;
using System;
using System.Data;
using System.Linq;

namespace IFY.Phorm.SqlClient
{
    public class SqlPhormRunner : AbstractPhormRunner
    {
        private readonly string? _connectionName;

        public string ViewPrefix { get; init; } = "vw_";
        public string ProcedurePrefix { get; init; } = "usp_";
        public string TablePrefix { get; init; } = "";

        public SqlPhormRunner(IPhormDbConnectionProvider connectionProvider, string? connectionName)
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

            return base.CreateCommand(connection, schema, objectName, objectType);
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
