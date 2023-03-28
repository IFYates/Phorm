using Microsoft.Data.SqlClient;
using System.Data;
using System.Text;

namespace IFY.Phorm.ExampleApp;

public static class DatabaseHelper
{
    public const string DB_CONN = @"Server=(localdb)\ProjectModels;Database=PhormTests;MultipleActiveResultSets=True";

    public static void RunScript(string filename)
    {
        using var db = new SqlConnection(DB_CONN);
        db.Open();

        var resourceName = typeof(Program).Assembly.GetManifestResourceNames()
            .Single(n => n.EndsWith("." + filename, StringComparison.OrdinalIgnoreCase));
        var stream = typeof(Program).Assembly.GetManifestResourceStream(resourceName)!;

        var sb = new StringBuilder();
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            var ln = reader.ReadLine();
            if (ln?.Equals("GO", StringComparison.OrdinalIgnoreCase) == true)
            {
                var sql = sb.ToString().Trim();
                if (sql.Length > 0)
                {
                    runSQL(db, sql);
                    sb.Clear();
                }
            }
            else
            {
                sb.AppendLine(ln);
            }
        }
    }

    private static void runSQL(IDbConnection db, string sql)
    {
        using var cmd = db.CreateCommand();
        cmd.CommandType = CommandType.Text;
        cmd.CommandText = sql;
        _ = cmd.ExecuteNonQuery();
    }
}
