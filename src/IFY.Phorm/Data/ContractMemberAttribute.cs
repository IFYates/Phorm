namespace IFY.Phorm.Data;

/// <summary>
/// Specifies contract-related metadata for a property or method, indicating whether it should be considered for input
/// or output in data source operations.
/// </summary>
/// <remarks>Apply this attribute to properties or methods to control their participation in contract-based data
/// exchange. Setting the relevant properties allows fine-grained control over which members are used for input or
/// output when interacting with a data source.</remarks>
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
