using IFY.Shimr;
using System.Data.Common;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace System.Data;

/// <summary>
/// Exposes the asynchronous parts of <see cref="DbConnection"/>.
/// </summary>
public interface IAsyncDbConnection : IDisposable
{
    /// <summary>
    /// Gets or sets the connection string used to establish a connection to the data source.
    /// </summary>
    string ConnectionString { get; set; }
    /// <summary>
    /// Gets the time, in seconds, to wait while trying to establish a connection before terminating the attempt and
    /// generating an error.
    /// </summary>
    int ConnectionTimeout { get; }
    /// <summary>
    /// Gets the name of the current database for the connection.
    /// </summary>
    string Database { get; }
    /// <summary>
    /// Gets the current state of the connection.
    /// </summary>
    ConnectionState State { get; }

    /// <summary>
    /// Begins an asynchronous database transaction using the current connection.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A value task representing the asynchronous operation. The result contains an <see cref="IDbTransaction"/> that
    /// represents the new transaction.</returns>
    [Shim(typeof(DbConnection))] ValueTask<IDbTransaction> BeginTransactionAsync(CancellationToken cancellationToken);
    /// <summary>
    /// Begins an asynchronous database transaction with the specified isolation level.
    /// </summary>
    /// <param name="il">The isolation level to use for the transaction. Determines the locking and row versioning behavior for the
    /// transaction.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to request cancellation of the asynchronous operation.</param>
    /// <returns>A value task representing the asynchronous operation. The result contains an <see cref="IDbTransaction"/> that
    /// represents the new transaction.</returns>
    [Shim(typeof(DbConnection))] ValueTask<IDbTransaction> BeginTransactionAsync(IsolationLevel il, CancellationToken cancellationToken);
    /// <summary>
    /// Asynchronously closes the connection and releases any associated resources.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the close operation.</param>
    /// <returns>A task that represents the asynchronous close operation.</returns>
    [Shim(typeof(DbConnection))] Task CloseAsync(CancellationToken cancellationToken);
    /// <summary>
    /// Asynchronously changes the current database for an open connection to the specified database name.
    /// </summary>
    /// <remarks>The connection must be open before calling this method. If the operation is canceled, the
    /// connection remains unchanged.</remarks>
    /// <param name="databaseName">The name of the database to switch to. Cannot be null, empty, or contain only whitespace.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Shim(typeof(DbConnection))] Task ChangeDatabaseAsync(string databaseName, CancellationToken cancellationToken);
    /// <summary>
    /// Creates and returns a new command associated with the current database connection.
    /// </summary>
    /// <remarks>The returned command is not automatically associated with a transaction. The caller is
    /// responsible for configuring the command's properties, such as the command text and parameters, before
    /// execution.</remarks>
    /// <returns>An <see cref="IDbCommand"/> object that can be used to execute queries or commands against the data source.</returns>
    IDbCommand CreateCommand();
    /// <summary>
    /// Asynchronously opens the connection, enabling operations that require an open state.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the open operation before it completes.</param>
    /// <returns>A task that represents the asynchronous open operation.</returns>
    [Shim(typeof(DbConnection))] Task OpenAsync(CancellationToken cancellationToken);
}