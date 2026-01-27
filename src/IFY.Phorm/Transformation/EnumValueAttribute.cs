using System.Reflection;
using System.Runtime.Serialization;

namespace IFY.Phorm.Transformation;

/// <summary>
/// Transform the contract property enum value to/from the same integer or string representation.
/// Both the integer and string representations are always supported for receiving, but sending must specify.
/// </summary>
public class EnumValueAttribute : AbstractTransphormAttribute
{
    /// <summary>
    /// Whether to send the string representation of this enum, or the integer.
    /// Defaults to false.
    /// 
    /// Note: will honour <see cref="EnumMemberAttribute"/> decoration on the enum values, if exist.
    /// </summary>
    public bool SendAsString { get; set; }

    /// <inheritdoc/>
    public override object? FromDatasource(Type type, object? data, object? context)
    {
        var enumType = Nullable.GetUnderlyingType(type) ?? type;
        if (data == null)
        {
            if (enumType != type)
            {
                return null;
            }
            throw new ArgumentNullException(nameof(data), $"Contract property for enum {type.FullName} does not support null.");
        }

        if (data.GetType() == enumType)
        {
            return data;
        }

        return ConvertToEnum(data, enumType);
    }

    /// <summary>
    /// Converts the specified value to an instance of the given enumeration type, supporting both numeric and string
    /// representations.
    /// </summary>
    /// <remarks>If the input is not a string, the method attempts to parse it as a numeric value. If the
    /// input is a string, it first matches against enum member names and EnumMemberAttribute values, ignoring case. If
    /// no match is found, an exception may be thrown. The method does not validate whether the value is defined in the
    /// enumeration.</remarks>
    /// <param name="data">The value to convert to the enumeration type. Can be a numeric value or a string representing either the enum
    /// member name or its associated EnumMemberAttribute value.</param>
    /// <param name="enumType">The type of the enumeration to convert to. Must be a valid enum type.</param>
    /// <returns>An object representing the corresponding value of the specified enumeration type.</returns>
    public static object ConvertToEnum(object data, Type enumType)
    {
        // Favour numeric
        if (data is not string str)
        {
            return Enum.Parse(enumType, $"{data}"); // Will not fail on bad value (TODO: option to check)
        }

        // Supports name by EnumMember first
        var memberMatch = enumType.GetMembers()
            .Where(m => m is FieldInfo fi && fi.IsStatic && fi.IsLiteral)
            .FirstOrDefault(m => m.GetCustomAttribute<EnumMemberAttribute>()?.Value?.Equals(str, StringComparison.OrdinalIgnoreCase) == true);
        return Enum.Parse(enumType, memberMatch?.Name ?? str, true); // Will fail on bad value
    }

    /// <inheritdoc/>
    public override object? ToDatasource(object? data, object? context)
    {
        if (data == null)
        {
            return null;
        }

        var enumType = data.GetType();
        if (!enumType.IsEnum)
        {
            throw new InvalidCastException($"Property is marked with '{nameof(EnumValueAttribute)}', but contained value of type {enumType.FullName}.");
        }

        if (!SendAsString)
        {
            return (int)data;
        }

        var enumValue = enumType.GetMember($"{data}").First();
        var memberAttr = enumValue.GetCustomAttribute<EnumMemberAttribute>();
        return memberAttr?.Value ?? enumValue.Name;
    }
}
