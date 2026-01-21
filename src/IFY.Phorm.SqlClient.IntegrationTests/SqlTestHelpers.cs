using IFY.Phorm.Execution;
using System.Data;

namespace IFY.Phorm.SqlClient.IntegrationTests;

internal class SqlTestHelpers
{
    public static async Task ApplySql(AbstractPhormSession connProv, CancellationToken cancellationToken, params string[] scripts)
    {
        using var conn = connProv.GetConnection(false);
        foreach (var sql in scripts)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = sql;
            (await cmd.ExecuteReaderAsync(cancellationToken)).Read();
        }
    }
}