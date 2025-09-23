namespace IFY.Phorm.Data;

/// <summary>
/// Represents an output contract member that provides strongly typed access to the output value of a contract
/// parameter.
/// </summary>
/// <remarks>Use this class to retrieve output values from contract invocations in a type-safe manner. This type
/// is typically constructed internally and returned by contract execution APIs.</remarks>
/// <typeparam name="T">The type of the value held by the contract output member.</typeparam>
public sealed class ContractOutMember<T> : ContractMember
{
    /// <summary>
    /// Value being passed to or returned from stored procedure.
    /// </summary>
    public new T Value => (T)base.Value!;

    internal ContractOutMember()
        : base(null, default, ParameterType.Output, typeof(T))
    { }
    internal ContractOutMember(T value)
        : base(null, value, ParameterType.InputOutput, typeof(T))
    { }
}
