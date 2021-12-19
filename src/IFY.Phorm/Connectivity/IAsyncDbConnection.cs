using Shimterface;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace System.Data
{
    /// <summary>
    /// Exposes the asynchronous parts of <see cref="DbCommand"/>
    /// </summary>
    public interface IAsyncDbCommand : IDisposable
    {
        string CommandText { get; set; }
        CommandType CommandType { get; set; }
        [Shim(typeof(IDbCommand))] IDataParameterCollection Parameters { get; }

        [Shim(typeof(IDbCommand))]
        IDbDataParameter CreateParameter();
        Task<DbDataReader> ExecuteReaderAsync(CancellationToken cancellationToken);
    }
}
