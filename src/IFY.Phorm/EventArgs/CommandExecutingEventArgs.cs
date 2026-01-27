namespace IFY.Phorm.EventArgs;

/// <summary>
/// Provides data for an event that occurs before a command is executed.
/// </summary>
/// <remarks>This class supplies information about the command being executed, including its unique identifier,
/// text, and parameters. It is typically used in event handlers to inspect or log command details prior to
/// execution.</remarks>
public sealed class CommandExecutingEventArgs : System.EventArgs
{
    /// <summary>
    /// The unique GUID for this command instance.
    /// </summary>
    public Guid CommandGuid { get; internal set; } = Guid.Empty;

    /// <summary>
    /// The text of the command being executed.
    /// </summary>
    public string CommandText { get; internal set; } = string.Empty;

    /// <summary>
    /// The parameters of the command being executed.
    /// </summary>
    public Dictionary<string, object?> CommandParameters { get; internal set; } = [];
}
