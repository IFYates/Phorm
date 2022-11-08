namespace IFY.Phorm.Execution
{
#if !NET5_0_OR_GREATER
    public class ConsoleMessage
#else
    public record ConsoleMessage
#endif
    {
        /// <summary>
        /// Whether this message is for an error that interrupted execution.
        /// </summary>
        public bool IsError { get; set; } = false;
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
