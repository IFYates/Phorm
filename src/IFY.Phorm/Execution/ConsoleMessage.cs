namespace IFY.Phorm.Execution;

public record ConsoleMessage
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
