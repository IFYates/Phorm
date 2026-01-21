using IFY.Phorm.Connectivity;
using System.Data;
using System.Diagnostics.CodeAnalysis;

namespace IFY.Phorm.Tests;

[ExcludeFromCodeCoverage]
public class TestPhormConnection(string? connectionName) : IPhormDbConnection
{
    public Queue<IAsyncDbCommand> CommandQueue { get; } = new Queue<IAsyncDbCommand>();

    public virtual string? ConnectionName { get; } = connectionName;

    public virtual string DefaultSchema { get; set; } = "dbo";
    [AllowNull] public virtual string ConnectionString { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public virtual int ConnectionTimeout => throw new NotImplementedException();

    public virtual string Database => throw new NotImplementedException();

    public virtual ConnectionState State => ConnectionState.Open;

    public virtual ValueTask<IDbTransaction> BeginTransactionAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public virtual ValueTask<IDbTransaction> BeginTransactionAsync(IsolationLevel il, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public virtual Task ChangeDatabaseAsync(string databaseName, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public virtual Task CloseAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public virtual IAsyncDbCommand CreateCommand()
    {
        if (CommandQueue.TryDequeue(out var cmd))
        {
            return cmd;
        }
        return new TestDbCommand();
    }
    IDbCommand IAsyncDbConnection.CreateCommand() => (IDbCommand)CreateCommand();

    public virtual void Dispose()
    {
        // NOOP
    }

    public virtual Task OpenAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
