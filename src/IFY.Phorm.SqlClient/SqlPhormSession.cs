using IFY.Phorm.Connectivity;
using IFY.Phorm.Execution;
using IFY.Shimr.Extensions;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Data.Common;
using System.Text;

namespace IFY.Phorm.SqlClient;

/// <summary>
/// Provides a session for interacting with a SQL Server database using the Phorm data access framework.
/// </summary>
/// <remarks>This session enables SQL Server-specific features, such as transaction support and schema discovery.
/// It is designed for use with the Phorm framework and should be disposed of properly to release database
/// resources.</remarks>
/// <param name="databaseConnectionString">The connection string used to establish a connection to the SQL Server database.</param>
/// <param name="connectionName">An optional name for the connection, used to identify the session or set the application name in the connection
/// string. If null, the default application name is used.</param>
public class SqlPhormSession(string databaseConnectionString, string? connectionName = null)
    : AbstractPhormSession(databaseConnectionString, connectionName)
{
    internal Func<string, IAsyncDbConnection> _connectionBuilder = (sqlConnStr) => new SqlConnection(sqlConnStr).Shim<IAsyncDbConnection>();

    /// <inheritdoc/>
    public override IPhormSession WithContext(string connectionName, IDictionary<string, object?> contextData)
    {
        // TODO
        return new SqlPhormSession(_databaseConnectionString, connectionName)
        {
            _connectionBuilder = _connectionBuilder
        };
    }

    /// <inheritdoc/>
    protected override string GetConnectionString(bool readOnly)
    {
        var connstrBuilder = new SqlConnectionStringBuilder(_databaseConnectionString)
        {
            ApplicationIntent = readOnly ? ApplicationIntent.ReadOnly : ApplicationIntent.ReadWrite,
        };
        connstrBuilder.ApplicationName = ConnectionName ?? connstrBuilder.ApplicationName;
        return connstrBuilder.ConnectionString;
    }

    /// <inheritdoc/>
    protected override IAsyncDbConnection CreateConnection(string connectionString)
    {
        return _connectionBuilder(connectionString);
    }

    /// <inheritdoc/>
    protected override async Task ApplyContextAsync(IPhormDbConnection phormConn)
    {
        if (ContextData.Count > 0)
        {
            using var cmd = phormConn.CreateCommand();
            var sql = new StringBuilder();
            var data = ContextData.ToArray();
            for (var i = 0; i < data.Length; i++)
            {
                sql.AppendLine($"EXEC sp_set_session_context @keyParam{i}, @valueParam{i};");
                var keyParam = cmd.CreateParameter();
                keyParam.ParameterName = $"@keyParam{i}";
                keyParam.DbType = DbType.String;
                keyParam.Value = data[i].Key;
                cmd.Parameters.Add(keyParam);
                var valueParam = cmd.CreateParameter();
                valueParam.ParameterName = $"@valueParam{i}";
                valueParam.DbType = DbType.String;
                valueParam.Value = data[i].Value ?? DBNull.Value;
                cmd.Parameters.Add(valueParam);
            }
            await cmd.ExecuteNonQueryAsync(default);
        }
    }

    /// <inheritdoc/>
    protected override async Task ResolveDefaultSchemaAsync(IPhormDbConnection phormConn)
    {
        using var cmd = phormConn.CreateCommand();
        cmd.CommandText = "SELECT schema_name()";
        var result = await cmd.ExecuteScalarAsync(default);
        phormConn.DefaultSchema = result?.ToString()
            ?? new SqlConnectionStringBuilder(_databaseConnectionString).UserID;
    }

    #region Console capture

    /// <inheritdoc/>
    protected override AbstractConsoleMessageCapture StartConsoleCapture(Guid commandGuid, IAsyncDbCommand cmd)
    {
        return cmd.Connection is SqlConnection sql
            ? new SqlConsoleMessageCapture(this, commandGuid, sql)
            : NullConsoleMessageCapture.Instance;
    }

    #endregion Console capture

    #region Transactions

    /// <inheritdoc/>
    public override bool SupportsTransactions => true;
    /// <inheritdoc/>
    public override bool IsInTransaction => false;

    /// <inheritdoc/>
    public override async Task<ITransactedPhormSession> BeginTransactionAsync(CancellationToken cancellationToken)
    {
        var conn = GetConnection(false);
        var transaction = await conn.BeginTransactionAsync(cancellationToken);
        return WrapSessionAsTransacted(transaction);
    }

    #endregion Transactions
}
