namespace IFY.Phorm.EventArgs;

public sealed class CommandExecutedEventArgs : System.EventArgs
{
    /// <summary>
    /// The unique GUID for this command instance.
    /// </summary>
    public Guid CommandGuid { get; internal set; } = Guid.Empty;

    /// <summary>
    /// The text of the command that was executed.
    /// </summary>
    public string CommandText { get; internal set; } = string.Empty;

    /// <summary>
    /// The parameters of the command that was executed.
    /// </summary>
    public Dictionary<string, object?> CommandParameters { get; internal set; } = [];

    /// <summary>
    /// The number of entity results parsed from execution.
    /// </summary>
    public int? ResultCount { get; internal set; }

    /// <summary>
    /// The return value of the execution.
    /// </summary>
    public int ReturnValue { get; internal set; }
}
