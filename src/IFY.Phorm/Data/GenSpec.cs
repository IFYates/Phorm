using System;
using System.Collections.Generic;
using System.Linq;

namespace IFY.Phorm.Data
{
    public abstract class GenSpecBase
    {
        internal Type[] SpecTypes { get; }

        internal GenSpecBase()
        {
            SpecTypes = GetType().GenericTypeArguments[1..];
        }

        internal abstract void SetData(IEnumerable<object> data);
    }

    /// <summary>
    /// Fetch a resultset containing "Specialised" instances with a common "Generalised" base type.
    /// </summary>
    /// <typeparam name="TBase">The "Generalised" base type that the other types share.</typeparam>
    /// <typeparam name="T1">A "Specialised" type.</typeparam>
    /// <typeparam name="T2">A "Specialised" type.</typeparam>
    public class GenSpec<TBase, T1, T2> : GenSpecBase
        where T1 : TBase
        where T2 : TBase
    {
        private readonly List<TBase> _data = new List<TBase>();

        public Type GenType { get; } = typeof(TBase);

        internal override void SetData(IEnumerable<object> data)
        {
            _data.AddRange(data.Cast<TBase>());
        }

        /// <summary>
        /// Get all records from the resultset, as the "Generalised" base type.
        /// </summary>
        public TBase[] All() => _data.ToArray();
        /// <summary>
        /// Get all records that are of the specified "Specialised" type.
        /// </summary>
        public IEnumerable<T> OfType<T>() => _data.OfType<T>();
    }

    /// <summary>
    /// Fetch a resultset containing "Specialised" instances with a common "Generalised" base type.
    /// </summary>
    /// <typeparam name="TBase">The "Generalised" base type that the other types share.</typeparam>
    /// <typeparam name="T1">A "Specialised" type.</typeparam>
    /// <typeparam name="T2">A "Specialised" type.</typeparam>
    /// <typeparam name="T3">A "Specialised" type.</typeparam>ToUpperInvariant
    public sealed class GenSpec<TBase, T1, T2, T3> : GenSpec<TBase, T1, T2>
        where T1 : TBase
        where T2 : TBase
        where T3 : TBase
    {
    }
    /// <summary>
    /// Fetch a resultset containing "Specialised" instances with a common "Generalised" base type.
    /// </summary>
    /// <typeparam name="TBase">The "Generalised" base type that the other types share.</typeparam>
    /// <typeparam name="T1">A "Specialised" type.</typeparam>
    /// <typeparam name="T2">A "Specialised" type.</typeparam>
    /// <typeparam name="T3">A "Specialised" type.</typeparam>
    /// <typeparam name="T4">A "Specialised" type.</typeparam>
    public sealed class GenSpec<TBase, T1, T2, T3, T4> : GenSpec<TBase, T1, T2>
        where T1 : TBase
        where T2 : TBase
        where T3 : TBase
        where T4 : TBase
    {
    }
    /// <summary>
    /// Fetch a resultset containing "Specialised" instances with a common "Generalised" base type.
    /// </summary>
    /// <typeparam name="TBase">The "Generalised" base type that the other types share.</typeparam>
    /// <typeparam name="T1">A "Specialised" type.</typeparam>
    /// <typeparam name="T2">A "Specialised" type.</typeparam>
    /// <typeparam name="T3">A "Specialised" type.</typeparam>
    /// <typeparam name="T4">A "Specialised" type.</typeparam>
    /// <typeparam name="T5">A "Specialised" type.</typeparam>
    public sealed class GenSpec<TBase, T1, T2, T3, T4, T5> : GenSpec<TBase, T1, T2>
        where T1 : TBase
        where T2 : TBase
        where T3 : TBase
        where T4 : TBase
        where T5 : TBase
    {
    }
}
