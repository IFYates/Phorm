using IFY.Phorm.Connectivity;
using IFY.Phorm.Execution;
using Microsoft.Data.SqlClient;
using System.Data;

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
    internal Func<string, string?, IPhormDbConnection> _connectionBuilder = (sqlConnStr, connectionName) => new PhormDbConnection(connectionName, new SqlConnection(sqlConnStr));

    /// <inheritdoc/>
    public override IPhormSession SetConnectionName(string connectionName)
    {
        return new SqlPhormSession(_databaseConnectionString, connectionName)
        {
            _connectionBuilder = _connectionBuilder
        };
    }

    /// <inheritdoc/>
    protected override IPhormDbConnection CreateConnection()
    {
        // Ensure application name is known user
        var connectionString = new SqlConnectionStringBuilder(_databaseConnectionString);
        connectionString.ApplicationName = ConnectionName ?? connectionString.ApplicationName;
        var sqlConnStr = connectionString.ToString();

        // Create connection
        var conn = _connectionBuilder(sqlConnStr, ConnectionName);
        if (conn.DefaultSchema.Length == 0)
        {
            conn.DefaultSchema = connectionString.UserID;
        }
        return conn;
    }

    /// <inheritdoc/>
    protected override string? GetDefaultSchema(IPhormDbConnection phormConn)
    {
        using var cmd = ((IDbConnection)phormConn).CreateCommand();
        cmd.CommandText = "SELECT schema_name()";
        return cmd.ExecuteScalar()?.ToString();
    }

    #region Console capture

    /// <inheritdoc/>
    protected override AbstractConsoleMessageCapture StartConsoleCapture(Guid commandGuid, IAsyncDbCommand cmd)
    {
        return cmd.Connection is SqlConnection sql
            ? new SqlConsoleMessageCapture(this, commandGuid, sql)
            : (AbstractConsoleMessageCapture)NullConsoleMessageCapture.Instance;
    }

    #endregion Console capture

    #region Transactions

    /// <inheritdoc/>
    public override bool SupportsTransactions => true;
    /// <inheritdoc/>
    public override bool IsInTransaction => false;

    /// <inheritdoc/>
    public override ITransactedPhormSession BeginTransaction()
    {
        var conn = GetConnection();
        conn.Open();
        var transaction = conn.BeginTransaction();
        return WrapSessionAsTransacted(transaction);
    }

    #endregion Transactions
}
