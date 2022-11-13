namespace IFY.Phorm.Data;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method)]
public class ContractMemberAttribute : Attribute
{
    /// <summary>
    /// Set to true to ensure that this member is never used as input to the data source.
    /// </summary>
    public bool DisableInput { get; set; }

    /// <summary>
    /// Set to true to ensure that this member never expects to receive output from the data source.
    /// </summary>
    public bool DisableOutput { get; set; }
}
