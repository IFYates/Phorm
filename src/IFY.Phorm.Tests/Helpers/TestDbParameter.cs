using System.Data;
using System.Diagnostics.CodeAnalysis;

namespace IFY.Phorm.Tests
{
    /// <summary>
    /// Mockable test object with useful default implementation.
    /// </summary>
    [ExcludeFromCodeCoverage]
    internal class TestDbParameter : IDbDataParameter
    {
        public virtual byte Precision { get; set; }
        public virtual byte Scale { get; set; }
        public virtual int Size { get; set; }
        public virtual DbType DbType { get; set; }
        public virtual ParameterDirection Direction { get; set; }

        public virtual bool IsNullable { get; } = true;

        public virtual string ParameterName { get; [param: AllowNull] set; } = string.Empty;
        public virtual string SourceColumn { get; [param: AllowNull] set; } = string.Empty;
        public virtual DataRowVersion SourceVersion { get; set; }
        public virtual object? Value { get; set; }
    }
}
