using IFY.Phorm.EventArgs;

namespace IFY.Phorm.Execution;

/// <summary>
/// Provides base logic for an object that captures console message events.
/// </summary>
public abstract class AbstractConsoleMessageCapture : IDisposable
{
    private readonly List<ConsoleMessage> _consoleEvents = new List<ConsoleMessage>();
    protected readonly AbstractPhormSession _session;
    protected readonly Guid _commandGuid;

    /// <summary>
    /// Becomes true if this instance has captured at least one error.
    /// </summary>
    public bool HasError { get; protected set; }

    protected AbstractConsoleMessageCapture(AbstractPhormSession session, Guid commandGuid)
    {
        _session = session;
        _commandGuid = commandGuid;
    }

    protected void OnConsoleMessage(ConsoleMessage ev)
    {
        _consoleEvents.Add(ev);

        _session.OnConsoleMessage(new ConsoleMessageEventArgs
        {
            CommandGuid = _commandGuid,
            ConsoleMessage = ev
        });
    }

    public ConsoleMessage[] GetConsoleMessages() => _consoleEvents.ToArray();

    public abstract bool ProcessException(Exception ex);

    public abstract void Dispose();
}
