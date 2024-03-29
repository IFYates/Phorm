﻿using IFY.Phorm.Connectivity;
using IFY.Phorm.Execution;
using Microsoft.Data.SqlClient;
using System.Data;

namespace IFY.Phorm.SqlClient;

public class SqlPhormSession : AbstractPhormSession
{
    internal Func<string, string?, IPhormDbConnection> _connectionBuilder = (sqlConnStr, connectionName) => new PhormDbConnection(connectionName, new SqlConnection(sqlConnStr));

    public SqlPhormSession(string databaseConnectionString)
        : base(databaseConnectionString, null)
    { }
    public SqlPhormSession(string databaseConnectionString, string? connectionName)
        : base(databaseConnectionString, connectionName)
    { }

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
