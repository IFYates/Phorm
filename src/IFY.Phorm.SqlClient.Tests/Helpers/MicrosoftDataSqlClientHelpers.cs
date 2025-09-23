using Microsoft.Data.SqlClient;
using System.Reflection;

namespace IFY.Phorm.SqlClient.Tests.Helpers;

/// <summary>
/// Provides helper methods for creating and manipulating internal Microsoft.Data.SqlClient types, such as SqlException,
/// SqlError, and related event arguments, primarily for advanced scenarios or testing purposes.
/// </summary>
public static class MicrosoftDataSqlClientHelpers
{
    public static SqlException NewSqlException(string message, SqlErrorCollection errs)
    {
        return (SqlException)typeof(SqlException).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
            .First().Invoke([message, errs, null!, Guid.Empty]);
    }

    public static SqlError NewSqlError(int number, byte state, byte errorClass, string server, string message, string procedure, int lineNumber, uint win32ErrCode, Exception innerException)
    {
        return (SqlError)typeof(SqlError).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
            .First().Invoke([number, state, errorClass, server, message, procedure, lineNumber, (int)win32ErrCode, innerException]);
    }

    public static SqlErrorCollection NewSqlErrorCollection()
    {
        return (SqlErrorCollection)Activator.CreateInstance(typeof(SqlErrorCollection), true)!;
    }

    public static SqlInfoMessageEventArgs NewSqlInfoMessageEventArgs(SqlException ex)
    {
        return (SqlInfoMessageEventArgs)typeof(SqlInfoMessageEventArgs).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
            .First().Invoke([ex]);
    }

    public static void AddErrorToCollection(SqlErrorCollection coll, SqlError err)
    {
        var addErr = coll.GetType().GetMethod("Add", BindingFlags.Instance | BindingFlags.NonPublic);
        addErr!.Invoke(coll, [err]);
    }

    public static void FireInfoMessageEvent(SqlConnection conn, SqlInfoMessageEventArgs e)
    {
        var ev = (MulticastDelegate)typeof(SqlConnection).GetField("InfoMessage", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(conn)!;
        foreach (var handler in ev?.GetInvocationList() ?? [])
        {
            handler.Method.Invoke(handler.Target, [conn, e]);
        }
    }
}
