using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace System.Data
{
    /// <summary>
    /// Exposes the asynchronous parts of <see cref="DbCommand"/>
    /// </summary>
    public interface IAsyncDbCommand //: IDbCommand
    {
        Task<DbDataReader> ExecuteReaderAsync(CancellationToken cancellationToken);
    }
}
