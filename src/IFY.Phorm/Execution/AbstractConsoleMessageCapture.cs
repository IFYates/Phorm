using IFY.Phorm.EventArgs;

namespace IFY.Phorm.Execution;

/// <summary>
/// Provides base logic for an object that captures console message events.
/// </summary>
public abstract class AbstractConsoleMessageCapture(AbstractPhormSession session, Guid commandGuid)
    : IDisposable
{
    private readonly List<ConsoleMessage> _consoleEvents = [];
    protected readonly AbstractPhormSession _session = session;
    protected readonly Guid _commandGuid = commandGuid;

    /// <summary>
    /// Becomes true if this instance has captured at least one error.
    /// </summary>
    public bool HasError { get; protected set; }

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

    /// <inheritdoc/>
    public abstract bool ProcessException(Exception ex);

    /// <inheritdoc/>
    public abstract void Dispose();
}
