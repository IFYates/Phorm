using IFY.Phorm.EventArgs;
using System.ComponentModel;

namespace IFY.Phorm;

/// <summary>
/// Represents a session for executing Phorm contracts and database operations, providing methods for invoking actions,
/// retrieving data, and managing transactions within a scoped database connection.
/// </summary>
/// <remarks>The session exposes events for monitoring connection lifecycle, command execution, and contract
/// mapping issues. It supports both synchronous and asynchronous operations, as well as transaction management if
/// supported by the underlying runner. Configuration properties allow control over error handling and result size
/// strictness. Use this interface to interact with Phorm contracts and database entities in a scoped and configurable
/// manner.</remarks>
public interface IPhormSession : IPhormConnectedSession
{
    #region Events

    /// <summary>
    /// The event invoked when a new database connection is created.
    /// </summary>
    event EventHandler<ConnectedEventArgs> Connected;

    #endregion Events

    /// <summary>
    /// Get a new instance of this session scoped with a different connection name.
    /// </summary>
    /// <param name="connectionName">The connection name to use when scoping the new session instance.</param>
    /// <returns>A new instance of this session with a different connection name.</returns>
    [Obsolete("Use WithContext(string) instead.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    IPhormSession SetConnectionName(string connectionName)
        => WithContext(connectionName, null!);

    /// <summary>
    /// Returns a new session instance that uses the specified connection context.
    /// </summary>
    /// <param name="connectionName">The connection name to use when scoping the new session instance.</param>
    /// <returns>A new <see cref="IPhormSession"/> instance configured to use the specified connection context.</returns>
    /// <remarks>The new session will have an empty <see cref="IPhormConnectedSession.ContextData"/>.</remarks>
    public IPhormSession WithContext(string connectionName)
        => WithContext(connectionName, null!);
    /// <summary>
    /// Creates a new session with the specified context data applied.
    /// </summary>
    /// <remarks>This method returns a new session instance with the updated
    /// context data, persisting any current <see cref="IPhormConnectedSession.ConnectionName"/>.</remarks>
    /// <param name="contextData">A dictionary containing key-value pairs to associate with the session context. Keys must be non-null strings;
    /// values may be any object.</param>
    /// <returns>A new session instance that includes the provided context data.</returns>
    public IPhormSession WithContext(IDictionary<string, object?> contextData)
        => WithContext(ConnectionName, contextData);
    /// <summary>
    /// Creates a new session instance with the specified connection name and additional context data.
    /// </summary>
    /// <param name="connectionName">The connection name to use when scoping the new session instance. Null will remove the current value.</param>
    /// <param name="contextData">A dictionary containing key-value pairs that provide additional context for the session. Cannot be null.</param>
    /// <returns>A new <see cref="IPhormSession"/> instance configured with the specified connection name and context data.</returns>
    IPhormSession WithContext(string? connectionName, IDictionary<string, object?> contextData);
}
