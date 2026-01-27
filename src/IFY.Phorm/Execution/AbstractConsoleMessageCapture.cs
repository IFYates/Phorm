using IFY.Phorm.EventArgs;

namespace IFY.Phorm.Execution;

/// <summary>
/// Provides base logic for an object that captures console message events.
/// </summary>
public abstract class AbstractConsoleMessageCapture(AbstractPhormSession session, Guid commandGuid)
    : IDisposable
{
    private readonly List<ConsoleMessage> _consoleEvents = [];
    private readonly AbstractPhormSession _session = session;
    private readonly Guid _commandGuid = commandGuid;

    /// <summary>
    /// Becomes true if this instance has captured at least one error.
    /// </summary>
    public bool HasError { get; protected set; }

    /// <summary>
    /// Handles a console message event by recording the message and notifying the current session.
    /// </summary>
    /// <param name="ev">The console message to process. Cannot be null.</param>
    protected void OnConsoleMessage(ConsoleMessage ev)
    {
        _consoleEvents.Add(ev);

        _session.OnConsoleMessage(new ConsoleMessageEventArgs
        {
            CommandGuid = _commandGuid,
            ConsoleMessage = ev
        });
    }

    /// <summary>
    /// Retrieves all console messages that have been recorded.
    /// </summary>
    /// <returns>An array of <see cref="ConsoleMessage"/> objects representing the recorded console messages. The array is empty
    /// if no messages have been recorded.</returns>
    public ConsoleMessage[] GetConsoleMessages() => _consoleEvents.ToArray();

    /// <inheritdoc/>
    public abstract bool ProcessException(Exception ex);

    /// <inheritdoc/>
    public abstract void Dispose();
}
