using System;
using System.Collections.Generic;

namespace IFY.Phorm.Data
{
    /// <summary>
    /// A resultset containing "Specialised" instances with a common "Generalised" base type.
    /// </summary>
    internal interface IGenSpecResult
    {
        Type GenType { get; }
        Type[] SpecTypes { get; }

        /// <summary>
        /// Get all records that are of the specified "Specialised" type.
        /// </summary>
        IEnumerable<T> OfType<T>();
    }
}