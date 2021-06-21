using System;

namespace IFY.Phorm.Transformation
{
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
        /// Note: will honour <see cref="System.Runtime.Serialization.EnumMemberAttribute"/> decoration on the enum values, if exist.
        /// </summary>
        public bool SendAsString { get; init; }

        public override object? FromDatasource(Type type, object? data)
        {
            var enumType = Nullable.GetUnderlyingType(type) ?? type;
            var str = data?.ToString();
            if (str == null)
            {
                if (enumType != type)
                {
                    return null;
                }
                throw new ArgumentNullException(nameof(data), $"Contract property for enum {type.FullName} does not support null.");
            }

            if (data?.GetType() == enumType)
            {
                return data;
            }

            // Supports name or integer already
            // TODO: EnumMember value
            // TODO: Bad numeric values? e.g., <min, >max
            return Enum.Parse(enumType, str, true); // Will fail on bad value
        }

        public override object? ToDatasource(object? data)
        {
            if (data == null)
            {
                return null;
            }

            var enumType = Nullable.GetUnderlyingType(data.GetType()) ?? data.GetType();
            if (!enumType.IsEnum)
            {
                throw new InvalidCastException($"Property is marked with '{nameof(EnumValueAttribute)}', but contained value of type {enumType.GetType().FullName}.");
            }

            // TODO: EnumMember value
            return SendAsString ? data.ToString() : (int)data;
        }
    }
}
