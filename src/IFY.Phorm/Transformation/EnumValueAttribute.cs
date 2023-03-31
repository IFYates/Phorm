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

    internal static object ConvertToEnum(object data, Type enumType)
    {
        // Favour numeric
        if (data is not string str)
        {
            str = $"{data}";
            return Enum.Parse(enumType, str); // Will not fail on bad value (TODO: option to check)
        }

        // Supports name by EnumMember first
        var memberMatch = enumType.GetMembers()
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
