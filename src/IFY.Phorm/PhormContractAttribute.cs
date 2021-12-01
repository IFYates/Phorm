using System;

namespace IFY.Phorm
{
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
        public string? Name { get; init; }
        
        /// <summary>
        /// The database schema to use for this contract, if not the default schema of the connection.
        /// </summary>
        public string? Namespace { get; init; }
    }
}