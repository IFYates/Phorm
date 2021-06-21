using IFY.Phorm.Connectivity;
using System.Data;

namespace IFY.Phorm.SqlClient
{
    public class TransactedSqlPhormRunner : SqlPhormRunner, ITransactedPhormRunner
    {
        private bool _isDisposed = false;
        private readonly IDbTransaction _transaction;

        public TransactedSqlPhormRunner(IPhormDbConnectionProvider dbProvider, string? contextUser, IDbTransaction transaction)
            : base(dbProvider, contextUser)
        {
            _transaction = transaction;
        }

        public override bool IsInTransaction => true;

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
