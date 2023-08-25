using System.Data;
using System.Diagnostics.CodeAnalysis;

namespace IFY.Phorm.Tests;

/// <summary>
/// Mockable test object with useful default implementation.
/// </summary>
[ExcludeFromCodeCoverage]
internal class TestDbDataParameter : IDbDataParameter
{
    public virtual byte Precision { get; set; }
    public virtual byte Scale { get; set; }
    public virtual int Size { get; set; }
    public virtual DbType DbType { get; set; }
    public virtual ParameterDirection Direction { get; set; }

    public virtual bool IsNullable { get; } = true;

    [AllowNull] public virtual string ParameterName { get; set; } = string.Empty;
    [AllowNull] public virtual string SourceColumn { get; set; } = string.Empty;
    public virtual DataRowVersion SourceVersion { get; set; }
    public virtual object? Value { get; set; }
}
