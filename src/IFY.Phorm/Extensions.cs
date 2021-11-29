using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace IFY.Phorm
{
    public static class Extensions
    {
        public static IDataParameter[] AsParameters(this IDataParameterCollection coll)
        {
            return coll.Cast<IDataParameter>().ToArray();
        }

        public static bool TrySingle<T>(this IEnumerable<T> coll, Func<T, bool> predicate, [MaybeNullWhen(false)] out T result)
        {
            result = coll.SingleOrDefault(predicate);
            return result != null;
        }
    }
}
