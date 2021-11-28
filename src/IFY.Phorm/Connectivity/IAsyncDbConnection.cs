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
        public IDataParameterCollection Parameters { get; }

        public IDbDataParameter CreateParameter();
        Task<DbDataReader> ExecuteReaderAsync(CancellationToken cancellationToken);
    }
}
