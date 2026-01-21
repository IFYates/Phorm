namespace IFY.Phorm.Execution;

/// <summary>
/// Represents a message written to the console, including its content, severity, and source information.
/// </summary>
/// <remarks>Use this record to encapsulate information about console output, such as log entries, errors, or
/// informational messages. The properties provide details about the message's severity, origin, and content, which can
/// be useful for logging, diagnostics, or user feedback scenarios.</remarks>
public sealed record ConsoleMessage
{
    /// <summary>
    /// Whether this message is for an error that interrupted execution.
    /// </summary>
    public bool IsError
    {
        get;
#if !NET5_0_OR_GREATER
        set;
#else
        init;
#endif
    }

    /// <summary>
    /// The level of the log message.
    /// This is implementation specific.
    /// </summary>
    public int Level
    {
        get;
#if !NET5_0_OR_GREATER
        set;
#else
        init;
#endif
    }

    /// <summary>
    /// If supported, the file/procedure that raised the event, and any location info.
    /// </summary>
    public string? Source
    {
        get;
#if !NET5_0_OR_GREATER
        set;
#else
        init;
#endif
    }

    /// <summary>
    /// The content of the log message.
    /// </summary>
    public string Message
    {
        get;
#if !NET5_0_OR_GREATER
        set;
#else
        init;
#endif
    } = string.Empty;
}
