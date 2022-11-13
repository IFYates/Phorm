using IFY.Phorm.Execution;

namespace IFY.Phorm.EventArgs;

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
