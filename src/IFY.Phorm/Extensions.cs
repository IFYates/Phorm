using System.Data;
using System.Linq;

namespace IFY.Phorm
{
    public static class Extensions
    {
        public static IDataParameter[] AsParameters(this IDataParameterCollection coll)
        {
            return coll.Cast<IDataParameter>().ToArray();
        }
    }
}
