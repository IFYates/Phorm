namespace IFY.Phorm.Data;

/// <summary>
/// Gets the value returned from the stored procedure as an integer.
/// </summary>
public sealed class ReturnValueMember : ContractMember
{
    /// <summary>
    /// Value returned from stored procedure.
    /// </summary>
    public new int Value => (int)base.Value!;

    internal ReturnValueMember()
        : base("return", 0, ParameterType.ReturnValue, typeof(int))
    {
    }
}