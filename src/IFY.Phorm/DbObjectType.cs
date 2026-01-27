namespace IFY.Phorm;

/// <summary>
/// Specifies the type of a database object, such as a table, view, or stored procedure.
/// </summary>
public enum DbObjectType
{
    /// <summary>
    /// Use the most appropriate type based on context or default settings.
    /// </summary>
    Default,
    /// <summary>
    /// Use a database stored procedure.
    /// </summary>
    StoredProcedure,
    /// <summary>
    /// Use a database table.
    /// </summary>
    Table,
    /// <summary>
    /// Use a database view.
    /// </summary>
    View
}