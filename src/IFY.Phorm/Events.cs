using IFY.Phorm.EventArgs;
using System;

namespace IFY.Phorm
{
    /// <summary>
    /// Global event handlers.
    /// </summary>
    public class Events
    {
        #region CommandExecuting

        /// <summary>
        /// The event invoked when a command is about to be executed.
        /// </summary>
        public static event EventHandler<CommandExecutingEventArgs>? CommandExecuting;

        internal static void OnCommandExecuting(object sender, CommandExecutingEventArgs args)
        {
            try { CommandExecuting?.Invoke(sender, args); } catch { }
        }

        #endregion CommandExecuting

        #region CommandExecuted

        /// <summary>
        /// The event invoked when a command has finished executing.
        /// </summary>
        public static event EventHandler<CommandExecutedEventArgs>? CommandExecuted;

        internal static void OnCommandExecuted(object sender, CommandExecutedEventArgs args)
        {
            try { CommandExecuted?.Invoke(sender, args); } catch { }
        }

        #endregion CommandExecuted

        #region UnexpectedRecordColumn

        /// <summary>
        /// A result record contained a column not specified in the target entity type.
        /// </summary>
        public static event EventHandler<UnexpectedRecordColumnEventArgs>? UnexpectedRecordColumn;

        internal static void OnUnexpectedRecordColumn(object sender, UnexpectedRecordColumnEventArgs args)
        {
            try { UnexpectedRecordColumn?.Invoke(sender, args); } catch { }
        }

        #endregion UnexpectedRecordColumn

        #region UnresolvedContractMember

        /// <summary>
        /// A result record contained a column not specified in the target entity type.
        /// </summary>
        public static event EventHandler<UnresolvedContractMemberEventArgs>? UnresolvedContractMember;

        internal static void OnUnresolvedContractMember(object sender, UnresolvedContractMemberEventArgs args)
        {
            try { UnresolvedContractMember?.Invoke(sender, args); } catch { }
        }

        #endregion UnresolvedContractMember

        #region ConsoleMessage

        /// <summary>
        /// A log message was received during execution.
        /// </summary>
        public static event EventHandler<ConsoleMessageEventArgs>? ConsoleMessage;

        internal static void OnConsoleMessage(object sender, ConsoleMessageEventArgs args)
        {
            try { ConsoleMessage?.Invoke(sender, args); } catch { }
        }

        #endregion ConsoleMessage
    }
}
