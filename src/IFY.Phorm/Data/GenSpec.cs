using System.Reflection;

namespace IFY.Phorm.Data;

/// <summary>
/// Provides a base class for defining generic specification types used in contract member generation scenarios.
/// </summary>
/// <remarks>This abstract class is intended for advanced scenarios involving dynamic contract member mapping and
/// specification resolution. It is typically used as a foundation for derived types that implement custom logic for
/// handling specification data and member definitions. The class is not intended for direct instantiation and is
/// designed for internal use within the specification framework.</remarks>
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

/// <summary>
/// Provides a generic base class for working with collections of records, allowing access to all items as the base type
/// and filtering by specialised types.
/// </summary>
/// <remarks>This class enables retrieval of all records as the specified base type and supports filtering records
/// by derived or specialised types. It is intended to be used as a foundation for more specific collection types that
/// require generalised access and type-based filtering.</remarks>
/// <typeparam name="TBase">The base type of the records contained in the collection.</typeparam>
public class GenSpecBase<TBase> : GenSpecBase
{
    /// <summary>
    /// Returns the number of elements contained in the collection.
    /// </summary>
    /// <returns>The total number of elements in the collection.</returns>
    public int Count() => _data.Count();

    private IEnumerable<TBase> _data = [];

    /// <summary>
    /// Initializes a new instance of the GenSpecBase class.
    /// </summary>
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
