using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

namespace IFY.Phorm.Tests;

/// <summary>
/// Mockable test object with useful default implementation.
/// </summary>
[ExcludeFromCodeCoverage]
public partial class TestDbCommand : DbCommand, IAsyncDbCommand, IDbCommand
{
    public int ReturnValue { get; set; } = 1;
    public DbDataReader Reader { get; set; }

    public CancellationToken ExecutionCancellationToken { get; private set; } = CancellationToken.None;

    public override string CommandText { get; set; } = string.Empty;
    public override int CommandTimeout { get; set; }
    public override CommandType CommandType { get; set; }
    public new virtual IDbConnection? Connection { get; set; }

    public new virtual IDataParameterCollection Parameters { get; } = new TestDataParameterCollection();

    public new virtual IDbTransaction? Transaction { get; set; }
    public override UpdateRowSource UpdatedRowSource { get; set; }

    protected override DbConnection? DbConnection { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    protected override DbParameterCollection DbParameterCollection => throw new NotImplementedException();
    protected override DbTransaction? DbTransaction { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public override bool DesignTimeVisible { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public Action? OnExecuteReaderAsync { get; set; }

    public TestDbCommand()
    {
        Reader = new TestDbDataReader();
    }
    public TestDbCommand(DbDataReader reader)
    {
        Reader = reader;
    }

    public override void Cancel()
    {
    }

    public new virtual IDbDataParameter CreateParameter()
    {
        return new TestDbDataParameter();
    }

    public new virtual void Dispose()
    {
        base.Dispose();
    }
    public override ValueTask DisposeAsync()
    {
        return base.DisposeAsync();
    }
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
    }

    public override int ExecuteNonQuery()
    {
        throw new NotImplementedException();
    }

    public new virtual IDataReader ExecuteReader()
    {
        throw new NotImplementedException();
    }

    public new virtual IDataReader ExecuteReader(CommandBehavior behavior)
    {
        throw new NotImplementedException();
    }

    public new virtual Task<DbDataReader> ExecuteReaderAsync(CancellationToken cancellationToken)
    {
        ExecutionCancellationToken = cancellationToken;

        OnExecuteReaderAsync?.Invoke();
        var retvalParam = Parameters.AsParameters()
            .FirstOrDefault(p => p.Direction == ParameterDirection.ReturnValue);
        if (retvalParam != null)
        {
            retvalParam.Value = ReturnValue;
        }

        return Task.FromResult(Reader);
    }

    public override object? ExecuteScalar()
    {
        throw new NotImplementedException();
    }

    public override void Prepare()
    {
    }

    protected override DbParameter CreateDbParameter()
    {
        throw new NotImplementedException();
    }

    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
    {
        throw new NotImplementedException();
    }
}
