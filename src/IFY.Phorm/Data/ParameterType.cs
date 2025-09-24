namespace IFY.Phorm.Data;

/// <summary>
/// Specifies the type of a parameter used in data operations, indicating its direction or purpose within a command or
/// procedure.
/// </summary>
/// <remarks>This enumeration is commonly used to define whether a parameter is intended for input, output, both
/// input and output, as a return value, or to represent concatenated console output. The values for Input, Output,
/// InputOutput, and ReturnValue correspond to those in <see cref="System.Data.ParameterDirection"/> and can be used for
/// interoperability with data access APIs.</remarks>
public enum ParameterType
{
    /// <summary>
    /// Indicates that the item represents an input value to a procedure.
    /// </summary>
    Input = 1,
    /// <summary>
    /// Indicates that the item represents an output parameter from a procedure.
    /// </summary>
    Output = 2,
    /// <summary>
    /// Indicates that the item represents a procedure parameter that can be used for both input and output.
    /// </summary>
    InputOutput = 3,
    /// <summary>
    /// Indicates that the item models the return value of a procedure.
    /// </summary>
    ReturnValue = 6,

    /// <summary>
    /// Concatenated output of any printed data
    /// </summary>
    Console = 100,
}
