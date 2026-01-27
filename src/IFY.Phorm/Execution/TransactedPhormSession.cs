using System.Data;

namespace IFY.Phorm.Execution;

// TODO: Transaction logic to a new type of IPhormDbConnection instead?
// TODO: CommitAsync, RollbackAsync
internal sealed class TransactedPhormSession(IAsyncDbConnection connection, IDbTransaction transaction, AbstractPhormSession sourceSession)
    : AbstractPhormSession(transaction, sourceSession.ConnectionName), ITransactedPhormSession
{
    private readonly IAsyncDbConnection _connection = connection;
    private readonly IDbTransaction _transaction = transaction;

    /// <inheritdoc/>
    public override bool SupportsTransactions => true;

    /// <inheritdoc/>
    public override bool IsInTransaction => true;

    /// <inheritdoc/>
    protected override IAsyncDbConnection CreateConnection(bool readOnly)
    {
        return _connection;
    }

    /// <inheritdoc/>
    public void Commit()
    {
        _transaction.Commit();
    }

    /// <inheritdoc/>
    public void Rollback()
    {
        _transaction.Rollback();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _transaction.Dispose();
        GC.SuppressFinalize(this);
    }
}