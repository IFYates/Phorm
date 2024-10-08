﻿using System.Reflection;

namespace IFY.Phorm.Data;

public abstract class GenSpecBase
{
    internal class SpecDef
    {
        public Type Type { get; }
        public ContractMemberDefinition? GenProperty { get; }
        public object SpecValue { get; }
        public IDictionary<string, ContractMemberDefinition> Members { get; }

        public SpecDef(Type type)
        {
            Type = type;
            var attr = type.GetCustomAttribute<PhormSpecOfAttribute>(false);
            Members = ContractMemberDefinition.GetFromContract(type)
                .ToDictionary(m => m.DbName.ToUpperInvariant());
            if (attr != null)
            {
                GenProperty = Members.Values.FirstOrDefault(m => m.SourceMember!.Name.Equals(attr.GenProperty, StringComparison.InvariantCultureIgnoreCase));
                SpecValue = attr.PropertyValue;
            }
            else
            {
                SpecValue = Type.Missing; // Any non-null
            }
        }
    }

    internal Type GenType { get; }
    internal SpecDef[] SpecDefs { get; }

    private protected GenSpecBase()
    {
        var typeArgs = GetType().GenericTypeArguments;
        GenType = typeArgs[0];

        SpecDefs = typeArgs[1..].Select(t => new SpecDef(t)).ToArray();
        if (SpecDefs.Any(s => s.GenProperty == null))
        {
            throw new InvalidOperationException("Invalid GenSpec usage. Provided type was not decorated with a PhormSpecOfAttribute referencing a valid property: " + SpecDefs.First(s => s.GenProperty == null).Type.FullName);
        }
    }

    internal SpecDef? GetFirstSpecType(Func<ContractMemberDefinition, object?> valueProvider)
    {
        return SpecDefs.FirstOrDefault(s => valueProvider(s.GenProperty!)?.Equals(s.SpecValue) == true);
    }

    internal abstract void SetData(System.Collections.IEnumerable data);
}

public class GenSpecBase<TBase> : GenSpecBase
{
    public int Count() => _data.Count();

    private IEnumerable<TBase> _data = [];

    protected GenSpecBase()
    { }

    internal override void SetData(System.Collections.IEnumerable data)
    {
        _data = data is IEnumerable<TBase> col ? col : data.Cast<TBase>();
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
public class GenSpec<TBase, T1> : GenSpecBase<TBase>
    where T1 : TBase
{
}

/// <summary>
/// Fetch a resultset containing "Specialised" instances with a common "Generalised" base type.
/// </summary>
/// <typeparam name="TBase">The "Generalised" base type that the other types share.</typeparam>
/// <typeparam name="T1">A "Specialised" type.</typeparam>
/// <typeparam name="T2">A "Specialised" type.</typeparam>
public sealed class GenSpec<TBase, T1, T2> : GenSpec<TBase, T1>
    where T1 : TBase
    where T2 : TBase
{
}

/// <summary>
/// Fetch a resultset containing "Specialised" instances with a common "Generalised" base type.
/// </summary>
/// <typeparam name="TBase">The "Generalised" base type that the other types share.</typeparam>
/// <typeparam name="T1">A "Specialised" type.</typeparam>
/// <typeparam name="T2">A "Specialised" type.</typeparam>
/// <typeparam name="T3">A "Specialised" type.</typeparam>
public sealed class GenSpec<TBase, T1, T2, T3> : GenSpec<TBase, T1>
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
public sealed class GenSpec<TBase, T1, T2, T3, T4> : GenSpec<TBase, T1>
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
public sealed class GenSpec<TBase, T1, T2, T3, T4, T5> : GenSpec<TBase, T1>
    where T1 : TBase
    where T2 : TBase
    where T3 : TBase
    where T4 : TBase
    where T5 : TBase
{
}
