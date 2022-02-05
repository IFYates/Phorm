﻿using IFY.Phorm.Connectivity;
using System;
using System.Data;
using System.Linq;

namespace IFY.Phorm.SqlClient
{
    // TODO: handle errors and log messages

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

            return base.CreateCommand(connection, schema, objectName, objectType);
        }

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
