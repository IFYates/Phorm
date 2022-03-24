using IFY.Phorm.EventArgs;
using System;
using System.Diagnostics.CodeAnalysis;

namespace IFY.Phorm.SqlClient.IntegrationTests
{
    [ExcludeFromCodeCoverage]
    public abstract class SqlIntegrationTestBase
    {
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

        protected static IPhormSession getPhormSession(string? connectionName = null) => getPhormSession(out _, connectionName);
        protected static IPhormSession getPhormSession(out SqlConnectionProvider connProv, string? connectionName = null)
        {
            connProv = new SqlConnectionProvider(@"Server=(localdb)\ProjectModels;Database=PhormTests;");

            var phorm = new SqlPhormSession(connProv, connectionName);
            return phorm;
        }
    }
}
