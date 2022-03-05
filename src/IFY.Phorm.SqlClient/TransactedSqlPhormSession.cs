using IFY.Phorm.Connectivity;
using System.Data;

namespace IFY.Phorm.SqlClient
{
    public class TransactedSqlPhormSession : SqlPhormSession, ITransactedPhormSession
    {
        private bool _isDisposed = false;

        private readonly IPhormDbConnection _connection;
        private readonly IDbTransaction _transaction;

        public TransactedSqlPhormSession(IPhormDbConnection connection, IDbTransaction transaction)
            : base(connection.ConnectionString, connection.ConnectionName)
        {
            _connection = connection;
            _transaction = transaction;
        }

        public override bool IsInTransaction => true;

        protected override IPhormDbConnection GetConnection()
        {
            return _connection;
        }

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
                _transaction.Connection?.Dispose();
                _transaction.Dispose();
                _isDisposed = true;
            }
        }
    }
}
