using System.Data;

namespace IFY.Phorm.Connectivity
{
    public interface IPhormDbConnection : IDbConnection
    {
        /// <summary>
        /// The context name of this connection
        /// </summary>
        string? ConnectionName { get; }
        
        /// <summary>
        /// The default schema of this connection
        /// </summary>
        string DefaultSchema { get; set; }

        /// <summary>
        /// Creates and returns a <see cref="IAsyncDbCommand"/> object associated with the current connection.
        /// </summary>
        new IAsyncDbCommand CreateCommand();
    }
}
