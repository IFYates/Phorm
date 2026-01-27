namespace IFY.Phorm;

/// <summary>
/// Specifies metadata for a database contract, indicating the target object type, contract name, and schema for the
/// decorated class or interface.
/// </summary>
/// <remarks>Apply this attribute to a class or interface to define how it maps to a database object, such as a
/// stored procedure or table. The attribute allows customization of the contract's name and schema, which can be useful
/// when the database naming differs from the code. Only one instance of this attribute can be applied to a
/// type.</remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false)]
public class PhormContractAttribute : Attribute
{
    /// <summary>
    /// The type of database object the contract targets.
    /// Note that only <see cref="DbObjectType.StoredProcedure"/> supports receiving data via Call.
    /// </summary>
    public DbObjectType Target { get; set; } = DbObjectType.Default;

    /// <summary>
    /// The contract name to use, if not the same as the decorated class / interface.
    /// </summary>
    public string? Name { get; set; }
    
    /// <summary>
    /// The database schema to use for this contract, if not the default schema of the connection.
    /// </summary>
    public string? Namespace { get; set; }

    /// <summary>
    /// Hint that this contract is read-only and can use a read-only connection.
    /// </summary>
    public bool ReadOnly { get; set; } = false;
}
