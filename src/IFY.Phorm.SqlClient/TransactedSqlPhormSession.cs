using IFY.Phorm.Connectivity;
using System.Data;

namespace IFY.Phorm.SqlClient
{
    // TODO: this file isn't SQL-specific, but making it generic in current structure increases amount of code
    public class TransactedSqlPhormSession : SqlPhormSession, ITransactedPhormSession
    {
        private bool _isDisposed = false;
        private readonly IDbTransaction _transaction;

        public TransactedSqlPhormSession(IPhormDbConnectionProvider dbProvider, string? contextUser, IDbTransaction transaction)
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
