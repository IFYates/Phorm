using IFY.Shimr;
using System.Data.Common;

namespace System.Data;

/// <summary>
/// Exposes the asynchronous parts of <see cref="DbCommand"/>
/// </summary>
public interface IAsyncDbCommand : IDisposable
{
    /// <summary>
    /// Gets or sets the text command to run against the data source.
    /// </summary>
    /// <value>The text command to execute. The default value is an empty string ("").</value>
    string CommandText { get; set; }
    /// <summary>
    /// Indicates or specifies how the <see cref="IDbCommand.CommandText"/> property is interpreted.
    /// </summary>
    /// <value>One of the <see cref="Data.CommandType"/> values. The default is <see cref="CommandType.Text"/>.</value>
    CommandType CommandType { get; set; }
    /// <summary>
    /// Gets or sets the <see cref="IDbConnection"/> used by this instance of the <see cref="IDbCommand"/>.
    /// </summary>
    /// <value>The connection to the data source.</value>
    [Shim(typeof(IDbCommand))] IDbConnection? Connection { get; }
    /// <summary>
    /// Gets the <see cref="IDataParameterCollection"/>.
    /// </summary>
    // Returns:
    //     The parameters of the SQL statement or stored procedure.
    [Shim(typeof(IDbCommand))] IDataParameterCollection Parameters { get; }
    /// <summary>
    /// Gets or sets the transaction within which the Command object of a .NET data provider executes.
    /// </summary>
    /// <value>the Command object of a .NET Framework data provider executes. The default value is null.</value>
    [Shim(typeof(IDbCommand))] IDbTransaction? Transaction { get; set; }

    /// <summary>
    /// Creates a new instance of an <see cref="IDbDataParameter"/> object.
    /// </summary>
    /// <value>An <see cref="IDbDataParameter"/> object.</value>
    [Shim(typeof(IDbCommand))] IDbDataParameter CreateParameter();

    /// <summary>
    /// Executes the <see cref="IDbCommand.CommandText"/> against the <see cref="IDbCommand.Connection"/> and builds an <see cref="IDataReader"/>.
    /// </summary>
    /// <returns>An <see cref="IDataReader"/> object.</returns>
    [Shim(typeof(DbCommand))] Task<DbDataReader> ExecuteReaderAsync(CancellationToken cancellationToken);
}
