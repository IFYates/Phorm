using IFY.Phorm.Connectivity;
using System;
using System.Collections.Generic;
using System.Data;

namespace IFY.Phorm.Tests
{
    public class TestPhormConnection : IPhormDbConnection
    {
        public Queue<TestDbCommand> CommandQueue { get; } = new();

        public virtual string? ConnectionName { get; }

        public virtual string DefaultSchema { get; set; } = "dbo";
        public virtual string ConnectionString { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public virtual int ConnectionTimeout => throw new NotImplementedException();

        public virtual string Database => throw new NotImplementedException();

        public virtual ConnectionState State => throw new NotImplementedException();

        public TestPhormConnection(string? connectionName)
        {
            ConnectionName = connectionName;
        }

        public virtual IDbTransaction BeginTransaction()
        {
            throw new NotImplementedException();
        }

        public virtual IDbTransaction BeginTransaction(IsolationLevel il)
        {
            throw new NotImplementedException();
        }

        public virtual void ChangeDatabase(string databaseName)
        {
            throw new NotImplementedException();
        }

        public virtual void Close()
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
        IDbCommand IDbConnection.CreateCommand() => (IDbCommand)CreateCommand();

        public virtual void Dispose()
        {
            throw new NotImplementedException();
        }

        public virtual void Open()
        {
            throw new NotImplementedException();
        }
    }
}
