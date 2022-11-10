using IFY.Phorm.Connectivity;
using IFY.Phorm.Execution;
using System.Data;

namespace IFY.Phorm.SqlClient
{
    // TODO: this file isn't SQL-specific, but making it generic in current structure increases amount of code
    public class TransactedSqlPhormSession : SqlPhormSession, ITransactedPhormSession
    {
        private bool _isDisposed;

        private readonly IPhormDbConnection _connection;
        private readonly IDbTransaction _transaction;

        internal TransactedSqlPhormSession(IPhormDbConnection connection, IDbTransaction transaction)
            : base(connection.ConnectionString, connection.ConnectionName)
        {
            _connection = connection;
            _transaction = transaction;
        }

        public override bool IsInTransaction => true;

        protected override IPhormDbConnection GetConnection()
            => _connection;

        public void Commit()
        {
            _transaction.Commit();
        }

        public void Rollback()
        {
            _transaction.Rollback();
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _connection.Dispose();
                _transaction.Dispose();
                _isDisposed = true;
            }
        }
    }
}
