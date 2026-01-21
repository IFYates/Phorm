using IFY.Phorm.Execution;
using IFY.Shimr.Extensions;
using System.Data;

namespace IFY.Phorm.Connectivity;

/// <summary>
/// Wraps <see cref="IDbConnection"/> with additional Pho/rm values.
/// </summary>
public sealed class PhormDbConnection : IPhormDbConnection
{
    private readonly AbstractPhormSession _session;

    /// <inheritdoc/>
    public string? ConnectionName => _session.ConnectionName;

    /// <inheritdoc/>
    public IAsyncDbConnection DbConnection { get; }
    /// <inheritdoc/>
    public string ConnectionString { get => DbConnection.ConnectionString; set => DbConnection.ConnectionString = value; }
    /// <inheritdoc/>
    public int ConnectionTimeout => DbConnection.ConnectionTimeout;
    /// <inheritdoc/>
    public string Database => DbConnection.Database;
    /// <inheritdoc/>
    public ConnectionState State => DbConnection.State;

    /// <inheritdoc/>
    public string DefaultSchema { get; set; } = string.Empty;

    internal PhormDbConnection(AbstractPhormSession session, IAsyncDbConnection dbConnection)
    {
        DbConnection = dbConnection;
        _session = session;
    }

    /// <inheritdoc/>
    public ValueTask<IDbTransaction> BeginTransactionAsync(CancellationToken cancellationToken) => DbConnection.BeginTransactionAsync(cancellationToken);
    /// <inheritdoc/>
    public ValueTask<IDbTransaction> BeginTransactionAsync(IsolationLevel il, CancellationToken cancellationToken) => DbConnection.BeginTransactionAsync(il, cancellationToken);

    /// <inheritdoc/>
    public Task ChangeDatabaseAsync(string databaseName, CancellationToken cancellationToken) => DbConnection.ChangeDatabaseAsync(databaseName, cancellationToken);

    /// <inheritdoc/>
    public async Task OpenAsync(CancellationToken cancellationToken)
    {
        if (DbConnection.State == ConnectionState.Broken)
        {
            DbConnection.Close();
        }
        if (DbConnection.State == ConnectionState.Closed)
        {
            await DbConnection.OpenAsync(cancellationToken);

            await _session.ApplyContextAsync(this);
            await _session.ResolveDefaultSchemaAsync(this);

            _session.OnConnected(new());
        }
    }
    /// <inheritdoc/>
    public void Close()
    {
        if (DbConnection.State != ConnectionState.Closed)
        {
            DbConnection.Close();
        }
    }

    /// <inheritdoc/>
    public IAsyncDbCommand CreateCommand()
        => ((IAsyncDbConnection)this).CreateCommand().Shim<IAsyncDbCommand>()!;
    /// <inheritdoc/>
    IDbCommand IAsyncDbConnection.CreateCommand()
        => DbConnection.CreateCommand();

    /// <inheritdoc/>
    public void Dispose() => DbConnection.Dispose();
}
