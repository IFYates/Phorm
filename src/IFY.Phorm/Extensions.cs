using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace IFY.Phorm
{
    internal static class Extensions
    {
        public static IDataParameter[] AsParameters(this IDataParameterCollection coll)
        {
            return coll.Cast<IDataParameter>().ToArray();
        }

        public static byte[] GetBytes(this object? value)
        {
            if (value == null)
            {
                return Array.Empty<byte>();
            }

            if (value is decimal dec)
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
                byte val => BitConverter.GetBytes(val),
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

        public static bool TrySingle<T>(this IEnumerable<T> coll, Func<T, bool> predicate, [MaybeNullWhen(false)] out T result)
        {
            result = coll.SingleOrDefault(predicate);
            return result != null;
        }
    }
}
