using IFY.Phorm.Execution;

namespace IFY.Phorm.Tests;

internal class TestConsoleMessageCapture : AbstractConsoleMessageCapture
{
    private readonly TestPhormSession _session;

    public Func<Exception, bool>? ProcessExceptionLogic { get; set; }

    public TestConsoleMessageCapture(TestPhormSession session, Guid commandGuid)
        : base(session, commandGuid)
    {
        _session = session;
        sendAllMessages();
    }

    private void sendAllMessages()
    {
        var messages = _session.ConsoleMessages.ToArray();
        _session.ConsoleMessages.Clear();
        foreach (var message in messages)
        {
            OnConsoleMessage(new ConsoleMessage
            {
                Level = message.Level,
                Source = message.Source,
                Message = message.Message
            });
        }
    }

    public override bool ProcessException(Exception ex)
    {
        return ProcessExceptionLogic?.Invoke(ex)
            ?? false;
    }

    public override void Dispose()
    {
        sendAllMessages();
    }
}