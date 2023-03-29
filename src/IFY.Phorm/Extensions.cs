﻿using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace IFY.Phorm;

internal static class Extensions
{
    public static IDataParameter[] AsParameters(this IDataParameterCollection coll)
    {
        return coll.Cast<IDataParameter>().ToArray();
    }

    [return: NotNullIfNotNull("value")]
    public static object? ChangeType(this object? value, Type conversionType)
    {
        if (value != null)
        {
            if (conversionType.IsInstanceOfType(value))
            {
                return value;
            }

#if NET6_0_OR_GREATER
            if (conversionType == typeof(DateOnly))
            {
                var dt = (DateTime)Convert.ChangeType(value, typeof(DateTime));
                return DateOnly.FromDateTime(dt);
            }
#endif
        }

        return Convert.ChangeType(value, conversionType);
    }

    public static byte[] GetBytes(this object? value)
    {
        if (value == null)
        {
            return Array.Empty<byte>();
        }

#if NET6_0_OR_GREATER
        if (value is DateOnly date)
        {
            value = (date.Year * 366) + (date.DayOfYear - 1);
        }
#endif
        else if (value is DateTime dt)
        {
            value = dt.Ticks;
        }
        else if (value is decimal dec)
        {
            var ints = decimal.GetBits(dec);
            var bytes = new byte[16];
            Array.Copy(BitConverter.GetBytes(ints[0]), 0, bytes, 0, 4);
            Array.Copy(BitConverter.GetBytes(ints[1]), 0, bytes, 4, 4);
            Array.Copy(BitConverter.GetBytes(ints[2]), 0, bytes, 8, 4);
            Array.Copy(BitConverter.GetBytes(ints[3]), 0, bytes, 12, 4);
            return bytes;
        }

        return value switch
        {
            byte[] val => val,
            byte val => new[] { val },
            char val => BitConverter.GetBytes(val),
            double val => BitConverter.GetBytes(val),
            float val => BitConverter.GetBytes(val),
            Guid val => val.ToByteArray(),
            int val => BitConverter.GetBytes(val),
            long val => BitConverter.GetBytes(val),
            short val => BitConverter.GetBytes(val),
            string val => Encoding.UTF8.GetBytes(val),
            _ => throw new InvalidCastException(),
        };
    }

    public static PropertyInfo[] GetReferencedObjectProperties(this Expression expr, Type objectType)
    {
        var props = new List<PropertyInfo>();
        parseExpression(expr);
        return props.ToArray();

        void parseExpression(Expression expr)
        {
            switch (expr)
            {
                case BinaryExpression be:
                    parseExpression(be.Left);
                    parseExpression(be.Right);
                    break;
                case ConditionalExpression ce:
                    parseExpression(ce.Test);
                    parseExpression(ce.IfTrue);
                    parseExpression(ce.IfFalse);
                    break;
                case ConstantExpression:
                    break;
                case MethodCallExpression mce:
                    if (mce.Method.IsStatic)
                    {
                        foreach (var arg in mce.Arguments)
                        {
                            parseExpression(arg);
                        }
                        break;
                    }
                    if (mce.Object != null)
                    {
                        parseExpression(mce.Object);
                        break;
                    }
                    throw new NotSupportedException($"Phorm resultset filtering does not support instance method calls ({mce}).");
                case MemberExpression me:
                    if (me.Member is PropertyInfo pi)
                    {
                        if (pi.GetGetMethod()?.IsStatic == true)
                        {
                            // Reading a static property is safe
                            break;
                        }
                        if (me.Member.DeclaringType == objectType)
                        {
                            props.Add(pi);
                            break;
                        }
                    }
                    if (me.Member is FieldInfo fi
                        && fi.IsStatic)
                    {
                        // Reading a static field is safe
                        break;
                    }
                    if (me.Expression != null)
                    {
                        parseExpression(me.Expression);
                        break;
                    }
                    throw new NotSupportedException($"Phorm resultset filtering does not support accessing instance members of other objects ({me}).");
                case UnaryExpression ue:
                    if (ue.NodeType is ExpressionType.Convert or ExpressionType.Not)
                    {
                        parseExpression(ue.Operand);
                        break;
                    }
                    throw new NotSupportedException();
                default:
                    throw new NotImplementedException();
            }
        }
    }

    public static T? FromBytes<T>(this byte[]? bytes)
        => (T?)FromBytes(bytes, typeof(T));
    public static object? FromBytes(this byte[]? bytes, Type resultType)
    {
        if (bytes == null)
        {
            return default;
        }

        if (resultType == typeof(byte[]))
        {
            return bytes;
        }
#if NET6_0_OR_GREATER
        if (resultType == typeof(DateOnly))
        {
            var dateVal = BitConverter.ToInt32(bytes);
            return new DateOnly(dateVal / 366, 1, 1)
                .AddDays(dateVal % 366);
        }
#endif
        if (resultType == typeof(DateTime))
        {
            return new DateTime(BitConverter.ToInt64(bytes));
        }
        if (resultType == typeof(decimal))
        {
            var bits = new int[4];
            var arr = new byte[4];
            Array.Copy(bytes, 0, arr, 0, 4);
            bits[0] = BitConverter.ToInt32(arr);
            Array.Copy(bytes, 4, arr, 0, 4);
            bits[1] = BitConverter.ToInt32(arr);
            Array.Copy(bytes, 8, arr, 0, 4);
            bits[2] = BitConverter.ToInt32(arr);
            Array.Copy(bytes, 12, arr, 0, 4);
            bits[3] = BitConverter.ToInt32(arr);
            return new decimal(bits);
        }
        if (resultType == typeof(string))
        {
            return Encoding.UTF8.GetString(bytes);
        }

        var def = Activator.CreateInstance(resultType);
        return def switch
        {
            byte => bytes.Single(),
            char => BitConverter.ToChar(bytes),
            double => BitConverter.ToDouble(bytes),
            float => BitConverter.ToSingle(bytes),
            Guid => new Guid(bytes),
            int => BitConverter.ToInt32(bytes),
            long => BitConverter.ToInt64(bytes),
            short => BitConverter.ToInt16(bytes),
            _ => throw new InvalidCastException(),
        };
    }
}
