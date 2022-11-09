using System;
using System.Collections.Generic;

namespace IFY.Phorm.EventArgs
{
    public sealed class CommandExecutingEventArgs : System.EventArgs
    {
        /// <summary>
        /// The unique GUID for this command instance.
        /// </summary>
        public Guid CommandGuid { get; internal set; } = Guid.Empty;

        /// <summary>
        /// The text of the command being executed.
        /// </summary>
        public string CommandText { get; internal set; } = string.Empty;

        /// <summary>
        /// The parameters of the command being executed.
        /// </summary>
        public Dictionary<string, object?> CommandParameters { get; internal set; } = new Dictionary<string, object?>();
    }
}
