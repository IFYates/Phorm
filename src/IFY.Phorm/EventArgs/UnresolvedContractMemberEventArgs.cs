namespace IFY.Phorm.EventArgs;

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
    public string[] MemberNames { get; internal set; } = Array.Empty<string>();
}
