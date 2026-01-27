namespace IFY.Phorm.EventArgs;

/// <summary>
/// Provides data for an event that is raised when a record column cannot be mapped to a property of the target entity
/// type during data processing.
/// </summary>
/// <remarks>This event argument supplies information about the command instance, the entity type being
/// constructed, and the name of the unmapped column. It can be used to log, handle, or inspect unexpected columns
/// encountered during record-to-entity mapping operations.</remarks>
public sealed class UnexpectedRecordColumnEventArgs : System.EventArgs
{
    /// <summary>
    /// The unique GUID for the command instance that raised this event.
    /// </summary>
    public Guid CommandGuid { get; internal set; }

    /// <summary>
    /// The type of entity being constructed.
    /// </summary>
    public Type EntityType { get; internal set; } = null!;

    /// <summary>
    /// The name of the record column that cannot be mapped to the entity.
    /// </summary>
    public string ColumnName { get; internal set; } = string.Empty;
}
