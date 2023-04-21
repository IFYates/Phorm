using IFY.Phorm.Connectivity;
using IFY.Phorm.Execution;
using System.Data;

namespace IFY.Phorm.SqlClient;

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

    /// <inheritdoc/>
    public override bool IsInTransaction => true;

    /// <inheritdoc/>
    protected override IAsyncDbCommand CreateCommand(IPhormDbConnection connection, string schema, string objectName, DbObjectType objectType)
    {
        var cmd = base.CreateCommand(connection, schema, objectName, objectType);
        cmd.Transaction = _transaction;
        return cmd;
    }

    /// <inheritdoc/>
    protected override IPhormDbConnection GetConnection()
        => _connection;

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
        if (!_isDisposed)
        {
            _connection.Dispose();
            _transaction.Dispose();
            _isDisposed = true;
        }
    }
}
