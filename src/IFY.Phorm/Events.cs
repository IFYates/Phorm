﻿using IFY.Phorm.EventArgs;
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
    }
}
