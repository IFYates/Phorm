using IFY.Shimr.Extensions;
using System.Data;

namespace IFY.Phorm.Connectivity;

/// <summary>
/// Wraps <see cref="IDbConnection"/> with additional Pho/rm values.
/// </summary>
public sealed class PhormDbConnection(string? connectionName, IDbConnection dbConnection)
    : IPhormDbConnection
{
    /// <inheritdoc/>
    public IDbConnection DbConnection => dbConnection;

    /// <inheritdoc/>
    public string? ConnectionName { get; } = connectionName;
    /// <inheritdoc/>
    public string DefaultSchema { get; set; } = string.Empty;

    /// <inheritdoc/>
    [System.Diagnostics.CodeAnalysis.AllowNull] public string ConnectionString { get => dbConnection.ConnectionString; set => dbConnection.ConnectionString = value; }
    /// <inheritdoc/>
    public int ConnectionTimeout => dbConnection.ConnectionTimeout;
    /// <inheritdoc/>
    public string Database => dbConnection.Database;
    /// <inheritdoc/>
    public ConnectionState State => dbConnection.State;

    /// <inheritdoc/>
    public IDbTransaction BeginTransaction() => dbConnection.BeginTransaction();
    /// <inheritdoc/>
    public IDbTransaction BeginTransaction(IsolationLevel il) => dbConnection.BeginTransaction(il);

    /// <inheritdoc/>
    public void ChangeDatabase(string databaseName) => dbConnection.ChangeDatabase(databaseName);

    /// <inheritdoc/>
    public void Open()
    {
        if (dbConnection.State != ConnectionState.Open)
        {
            dbConnection.Open();
        }
    }
    /// <inheritdoc/>
    public void Close()
    {
        if (dbConnection.State != ConnectionState.Closed)
        {
            dbConnection.Close();
        }
    }

    /// <inheritdoc/>
    public IAsyncDbCommand CreateCommand()
        => ((IDbConnection)this).CreateCommand().Shim<IAsyncDbCommand>()!;
    IDbCommand IDbConnection.CreateCommand()
    {
        Open();
        return dbConnection.CreateCommand();
    }

    /// <inheritdoc/>
    public void Dispose() => dbConnection.Dispose();
}
