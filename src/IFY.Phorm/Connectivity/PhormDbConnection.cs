using IFY.Shimr.Extensions;
using System.Data;

namespace IFY.Phorm.Connectivity;

/// <summary>
/// Wraps <see cref="IDbConnection"/> with additional Pho/rm values.
/// </summary>
public sealed class PhormDbConnection : IPhormDbConnection
{
    private readonly IDbConnection _db;
    /// <inheritdoc/>
    public IDbConnection DbConnection => _db;

    /// <inheritdoc/>
    public string? ConnectionName { get; }
    /// <inheritdoc/>
    public string DefaultSchema { get; set; } = string.Empty;

    /// <inheritdoc/>
    public string ConnectionString { get => _db.ConnectionString; set => _db.ConnectionString = value; }
    /// <inheritdoc/>
    public int ConnectionTimeout => _db.ConnectionTimeout;
    /// <inheritdoc/>
    public string Database => _db.Database;
    /// <inheritdoc/>
    public ConnectionState State => _db.State;

    public PhormDbConnection(string? connectionName, IDbConnection dbConnection)
    {
        ConnectionName = connectionName;
        _db = dbConnection;
    }

    /// <inheritdoc/>
    public IDbTransaction BeginTransaction() => _db.BeginTransaction();
    /// <inheritdoc/>
    public IDbTransaction BeginTransaction(IsolationLevel il) => _db.BeginTransaction(il);

    /// <inheritdoc/>
    public void ChangeDatabase(string databaseName) => _db.ChangeDatabase(databaseName);

    /// <inheritdoc/>
    public void Open()
    {
        if (_db.State != ConnectionState.Open)
        {
            _db.Open();
        }
    }
    /// <inheritdoc/>
    public void Close()
    {
        if (_db.State != ConnectionState.Closed)
        {
            _db.Close();
        }
    }

    /// <inheritdoc/>
    public IAsyncDbCommand CreateCommand()
        => ((IDbConnection)this).CreateCommand().Shim<IAsyncDbCommand>()!;
    IDbCommand IDbConnection.CreateCommand()
    {
        Open();
        return _db.CreateCommand();
    }

    /// <inheritdoc/>
    public void Dispose() => _db.Dispose();
}
