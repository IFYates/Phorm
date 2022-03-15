﻿namespace IFY.Phorm.Execution
{
    public class ConsoleMessage
    {
        /// <summary>
        /// The level of the log message.
        /// This is implementation specific.
        /// </summary>
        public int Level { get; set; }
        /// <summary>
        /// If supported, the file/procedure that raised the event, and any location info.
        /// </summary>
        public string? Source { get; set; }
        /// <summary>
        /// The content of the log message.
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }
}
