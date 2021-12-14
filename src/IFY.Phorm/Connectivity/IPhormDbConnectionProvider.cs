using System;

namespace IFY.Phorm.Connectivity
{
    /// <summary>
    /// Handles provision of existing or new <see cref="IPhormDbConnection"/> instances.
    /// </summary>
    public interface IPhormDbConnectionProvider
    {
        /// <summary>
        /// Allow subscribers to perform actions when a connection is made.
        /// </summary>
        event EventHandler<IPhormDbConnection>? Connected;

        /// <summary>
        /// Get an existing or new connection for the connection context.
        /// </summary>
        /// <param name="connectionName">The name of the connection to scope this connection for.</param>
        /// <returns>A datasource connection for the given context.</returns>
        IPhormDbConnection GetConnection(string? connectionName = null);
    }
}
