using IFY.Phorm.Connectivity;
using System.Data;
using System.Threading;

namespace IFY.Phorm.SqlClient.IntegrationTests
{
    internal class SqlTestHelpers
    {
        public static void ApplySql(IPhormDbConnectionProvider connProv, string sql)
        {
            using var conn = connProv.GetConnection(null);
            using var cmd = conn.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = sql;
            _ = cmd.ExecuteReaderAsync(CancellationToken.None).Result.Read();
        }
    }
}