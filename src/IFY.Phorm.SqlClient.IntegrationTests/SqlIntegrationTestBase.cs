using IFY.Phorm.EventArgs;
using IFY.Phorm.Execution;
using System.Diagnostics.CodeAnalysis;

namespace IFY.Phorm.SqlClient.IntegrationTests;

[ExcludeFromCodeCoverage]
[DoNotParallelize]
public abstract class SqlIntegrationTestBase
{
    public required TestContext TestContext { get; set; }

    protected Action<object?, CommandExecutingEventArgs>? _globalCommandExecuting = null;
    private void invokeHandler(object? sender, CommandExecutingEventArgs args) => _globalCommandExecuting?.Invoke(sender, args);
    protected Action<object?, CommandExecutedEventArgs>? _globalCommandExecuted = null;
    private void invokeHandler(object? sender, CommandExecutedEventArgs args) => _globalCommandExecuted?.Invoke(sender, args);
    protected Action<object?, UnexpectedRecordColumnEventArgs>? _globalUnexpectedRecordColumn = null;
    private void invokeHandler(object? sender, UnexpectedRecordColumnEventArgs args) => _globalUnexpectedRecordColumn?.Invoke(sender, args);
    protected Action<object?, UnresolvedContractMemberEventArgs>? _globalUnresolvedContractMember = null;
    private void invokeHandler(object? sender, UnresolvedContractMemberEventArgs args) => _globalUnresolvedContractMember?.Invoke(sender, args);
    protected Action<object?, ConsoleMessageEventArgs>? _globalConsoleMessage = null;
    private void invokeHandler(object? sender, ConsoleMessageEventArgs args) => _globalConsoleMessage?.Invoke(sender, args);

    protected void enableGlobalEventHandlers()
    {
        Events.CommandExecuting += invokeHandler;
        Events.CommandExecuted += invokeHandler;
        Events.UnexpectedRecordColumn += invokeHandler;
        Events.UnresolvedContractMember += invokeHandler;
        Events.ConsoleMessage += invokeHandler;
    }
    protected void disableGlobalEventHandlers()
    {
        Events.CommandExecuting -= invokeHandler;
        Events.CommandExecuted -= invokeHandler;
        Events.UnexpectedRecordColumn -= invokeHandler;
        Events.UnresolvedContractMember -= invokeHandler;
        Events.ConsoleMessage -= invokeHandler;
    }

    protected static AbstractPhormSession getPhormSession(string? connectionName = null)
    {
        return new SqlPhormSession(@"Server=(localdb)\ProjectModels;Database=PhormTests;MultipleActiveResultSets=True", connectionName);
    }
}
