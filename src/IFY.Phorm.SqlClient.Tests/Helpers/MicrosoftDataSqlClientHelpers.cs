using Microsoft.Data.SqlClient;
using System;
using System.Linq;
using System.Reflection;

namespace IFY.Phorm.SqlClient.Tests.Helpers
{
    public static class MicrosoftDataSqlClientHelpers
    {
        public static SqlException NewSqlException(string message, SqlErrorCollection errs)
        {
            return (SqlException)typeof(SqlException).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
                .First().Invoke(new object[] { message, errs, null!, Guid.Empty });
        }

        public static SqlError NewSqlError(int number, byte state, byte errorClass, string server, string message, string procedure, int lineNumber, uint win32ErrCode, Exception innerException)
        {
            return (SqlError)typeof(SqlError).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
                .First().Invoke(new object[] { number, state, errorClass, server, message, procedure, lineNumber, win32ErrCode, innerException });
        }

        public static SqlErrorCollection NewSqlErrorCollection()
        {
            return (SqlErrorCollection)Activator.CreateInstance(typeof(SqlErrorCollection), true)!;
        }

        public static SqlInfoMessageEventArgs NewSqlInfoMessageEventArgs(SqlException ex)
        {
            return (SqlInfoMessageEventArgs)typeof(SqlInfoMessageEventArgs).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
                .First().Invoke(new object[] { ex });
        }

        public static void AddErrorToCollection(SqlErrorCollection coll, SqlError err)
        {
            var addErr = coll.GetType().GetMethod("Add", BindingFlags.Instance | BindingFlags.NonPublic);
            addErr!.Invoke(coll, new object[] { err });
        }

        public static void FireInfoMessageEvent(SqlConnection conn, SqlInfoMessageEventArgs e)
        {
            var ev = (MulticastDelegate)typeof(SqlConnection).GetField("InfoMessage", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(conn)!;
            foreach (var handler in ev?.GetInvocationList() ?? Array.Empty<Delegate>())
            {
                handler.Method.Invoke(handler.Target, new object[] { conn, e });
            }
        }
    }
}
