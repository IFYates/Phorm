using IFY.Phorm.Data;
using System;

namespace IFY.Phorm.Transformation
{
    /// <summary>
    /// Base structure for an attribute that can transform data between a contract property type and a databasource.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public abstract class AbstractTransphormAttribute : Attribute, IContractMemberAttribute
    {
        /// <summary>
        /// Transform source data to the required contract property type.
        /// </summary>
        /// <param name="type">The property type of the target property on the contract.</param>
        /// <param name="data">The raw data from the datasource.</param>
        /// <returns>The data to be put in the contract property.</returns>
        public abstract object? FromDatasource(Type type, object? data);

        public virtual void SetContext(object? context) { }

        /// <summary>
        /// Transform contract data to the type expected by the datasource.
        /// </summary>
        /// <param name="data">The current value in the contract.</param>
        /// <returns>The data to be sent to the datasource.</returns>
        public abstract object? ToDatasource(object? data);
    }
}
