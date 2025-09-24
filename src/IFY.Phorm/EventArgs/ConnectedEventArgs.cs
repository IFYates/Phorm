using IFY.Phorm.Connectivity;

namespace IFY.Phorm.EventArgs;

/// <summary>
/// Provides data for events that are raised when a new database connection is established.
/// </summary>
public sealed class ConnectedEventArgs : System.EventArgs
{
    /// <summary>
    /// The connection that has been created.
    /// </summary>
    public IPhormDbConnection Connection { get; internal set; } = null!;
}
