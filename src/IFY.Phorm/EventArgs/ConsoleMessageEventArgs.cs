using IFY.Phorm.Execution;

namespace IFY.Phorm.EventArgs;

/// <summary>
/// Provides data for events that report messages written to the console by a command instance.
/// </summary>
/// <remarks>Use this class to access information about a console message generated during the execution of a
/// command, including the unique identifier of the command and the message details. Instances of this class are
/// typically provided to event handlers for console message events.</remarks>
public sealed class ConsoleMessageEventArgs : System.EventArgs
{
    /// <summary>
    /// The unique GUID for the command instance that raised this event.
    /// </summary>
    public Guid CommandGuid { get; internal set; }

    /// <summary>
    /// The console message that was raised.
    /// </summary>
    public ConsoleMessage ConsoleMessage { get; internal set; } = new ConsoleMessage();
}
