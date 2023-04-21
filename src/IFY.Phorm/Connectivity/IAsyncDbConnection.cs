using IFY.Shimr;
using System.Data.Common;

namespace System.Data;

/// <summary>
/// Exposes the asynchronous parts of <see cref="DbCommand"/>
/// </summary>
public interface IAsyncDbCommand : IDisposable
{
    string CommandText { get; set; }
    CommandType CommandType { get; set; }
    [Shim(typeof(IDbCommand))] IDbConnection? Connection { get; }
    [Shim(typeof(IDbCommand))] IDataParameterCollection Parameters { get; }
    [Shim(typeof(IDbCommand))] IDbTransaction? Transaction { get; set; }

    [Shim(typeof(IDbCommand))] IDbDataParameter CreateParameter();
    Task<DbDataReader> ExecuteReaderAsync(CancellationToken cancellationToken);
}
