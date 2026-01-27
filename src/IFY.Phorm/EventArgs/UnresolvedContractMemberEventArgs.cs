namespace IFY.Phorm.EventArgs;

/// <summary>
/// Provides data for an event that occurs when one or more contract members cannot be resolved during entity
/// construction.
/// </summary>
/// <remarks>This event argument is typically used to notify subscribers when the mapping between data source
/// fields and entity members is incomplete. It allows handlers to inspect which members were not matched and take
/// appropriate action, such as logging or applying custom resolution logic.</remarks>
public sealed class UnresolvedContractMemberEventArgs : System.EventArgs
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
    /// The list of member names that were not matched to the records in the resultset.
    /// </summary>
    public string[] MemberNames { get; internal set; } = [];
}
