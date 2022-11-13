using IFY.Phorm.Execution;
using System.Data;

namespace IFY.Phorm.SqlClient.IntegrationTests;

internal class SqlTestHelpers
{
    public static void ApplySql(AbstractPhormSession connProv, string sql)
    {
        using var conn = connProv.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandType = CommandType.Text;
        cmd.CommandText = sql;
        _ = cmd.ExecuteReaderAsync(CancellationToken.None).Result.Read();
    }
}